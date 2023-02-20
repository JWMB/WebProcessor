using Common;
using Microsoft.ML;
using ML.Helpers;
using System.Data;

namespace ML.Dynamic
{
    public class MLDynamicPredict
    {
        private const string DefaultScoreColumn = "Score";
        private readonly MLContext ctx;
        private readonly DataViewSchema schema;
        private readonly ColumnInfo colInfo;
        private readonly ITransformer model;

        public MLDynamicPredict(DataViewSchema schema, ITransformer model, ColumnInfo colInfo)
        {
            this.ctx = new MLContext(seed: 0);
            this.schema = schema;
            this.colInfo = colInfo;
            this.model = model;
        }

        public IEnumerable<object> Predict(IEnumerable<object> inputObjects, string scoreColumn = DefaultScoreColumn)
        {
            if (schema == null) throw new NullReferenceException($"{nameof(schema)} is null");
            if (model == null) throw new NullReferenceException($"{nameof(model)} is null");
            if (string.IsNullOrEmpty(colInfo.Label)) throw new NullReferenceException($"{nameof(colInfo.Label)} is null");

            var predictionType = DynamicTypeFactory.CreateType(
                    new[] { scoreColumn },
                    new[] { schema[colInfo.Label].Type.RawType });

            var inputType = inputObjects.First().GetType(); // CreateType(Schema);
            return CreateGenericPrediction(ctx, schema, model, inputObjects, predictionType, inputType, scoreColumn);
        }

        public object Predict(object inputObject, string scoreColumn = DefaultScoreColumn) =>
            Predict(new[] { inputObject }, scoreColumn).First();

        private static IEnumerable<object> CreateGenericPrediction(MLContext mlContext, DataViewSchema dataViewSchema, ITransformer model,
            IEnumerable<object> inputObjects, Type predictionType, Type? inputType = null, string scoreColumn = DefaultScoreColumn)
        {
            // Create runtime type from fields and types in a DataViewSchema
            var methodName = "CreatePredictionEngine";
            var genericPredictionMethod = mlContext.Model.GetType().GetMethod(methodName, new[] { typeof(ITransformer), typeof(DataViewSchema) });
            if (genericPredictionMethod == null)
                throw new Exception($"'{methodName}' not found");

            var inputTypeProvided = inputType != null;
            inputType ??= CreateType(dataViewSchema);
            if (inputType == null)
                throw new Exception("Could not create type");

            var predictionMethod = genericPredictionMethod.MakeGenericMethod(inputType, predictionType);
            // InvalidOperationException: Can't bind the IDataView column 'Score' of type 'Vector<Single, 6>' to field or property 'Score' of type 'System.UInt32'.
            dynamic? dynamicPredictionEngine = predictionMethod.Invoke(mlContext.Model, new object[] { model, dataViewSchema });
            if (dynamicPredictionEngine == null)
                throw new Exception($"Could not create {predictionMethod.Name}");

            var predictMethod = dynamicPredictionEngine.GetType().GetMethod("Predict", new[] { inputType });

            return inputObjects.Select(o => {
                var inputInstance = inputTypeProvided ? o : DynamicTypeFactory.CreateInstance(inputType, o);
                var predictionResult = predictMethod.Invoke(dynamicPredictionEngine, new[] { inputInstance });
                return predictionType.GetProperty(scoreColumn)!.GetValue(predictionResult);
            });
        }

        public static Type CreateType(DataViewSchema dataViewSchema)
        {
            var propTypes = dataViewSchema.ToDictionary(o => o.Name, o => o.Type.RawType);
            return DynamicTypeFactory.CreateType(propTypes);
        }
    }
}
