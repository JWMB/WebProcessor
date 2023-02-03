using Microsoft.Data.Analysis;
using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
using OldDb.Models;
using OldDbAdapter;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using System.Reflection.Emit;
using System.Reflection;
using static Microsoft.ML.DataOperationsCatalog;
using System.Text;

namespace Tools
{
    internal class OldDbMLFeatures
    {
        public async Task<string[]> CreateFeaturesForTraining(int trainingId)
        {
            var dbContext = new TrainingDbContext();
            var logItems = await RecreateLogFromOldDb.GetAsLogItems(dbContext, trainingId, Enumerable.Range(0, 6));

            var phases = LogEventsToPhases.Create(logItems);
            var features = MLFeaturesJulia.FromPhases(new TrainingSettings(), phases.PhasesCreated, 6, null, 5);
            return features.ToArray();
        }

        public async Task X()
        {
            // TODO: some criteria
            var trainingIds = new List<int>();

            var features = new List<string[]>();
            foreach (var id in trainingIds)
            {
                features.Add(await CreateFeaturesForTraining(id));
            }
        }

        private bool X(ColumnInformation colInfo, string name, Func<ColumnInformation, ICollection<string>> getCollection)
        {
            var all = new[]
            {
                colInfo.NumericColumnNames,
                colInfo.TextColumnNames,
                colInfo.CategoricalColumnNames,
                colInfo.IgnoredColumnNames,
                colInfo.ImagePathColumnNames,
            };
            var found = all.FirstOrDefault(o => o.Contains(name));
            if (found != null)
            {
                found.Remove(name);
                getCollection(colInfo).Add(name);
                return true;
            }
            return false;
        }

        private object CreateGenericPrediction(MLContext mlContext, DataViewSchema dataViewSchema, Type predictionType, ITransformer model, object inputObject)
        {
            // Create runtime type from fields and types in a DataViewSchema
            var methodName = "CreatePredictionEngine";
            var genericPredictionMethod = mlContext.Model.GetType().GetMethod(methodName, new[] { typeof(ITransformer), typeof(DataViewSchema) });
            if (genericPredictionMethod == null)
                throw new Exception($"'{methodName}' not found");

            var runtimeType = ClassFactory.CreateType(dataViewSchema);
            if (runtimeType == null)
                throw new Exception("Could not create type");

            var predictionMethod = genericPredictionMethod.MakeGenericMethod(runtimeType, predictionType);
            dynamic? dynamicPredictionEngine = predictionMethod.Invoke(mlContext.Model, new object[] { model, dataViewSchema });
            if (dynamicPredictionEngine == null)
                throw new Exception($"Could not create {predictionMethod.Name}");

            var inputInstance = ClassFactory.CreateInstance(runtimeType, inputObject);

            var predictMethod = dynamicPredictionEngine.GetType().GetMethod("Predict", new[] { runtimeType });
            return predictMethod.Invoke(dynamicPredictionEngine, new[] { inputInstance });
        }

