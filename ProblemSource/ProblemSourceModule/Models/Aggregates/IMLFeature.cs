namespace ProblemSource.Models.Aggregates
{
    public interface IMLFeature
    {
        Dictionary<string, object?> GetFlatFeatures();
        Dictionary<string, string> InvalidReasons { get; }

        public bool IsValid => InvalidReasons.Any() == false;
    }
}
