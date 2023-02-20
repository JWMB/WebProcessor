namespace ProblemSource.Models.Aggregates
{
    public interface IMLFeature
    {
        Dictionary<string, object?> GetFlatFeatures();
        bool IsValid { get; }
    }
}
