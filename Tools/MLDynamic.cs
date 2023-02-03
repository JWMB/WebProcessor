using Microsoft.ML;
using Microsoft.ML.AutoML;

namespace Tools
{
    public class MLDynamic
    {
        public void TutorialTest(string trainDataPath, string testDataPath, string labelColumnName, string[]? categoricalColumnNames = null, string? savedModelPath = null)
        {
            var ctx = new MLContext(seed: 0);

            // https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/how-to-use-the-automl-api

            ITransformer bestModel;
            DataViewSchema schema;
            if (!string.IsNullOrEmpty(savedModelPath) && File.Exists(savedModelPath))
            {
                bestModel = ctx.Model.Load(savedModelPath, out schema);
            }
            else
            {
                var result = Train(ctx, trainDataPath, testDataPath, labelColumnName, categoricalColumnNames);
                bestModel = result.model;
                schema = result.data.Schema;

                //CalcFeatureImportance(ctx, bestModel, result.data, labelColumnName);

                if (!string.IsNullOrEmpty(savedModelPath))
                    ctx.Model.Save(bestModel, schema, savedModelPath);
            }


            var prediction = CreateGenericPrediction(ctx, schema, bestModel, new
            {
                vendor_id = "CMT",
                rate_code = 1,
                passenger_count = 1,
                trip_time_in_secs = 1271,
                trip_distance = 3.8f,
                payment_type = "CRD",
                fare_amount = 0 //17.5
            }, labelColumnName);
        }

        private void CalcFeatureImportance(MLContext ctx, ITransformer model, IDataView data, string labelColumnName)
        {
            // https://github.com/dotnet/machinelearning-samples/issues/783
            var transformedData = model.Transform(data);
            var pfi = ctx.Regression.PermutationFeatureImportance(model, transformedData, permutationCount: 3, labelColumnName: labelColumnName);
            var featureImportance = pfi.Select(x => Tuple.Create(x.Key, x.Value.RSquared))
                .OrderByDescending(x => x.Item2);
        }

        private (IDataView data, ITransformer model) Train(MLContext ctx, string trainDataPath, string testDataPath, string labelColumnName, string[]? categoricalColumnNames = null)
        {
            var columnInference = ctx.Auto().InferColumns(trainDataPath, labelColumnName: labelColumnName, groupColumns: false);

            //MoveColumnCollection(columnInference.ColumnInformation, "rate_code", ci => ci.CategoricalColumnNames);
            //MoveColumnCollection(columnInference.ColumnInformation, "vendor_id", ci => ci.CategoricalColumnNames);
            //MoveColumnCollection(columnInference.ColumnInformation, "payment_type", ci => ci.CategoricalColumnNames);
            if (categoricalColumnNames != null)
            {
                foreach (var name in categoricalColumnNames)
                    MoveColumnCollection(columnInference.ColumnInformation, name, ci => ci.CategoricalColumnNames);
            }

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

            return (data, experimentResults.Model);
            //schema = data.Schema;
            //bestModel = experimentResults.Model;
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

        private object CreateGenericPrediction(MLContext mlContext, DataViewSchema dataViewSchema, ITransformer model, object inputObject, string labelColumnName)
        {
            var predictionType = ClassFactory.CreateType(
                    new[] { "Score" },
                    new[] { dataViewSchema[labelColumnName].Type.RawType });
            return CreateGenericPrediction(mlContext, dataViewSchema, model, inputObject, predictionType);
        }

        private object CreateGenericPrediction(MLContext mlContext, DataViewSchema dataViewSchema, ITransformer model, object inputObject, Type predictionType)
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