        public void TutorialTest()
        {
            var path = @"C:\Users\uzk446\Downloads\";
            var trainDataPath = Path.Join(path, "taxi-fare-train.csv");
            var testDataPath = Path.Join(path, "taxi-fare-test.csv");
            var savedModelPath = Path.Join(path, "taxi-fare-model.zip");
            var ctx = new MLContext(seed: 0);

            // https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/how-to-use-the-automl-api
            var labelColumnName = "fare_amount";

            ITransformer bestModel;
            DataViewSchema schema;
            if (File.Exists(savedModelPath))
            {
                bestModel = ctx.Model.Load(savedModelPath, out schema);
            }
            else
            {
                var columnInference = ctx.Auto().InferColumns(trainDataPath, labelColumnName: labelColumnName, groupColumns: false);
                X(columnInference.ColumnInformation, "rate_code", ci => ci.CategoricalColumnNames);
                //columnInference.ColumnInformation.NumericColumnNames.Remove("rate_code");
                //columnInference.ColumnInformation.CategoricalColumnNames.Add("rate_code");
                X(columnInference.ColumnInformation, "vendor_id", ci => ci.CategoricalColumnNames);
                X(columnInference.ColumnInformation, "payment_type", ci => ci.CategoricalColumnNames);

                //mlContext.Auto().CreateRegressionExperiment(new RegressionExperimentSettings { });
                var loader = ctx.Data.CreateTextLoader(columnInference.TextLoaderOptions);
                // Load data into IDataView
                var data = loader.Load(trainDataPath);

                //var trainValidationData = ctx.Data.TrainTestSplit(data, testFraction: 0.2);
                var trainValidationData = loader.Load(testDataPath);
                var pipeline = ctx.Auto()
                    .Featurizer(data, columnInformation: columnInference.ColumnInformation)
                    .Append(ctx.Auto().Regression(labelColumnName: labelColumnName));
                var experiment = ctx.Auto()
                    .CreateExperiment()
                    .SetPipeline(pipeline)
                    .SetRegressionMetric(RegressionMetric.RSquared, labelColumn: labelColumnName)
                    .SetTrainingTimeInSeconds(60)
                    .SetDataset(trainValidationData);
                // ctx.Auto().Regression(labelColumnName: labelColumnName, useLgbm: false);
                ctx.Log += (_, e) =>
                {
                    if (e.Source.Equals("AutoMLExperiment"))
                        Console.WriteLine(e.RawMessage);
                };

                var experimentResults = experiment.Run();

                schema = data.Schema;
                bestModel = experimentResults.Model;
                ctx.Model.Save(bestModel, data.Schema, savedModelPath);
            }

            //// https://github.com/dotnet/machinelearning-samples/issues/783
            //var transformedData = bestModel.Transform(data);
            //var pfi = ctx.Regression.PermutationFeatureImportance(bestModel, transformedData, permutationCount: 3, labelColumnName: columnInference.ColumnInformation.LabelColumnName);
            //var featureImportance = pfi.Select(x => Tuple.Create(x.Key, x.Value.RSquared))
            //    .OrderByDescending(x => x.Item2);


//            var item = new NewClass(
//"CMT",
//1,
//1,
//1271,
//3.8f,
//"CRD",
//0 //17.5
//);
            //var predictionFunction = ctx.Model.CreatePredictionEngine<NewClass, TaxiTripFarePrediction>(model);
            //var prediction = predictionFunction.Predict(item);

            var predictionType = ClassFactory.CreateType(
                new[] { "Score" }, //labelColumnName
                new[] { schema[labelColumnName].Type.RawType });
            CreateGenericPrediction(ctx, schema, predictionType, bestModel, new
            {
                vendor_id = "CMT",
                rate_code = 1,
                passenger_count = 1,
                trip_time_in_secs = 1271,
                trip_distance = 3.8f,
                payment_type = "CRD",
                fare_amount = 0 //17.5
            });


            var model = Train(ctx, trainDataPath);
            Evaluate(ctx, model, testDataPath);

            TestSinglePrediction(ctx, model, new TaxiTrip()
            {
                VendorId = "VTS",
                RateCode = "1",
                PassengerCount = 1,
                TripTime = 1140,
                TripDistance = 3.75f,
                PaymentType = "CRD",
                FareAmount = 0 // To predict. Actual/Observed = 15.5
            });
        }

