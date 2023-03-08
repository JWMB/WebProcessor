namespace ML.Helpers
{
    public interface IMLPredictor
    {
        Task<float?> Predict(ColumnInfo colInfo, Dictionary<string, object?> parameters);
    }
}
