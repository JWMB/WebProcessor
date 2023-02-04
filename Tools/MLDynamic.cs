using Microsoft.ApplicationInsights;
using Microsoft.ML;
using Microsoft.ML.AutoML;

namespace Tools
{
    public class MLDynamic
    {
        private readonly MLContext ctx;
        private ITransformer? model;
        private DataViewSchema? schema;
        private string? labelColumnName;

        public MLDynamic()
        {
            ctx = new MLContext(seed: 0);
        }

        public void Train(string[] dataPaths, string labelColumnName, IEnumerable<string>? categoricalColumnNames = null, string? savedModelPath = null)
        {
            // https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/how-to-use-the-automl-api

            this.labelColumnName = labelColumnName;

            if (!string.IsNullOrEmpty(savedModelPath) && File.Exists(savedModelPath))
            {
                model = ctx.Model.Load(savedModelPath, out schema);
            }
            else
            {
                var (data, colInfo) = LoadData(ctx, dataPaths, labelColumnName, categoricalColumnNames);
                schema = data.Schema;

                var result = Train(ctx, data, colInfo, labelColumnName);
                model = result.Model;

                var info = $"{nameof(result.Metric)}:{result.Metric}, {nameof(result.Loss)}:{result.Loss}";
                //CalcFeatureImportance(ctx, model, result.data, labelColumnName);

                if (!string.IsNullOrEmpty(savedModelPath))
                    ctx.Model.Save(model, schema, savedModelPath);
            }
        }

        public void CalcFeatureImportance(MLContext ctx, ITransformer model, IDataView data, string labelColumnName)
        {
            // https://github.com/dotnet/machinelearning-samples/issues/783
            var transformedData = model.Transform(data);
            var pfi = ctx.Regression.PermutationFeatureImportance(model, transformedData, permutationCount: 3, labelColumnName: labelColumnName);
            var featureImportance = pfi.Select(x => Tuple.Create(x.Key, x.Value.RSquared))
                .OrderByDescending(x => x.Item2);
        }

        private static (IDataView, ColumnInformation) LoadData(MLContext ctx, string[] dataPaths, string labelColumnName, IEnumerable<string>? categoricalColumnNames = null)
        {
            var columnInference = ctx.Auto().InferColumns(dataPaths.First(), labelColumnName: labelColumnName, groupColumns: false);

            if (categoricalColumnNames != null)
            {
                foreach (var name in categoricalColumnNames)
                    MoveColumnCollection(columnInference.ColumnInformation, name, ci => ci.CategoricalColumnNames);
            }

            // Column inference may get labelColumn type wrong
            columnInference.TextLoaderOptions.Columns.Single(o => o.Name == labelColumnName).DataKind = Microsoft.ML.Data.DataKind.Single;

            //mlContext.Auto().CreateRegressionExperiment(new RegressionExperimentSettings { });
            var loader = ctx.Data.CreateTextLoader(columnInference.TextLoaderOptions);
            //var data = loader.Load(trainDataPath);

            //var trainValidationDataX = ctx.Data.TrainTestSplit(data, testFraction: 0.2);
            var data = loader.Load(dataPaths);

            return (data, columnInference.ColumnInformation);
        }

        private static TrialResult Train(MLContext ctx, IDataView data, ColumnInformation colInfo, string labelColumnName) //, IEnumerable<string>? categoricalColumnNames = null)
        {
            var pipeline = ctx.Auto()
                .Featurizer(data, columnInformation: colInfo)
                .Append(ctx.Auto().Regression(labelColumnName: labelColumnName));
            var experiment = ctx.Auto()
                .CreateExperiment()
                .SetPipeline(pipeline)
                .SetRegressionMetric(RegressionMetric.RSquared, labelColumn: labelColumnName)
                .SetTrainingTimeInSeconds(60)
                .SetDataset(data);
            // ctx.Auto().Regression(labelColumnName: labelColumnName, useLgbm: false);
            ctx.Log += (_, e) =>
            {
                if (e.Source.Equals("AutoMLExperiment"))
                    Console.WriteLine(e.RawMessage);
            };

            var experimentResults = experiment.Run();

            if (experimentResults == null)
                throw new NullReferenceException(nameof(experimentResults));
            return experimentResults;
        }

        private static bool MoveColumnCollection(ColumnInformation colInfo, string name, Func<ColumnInformation, ICollection<string>> getCollection)
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

        public object CreateGenericPrediction(object inputObject)
        {
            if (schema == null) throw new NullReferenceException($"{nameof(schema)} is null");
            if (model == null) throw new NullReferenceException($"{nameof(model)} is null");
            if (string.IsNullOrEmpty(labelColumnName)) throw new NullReferenceException($"{nameof(labelColumnName)} is null");
            var predictionType = ClassFactory.CreateType(
                    new[] { "Score" },
                    new[] { schema[labelColumnName].Type.RawType });
            return CreateGenericPrediction(ctx, schema, model, inputObject, predictionType);
        }

        private static object CreateGenericPrediction(MLContext mlContext, DataViewSchema dataViewSchema, ITransformer model, object inputObject, Type predictionType)
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
            var predictionResult = predictMethod.Invoke(dynamicPredictionEngine, new[] { inputInstance });
            return predictionType.GetProperty("Score")!.GetValue(predictionResult);
        }
    }
}
