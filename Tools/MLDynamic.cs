using Common;
using Microsoft.ML;
using Microsoft.ML.AutoML;
using System.Data;
using System.Reflection;
using System.Xml.Serialization;

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
            public string? UserId { get; set; }
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

        public interface IExperimentConfig
        {
            SweepablePipeline ConfigurePipeline(MLContext ctx, SweepablePipeline pipeline);
            //ISweepable<IEstimator<ITransformer>> Pipio(MLContext ctx);
            AutoMLExperiment ConfigureMetric(AutoMLExperiment exp);
        }

        public class RegressionExperimentConfig : IExperimentConfig
        {
            private readonly ColumnInfo colInfo;
            public RegressionExperimentConfig(ColumnInfo colInfo)
            {
                this.colInfo = colInfo;
            }
            public SweepablePipeline ConfigurePipeline(MLContext ctx, SweepablePipeline pipeline)
            {
                return pipeline
                    .Append(ctx.Auto().Regression(labelColumnName: colInfo.Label));
            }
            //public ISweepable<IEstimator<ITransformer>> Pipio(MLContext ctx) => ctx.Auto().Regression(labelColumnName: colInfo.Label);
            public AutoMLExperiment ConfigureMetric(AutoMLExperiment exp) => exp.SetRegressionMetric(RegressionMetric.RSquared, labelColumn: colInfo.Label);
        }

        public class MultiClassificationExperimentConfig : IExperimentConfig
        {
            private readonly ColumnInfo colInfo;
            public MultiClassificationExperimentConfig(ColumnInfo colInfo)
            {
                this.colInfo = colInfo;
            }
            public SweepablePipeline ConfigurePipeline(MLContext ctx, SweepablePipeline pipeline)
            {
                return pipeline
                    .Append(ctx.Transforms.Conversion.MapValueToKey(colInfo.Label))
                    .Append(ctx.Auto().MultiClassification(labelColumnName: colInfo.Label));
            }
            //public ISweepable<IEstimator<ITransformer>> Pipio(MLContext ctx) => ctx.Auto().MultiClassification(labelColumnName: colInfo.Label);
            public AutoMLExperiment ConfigureMetric(AutoMLExperiment exp) => exp.SetMulticlassClassificationMetric(MulticlassClassificationMetric.LogLoss, labelColumn: colInfo.Label);
        }

        public async Task Train(IExperimentConfig config, TimeSpan? trainingTime = null, CancellationToken cancellation = default)
        {
            // https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/how-to-use-the-automl-api

            if (DataView == null) throw new NullReferenceException(nameof(DataView));
            if (ColumnInformation == null) throw new NullReferenceException(nameof(ColumnInformation));

            var experiment = ConfigureExperiment(ctx, DataView, ColumnInformation,
                config.ConfigurePipeline, config.ConfigureMetric, fold: 4,
                trainingTime: trainingTime);

            var trialResult = await experiment.RunAsync(cancellation);

            if (trialResult == null)
                throw new NullReferenceException(nameof(trialResult));

            Model = trialResult.Model;

            var info = $"{nameof(trialResult.Metric)}:{trialResult.Metric}, {nameof(trialResult.Loss)}:{trialResult.Loss}";

            //CalcFeatureImportance(ctx, model, result.data, labelColumnName);
        }

        public void Save(string modelPath)
        {
            ctx.Model.Save(Model, Schema, modelPath);
        }

        public void CalcFeatureImportance(MLContext ctx, ITransformer model, IDataView data, string labelColumnName)
        {
            // https://github.com/dotnet/machinelearning-samples/issues/783
            var transformedData = model.Transform(data);
            var pfi = ctx.Regression.PermutationFeatureImportance(model, transformedData, permutationCount: 3, labelColumnName: labelColumnName);
            var featureImportance = pfi.Select(x => Tuple.Create(x.Key, x.Value.RSquared))
                .OrderByDescending(x => x.Item2);
        }

        public void LoadData(string[] dataPaths, Action<Microsoft.ML.Data.TextLoader.Column[]>? modifyColumns = null)
        {
            var (data, colInfo) = LoadData(ctx, dataPaths, ColInfo, modifyColumns);
            DataView = data;
            Schema = data.Schema;
            ColumnInformation = colInfo;
        }

        private static (IDataView, ColumnInformation) LoadData(MLContext ctx, string[] dataPaths, ColumnInfo columnInfo, Action<Microsoft.ML.Data.TextLoader.Column[]>? modifyColumns = null)
        {
            var columnInference = ctx.Auto().InferColumns(dataPaths.First(), labelColumnName: columnInfo.Label, groupColumns: false);

            if (columnInfo.Categorical != null)
                foreach (var name in columnInfo.Categorical)
                    MoveColumnCollection(columnInference.ColumnInformation, name, ci => ci.CategoricalColumnNames);

            if (columnInfo.Ignore != null)
                foreach (var name in columnInfo.Ignore)
                    MoveColumnCollection(columnInference.ColumnInformation, name, ci => ci.IgnoredColumnNames);

            if (columnInfo.UserId != null)
                columnInference.ColumnInformation.UserIdColumnName = columnInfo.UserId;

            columnInference.ColumnInformation.LabelColumnName = columnInfo.Label;

            // Column inference may get labelColumn type wrong, modifyColumns provides a way to correct this
            modifyColumns?.Invoke(columnInference.TextLoaderOptions.Columns);

            var loader = ctx.Data.CreateTextLoader(columnInference.TextLoaderOptions);
            var data = loader.Load(dataPaths);

            return (data, columnInference.ColumnInformation);
        }

        //public class Progressor : IProgress<CrossValidationRunDetail<Microsoft.ML.Data.RegressionMetrics>>
        //{
        //    public void Report(CrossValidationRunDetail<Microsoft.ML.Data.RegressionMetrics> value) { }
        //}

        private static AutoMLExperiment ConfigureExperiment(MLContext ctx, IDataView data, ColumnInformation colInfo,
            Func<MLContext, SweepablePipeline, SweepablePipeline> configurePipeline,
            Func<AutoMLExperiment, AutoMLExperiment> configureMetric,
            int fold = 4, TimeSpan? trainingTime = null)
        {
            var maxSeconds = (uint)(int)(trainingTime ?? TimeSpan.FromSeconds(60)).TotalSeconds;

            var pipeline = ctx.Auto()
                .Featurizer(data, columnInformation: colInfo);

            pipeline = configurePipeline(ctx, pipeline);

            var experiment = ctx.Auto()
                .CreateExperiment()
                .SetPipeline(pipeline)
                .SetTrainingTimeInSeconds(maxSeconds)
                .SetDataset(data, fold: fold);

            experiment = configureMetric(experiment);

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
            return experiment;
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

        private const string DefaultScoreColumn = "Score";

        public object Predict(object inputObject, string scoreColumn = DefaultScoreColumn)
        {
            if (Schema == null) throw new NullReferenceException($"{nameof(Schema)} is null");
            if (Model == null) throw new NullReferenceException($"{nameof(Model)} is null");
            if (string.IsNullOrEmpty(ColInfo.Label)) throw new NullReferenceException($"{nameof(ColInfo.Label)} is null");

            var predictionType = DynamicTypeFactory.CreateType(
                    new[] { scoreColumn },
                    new[] { Schema[ColInfo.Label].Type.RawType });
            return CreateGenericPrediction(ctx, Schema, Model, inputObject, predictionType, scoreColumn);
        }

        private static object CreateGenericPrediction(MLContext mlContext, DataViewSchema dataViewSchema, ITransformer model, object inputObject, Type predictionType, string scoreColumn = DefaultScoreColumn)
        {
            // Create runtime type from fields and types in a DataViewSchema
            var methodName = "CreatePredictionEngine";
            var genericPredictionMethod = mlContext.Model.GetType().GetMethod(methodName, new[] { typeof(ITransformer), typeof(DataViewSchema) });
            if (genericPredictionMethod == null)
                throw new Exception($"'{methodName}' not found");

            var runtimeType = CreateType(dataViewSchema);
            if (runtimeType == null)
                throw new Exception("Could not create type");

            var predictionMethod = genericPredictionMethod.MakeGenericMethod(runtimeType, predictionType);
            // InvalidOperationException: Can't bind the IDataView column 'Score' of type 'Vector<Single, 6>' to field or property 'Score' of type 'System.UInt32'.
            dynamic? dynamicPredictionEngine = predictionMethod.Invoke(mlContext.Model, new object[] { model, dataViewSchema });
            if (dynamicPredictionEngine == null)
                throw new Exception($"Could not create {predictionMethod.Name}");

            var inputInstance = DynamicTypeFactory.CreateInstance(runtimeType, inputObject);

            var predictMethod = dynamicPredictionEngine.GetType().GetMethod("Predict", new[] { runtimeType });
            var predictionResult = predictMethod.Invoke(dynamicPredictionEngine, new[] { inputInstance });
            return predictionType.GetProperty(scoreColumn)!.GetValue(predictionResult);
        }

        public static Type CreateType(DataViewSchema dataViewSchema)
        {
            var propTypes = dataViewSchema.ToDictionary(o => o.Name, o => o.Type.RawType);
            return DynamicTypeFactory.CreateType(propTypes);
        }
    }
}