        ITransformer Train(MLContext mlContext, string dataPath)
        {
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "FareAmount")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "VendorIdEncoded", inputColumnName: "VendorId"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RateCodeEncoded", inputColumnName: "RateCode"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PaymentTypeEncoded", inputColumnName: "PaymentType"))
                .Append(mlContext.Transforms.Concatenate("Features", "VendorIdEncoded", "RateCodeEncoded", "PassengerCount", "TripDistance", "PaymentTypeEncoded"))
                .Append(mlContext.Regression.Trainers.FastTree());

            var model = pipeline.Fit(LoadFromCsv(dataPath));
            return model;
        }

        IDataView LoadFromCsv(string filename)
        {
            //return mlContext.Data.LoadFromTextFile<TaxiTrip>(dataPath, hasHeader: true, separatorChar: ',');
            //return mlContext.Data.LoadFromTextFile(dataPath, new TextLoader.Column[] { new TextLoader.Column() }, hasHeader: true, separatorChar: ',');

            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            return DataFrame.LoadCsv(filename, guessRows: 20);
        }

        void Evaluate(MLContext mlContext, ITransformer model, string testDataPath)
        {
            var dataView = LoadFromCsv(testDataPath); // mlContext.Data.LoadFromTextFile<TaxiTrip>(testDataPath, hasHeader: true, separatorChar: ',');
            var predictions = model.Transform(dataView);
            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");

            Console.WriteLine($"RSquared Score: {metrics.RSquared:0.##}, Root Mean Squared Error: {metrics.RootMeanSquaredError:#.##}");
        }

        void TestSinglePrediction(MLContext mlContext, ITransformer model, TaxiTrip item)
        {
            var predictionFunction = mlContext.Model.CreatePredictionEngine<TaxiTrip, TaxiTripFarePrediction>(model);
            var prediction = predictionFunction.Predict(item);

            Console.WriteLine($"Predicted fare: {prediction.FareAmount:0.####}, actual fare: 15.5");
        }

        public class TaxiTrip
        {
            [LoadColumn(0)]
            public string VendorId;

            [LoadColumn(1)]
            public string RateCode;

            [LoadColumn(2)]
            public float PassengerCount;

            [LoadColumn(3)]
            public float TripTime;

            [LoadColumn(4)]
            public float TripDistance;

            [LoadColumn(5)]
            public string PaymentType;

            [LoadColumn(6)]
            public float FareAmount;
        }

        public class TaxiTripFarePrediction
        {
            [ColumnName("Score")]
            public float FareAmount;
        }
    }

    public static class ClassFactory
    {
        //private static AssemblyName _assemblyName;
        //public static object CreateObject(string[] propertyNames, Type[] Types)
        //{
        //    var assemblyName = new AssemblyName("DynamicInput");
        //    if (propertyNames.Length != Types.Length)
        //        throw new ArgumentException("The number of property names should match their corresponding types number");

        //    var dynamicClass = CreateTypeBuilder(assemblyName);
        //    CreateConstructor(dynamicClass);
        //    for (int ind = 0; ind < propertyNames.Count(); ind++)
        //        CreateProperty(dynamicClass, propertyNames[ind], Types[ind]);
        //    var type = dynamicClass.CreateType();
        //    var instance = Activator.CreateInstance(type);
        //    if (instance == null)
        //        throw new NullReferenceException($"Could not instantiate dynamic type {type.Name}");
        //    return instance;
        //}

        public static object CreateInstance(Type type, object values)
        {
            var instance = CreateInstance(type);
            var inputType = values.GetType();
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var val = inputType.GetProperty(prop.Name)!.GetValue(values);
                if (prop.PropertyType.Name.Contains("ReadOnlyMemory`") && val is string str)
                {
                    val = new ReadOnlyMemory<char>(Encoding.UTF8.GetBytes(str).Select(o => (char)o).ToArray());
                }
                prop.SetValue(instance, val);
            }

            //foreach (var item in dataViewSchema)
            //    runtimeType.GetProperty(item.Name)!.SetValue(inputInstance, inputObject.GetType().GetProperty(item.Name).GetValue(inputObject));

            return instance;
        }

        public static object CreateInstance(Type type)
        {
            var instance = Activator.CreateInstance(type);
            if (instance == null)
                throw new NullReferenceException($"Could not instantiate dynamic type {type.Name}");
            return instance;
        }

        public static Type CreateType(string[] propertyNames, Type[] Types, AssemblyName? assemblyName = null)
        {
            assemblyName ??= new AssemblyName("DynamicInput");
            if (propertyNames.Length != Types.Length)
                throw new ArgumentException("The number of property names should match their corresponding types number");

            var dynamicClass = CreateTypeBuilder(assemblyName);
            CreateConstructor(dynamicClass);
            for (int ind = 0; ind < propertyNames.Count(); ind++)
                CreateProperty(dynamicClass, propertyNames[ind], Types[ind]);
            return dynamicClass.CreateType();
        }

        public static Type CreateType(DataViewSchema dataViewSchema, AssemblyName? assemblyName = null)
        {
            assemblyName ??= new AssemblyName("DynamicInput");
            var dynamicClass = CreateTypeBuilder(assemblyName);
            CreateConstructor(dynamicClass);
            foreach (var item in dataViewSchema)
                CreateProperty(dynamicClass, item.Name, item.Type.RawType);
            return dynamicClass.CreateType();
        }

        private static TypeBuilder CreateTypeBuilder(AssemblyName assemblyName)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            var typeBuilder = moduleBuilder.DefineType(assemblyName.FullName
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , null);
            return typeBuilder;
        }

        private static void CreateConstructor(TypeBuilder typeBuilder)
        {
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        }

        private static void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            var fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            var getPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            var getIl = getPropMthdBldr.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);
            var setPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });
            var setIl = setPropMthdBldr.GetILGenerator();
            var modifyProperty = setIl.DefineLabel();
            var exitSet = setIl.DefineLabel();
            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }

    internal class NewClass
    {
        public string Vendor_id { get; }
        public int Rate_code { get; }
        public int Passenger_count { get; }
        public int Trip_time_in_secs { get; }
        public float Trip_distance { get; }
        public string Payment_type { get; }
        public int Fare_amount { get; }

        public NewClass(string vendor_id, int rate_code, int passenger_count, int trip_time_in_secs, float trip_distance, string payment_type, int fare_amount)
        {
            Vendor_id = vendor_id;
            Rate_code = rate_code;
            Passenger_count = passenger_count;
            Trip_time_in_secs = trip_time_in_secs;
            Trip_distance = trip_distance;
            Payment_type = payment_type;
            Fare_amount = fare_amount;
        }

        public override bool Equals(object? obj)
        {
            return obj is NewClass other &&
                   Vendor_id == other.Vendor_id &&
                   Rate_code == other.Rate_code &&
                   Passenger_count == other.Passenger_count &&
                   Trip_time_in_secs == other.Trip_time_in_secs &&
                   Trip_distance == other.Trip_distance &&
                   Payment_type == other.Payment_type &&
                   Fare_amount == other.Fare_amount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Vendor_id, Rate_code, Passenger_count, Trip_time_in_secs, Trip_distance, Payment_type, Fare_amount);
        }
    }
}
