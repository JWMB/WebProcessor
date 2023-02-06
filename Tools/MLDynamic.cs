using Microsoft.ML;
using Microsoft.ML.AutoML;
using System.Data;
using static TorchSharp.torch.utils;

namespace Tools
{
    public class MLDynamic
    {
        private readonly MLContext ctx;
        public ITransformer? Model { get; private set; }
        public DataViewSchema? Schema { get; private set; }
        public IDataView? DataView { get; private set; }
        public ColumnInformation? ColumnInformation { get; private set; }
        private ColumnInfo ColInfo { get; set; }

        public MLDynamic(ColumnInfo columnInfo)
        {
            ctx = new MLContext(seed: 0);

            ColInfo = columnInfo;
        }

        public class ColumnInfo
        {
            public string Label { get; set; } = string.Empty;
            public IEnumerable<string>? Categorical { get; set; }
            public IEnumerable<string>? Ignore { get; set; }
        }

        public bool TryLoad(string savedModelPath)
        {
            if (File.Exists(savedModelPath))
            {
                Model = ctx.Model.Load(savedModelPath, out var schema);
                Schema = schema;
                return true;
            }
            return false;
        }

        public async Task Train(string[] dataPaths, string? savedModelPath = null, TimeSpan? trainingTime = null, CancellationToken cancellation = default)
        {
            // https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/how-to-use-the-automl-api

            LoadData(dataPaths);

            if (DataView == null) throw new NullReferenceException(nameof(DataView));
            if (ColumnInformation == null) throw new NullReferenceException(nameof(ColumnInformation));

            var result = await Train(ctx, DataView, ColumnInformation, ColInfo.Label, trainingTime, cancellation);
            Model = result.Model;

            var info = $"{nameof(result.Metric)}:{result.Metric}, {nameof(result.Loss)}:{result.Loss}";
            //CalcFeatureImportance(ctx, model, result.data, labelColumnName);

            if (!string.IsNullOrEmpty(savedModelPath))
                ctx.Model.Save(Model, Schema, savedModelPath);
        }

        public void CalcFeatureImportance(MLContext ctx, ITransformer model, IDataView data, string labelColumnName)
        {
            // https://github.com/dotnet/machinelearning-samples/issues/783
            var transformedData = model.Transform(data);
            var pfi = ctx.Regression.PermutationFeatureImportance(model, transformedData, permutationCount: 3, labelColumnName: labelColumnName);
            var featureImportance = pfi.Select(x => Tuple.Create(x.Key, x.Value.RSquared))
                .OrderByDescending(x => x.Item2);
        }

        public void LoadData(string[] dataPaths)
        {
            var (data, colInfo) = LoadData(ctx, dataPaths, ColInfo);
            DataView = data;
            Schema = data.Schema;
            ColumnInformation = colInfo;
        }

        private static (IDataView, ColumnInformation) LoadData(MLContext ctx, string[] dataPaths, ColumnInfo columnInfo)
        {
            var columnInference = ctx.Auto().InferColumns(dataPaths.First(), labelColumnName: columnInfo.Label, groupColumns: false);

            if (columnInfo.Categorical != null)
                foreach (var name in columnInfo.Categorical)
                    MoveColumnCollection(columnInference.ColumnInformation, name, ci => ci.CategoricalColumnNames);

            if (columnInfo.Ignore != null)
                foreach (var name in columnInfo.Ignore)
                    MoveColumnCollection(columnInference.ColumnInformation, name, ci => ci.IgnoredColumnNames);

            // Column inference may get labelColumn type wrong
            columnInference.TextLoaderOptions.Columns.Single(o => o.Name == columnInfo.Label).DataKind = Microsoft.ML.Data.DataKind.Single;

            var loader = ctx.Data.CreateTextLoader(columnInference.TextLoaderOptions);
            var data = loader.Load(dataPaths);

            return (data, columnInference.ColumnInformation);
        }

