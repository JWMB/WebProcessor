namespace ProblemSourceModule.Models.Aggregates.ML
{
    public interface IMLFeature
    {
        Dictionary<string, object?> GetFlatFeatures();
        bool IsValid { get; }
    }
}
