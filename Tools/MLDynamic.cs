using Microsoft.ML;
using Microsoft.ML.AutoML;
using ProblemSourceModule.Models.Aggregates.ML;
using System.Data;
using static ProblemSourceModule.Models.Aggregates.ML.ColumnTypeAttribute;

namespace Tools
{
    public class ColumnInfo
    {
        public string Label { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public IEnumerable<string>? Categorical { get; set; }
        public IEnumerable<string>? Ignore { get; set; }

        public static ColumnInfo Create(Type type)
        {
            var columnTypePerProperty = type
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .ToDictionary(o => o.Name, o => (Attribute.GetCustomAttribute(o, typeof(ColumnTypeAttribute)) as ColumnTypeAttribute)?.Type);

            return new ColumnInfo
            {
                Label = columnTypePerProperty.Single(o => o.Value == ColumnType.Label).Key,
                Categorical = columnTypePerProperty.Where(o => o.Value == ColumnType.Categorical).Select(o => o.Key),
                Ignore = columnTypePerProperty.Where(o => o.Value == ColumnType.Ignored).Select(o => o.Key),
                UserId = columnTypePerProperty.SingleOrDefault(o => o.Value == ColumnType.UserId).Key,
            };
        }
    }

    public class MLDynamic
    {
        private readonly MLContext ctx;
        public ITransformer? Model { get; private set; }
        public DataViewSchema? Schema { get; private set; }
        public IDataView? DataView { get; private set; }
        public ColumnInformation? ColumnInformation { get; private set; }
        private ColumnInfo ColInfo { get; set; }

        public MLDynamic(ColumnInfo columnInfo, MLContext? ctx = null)
        {
            this.ctx = ctx ?? new MLContext(seed: 0);

            ColInfo = columnInfo;
        }

        public interface IExperimentConfig
        {
            SweepablePipeline ConfigurePipeline(MLContext ctx, SweepablePipeline pipeline);
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

            //CalcFeatureImportance(DataView);
        }

        public void Save(string modelPath)
        {
            ctx.Model.Save(Model, Schema, modelPath);
        }

        public void CalcFeatureImportance(IDataView data)
        {
            // https://github.com/dotnet/machinelearning-samples/issues/783
            var transformedData = Model.Transform(data);
            var pfi = ctx.Regression.PermutationFeatureImportance(Model, transformedData, permutationCount: 3, labelColumnName: ColInfo.Label);
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
                    new[] { "current CPU:", "DefaultPerformanceMonitor has been started", "trial setting - ", "Update Running Trial - " }.Any(e.RawMessage.Contains) == false)
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
    }
}