        public class Progressor : IProgress<CrossValidationRunDetail<Microsoft.ML.Data.RegressionMetrics>>
        {
            public void Report(CrossValidationRunDetail<Microsoft.ML.Data.RegressionMetrics> value)
            {
            }
        }

        private static async Task<TrialResult> Train(MLContext ctx, IDataView data, ColumnInformation colInfo, string labelColumnName, TimeSpan? trainingTime = null, CancellationToken cancellation = default)
            //, IEnumerable<string>? categoricalColumnNames = null)
        {
            var maxSeconds = (uint)(int)(trainingTime ?? TimeSpan.FromSeconds(60)).TotalSeconds;
            //var result = ctx.Auto()
            //    .CreateRegressionExperiment(new RegressionExperimentSettings { MaxExperimentTimeInSeconds = maxSeconds })
            //    .Execute(trainData: data, numberOfCVFolds: 4, columnInformation: colInfo, progressHandler: new Progressor());
            //mlContext.Auto().CreateRegressionExperiment(new RegressionExperimentSettings { });

            var pipeline = ctx.Auto()
                .Featurizer(data, columnInformation: colInfo)
                .Append(ctx.Auto().Regression(labelColumnName: labelColumnName));

            var experiment = ctx.Auto()
                .CreateExperiment()
                .SetPipeline(pipeline)
                .SetRegressionMetric(RegressionMetric.RSquared, labelColumn: labelColumnName)
                .SetTrainingTimeInSeconds(maxSeconds)
                //.SetTrialRunner
                .SetDataset(data, fold: 4);

            var start = DateTime.Now;
            var messages = new List<string>();
            ctx.Log += (_, e) =>
            {
                //if (e.Kind != Microsoft.ML.Runtime.ChannelMessageKind.Trace)
                if (e.Source.Equals("AutoMLExperiment") &&
                    new[] { "current CPU:", "DefaultPerformanceMonitor has been started" }.Any(e.RawMessage.Contains) == false)
                {
                    var elapsed = DateTime.Now - start;
                    elapsed = new TimeSpan(elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
                    if (e.RawMessage.StartsWith("Update "))
                    {
                        messages.Add($"{elapsed}: {e.RawMessage}");
                    }
                    Console.WriteLine($"{elapsed}: {(e.RawMessage.Length > 200 ? e.RawMessage.Remove(200) : e.RawMessage)}");
                }
                else if (new[] { "Channel started", "Channel finished", "Channel disposed" }.Any(e.RawMessage.Contains))
                { }
                else if (new[] {
                    "[Source=ValueToKeyMappingEstimator",
                    "[Source=Converter", "[Source=ColumnConcatenatingEstimator",
                    "[Source=SelectColumnsDataTransform", "[Source=MissingValueReplacingEstimator", 
                    "[Source=GenerateNumber", "[Source=RangeFilter", "[Source=OneHotEncodingEstimator" }
                    .Any(e.RawMessage.StartsWith))
                { }
                else if (new[] { "[Source=TextLoader" }.Any(e.Message.StartsWith))
                { }
                else
                {
                    //Console.WriteLine(e.RawMessage);
                }
            };

            //experiment.SetTrialRunner
            var experimentResults = await experiment.RunAsync(cancellation);

            Console.WriteLine(string.Join("\n", messages));

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

        public object Predict(object inputObject)
        {
            if (Schema == null) throw new NullReferenceException($"{nameof(Schema)} is null");
            if (Model == null) throw new NullReferenceException($"{nameof(Model)} is null");
            if (string.IsNullOrEmpty(ColInfo.Label)) throw new NullReferenceException($"{nameof(ColInfo.Label)} is null");
            var predictionType = ClassFactory.CreateType(
                    new[] { "Score" },
                    new[] { Schema[ColInfo.Label].Type.RawType });
            return CreateGenericPrediction(ctx, Schema, Model, inputObject, predictionType);
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
