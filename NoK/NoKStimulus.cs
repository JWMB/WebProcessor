using ProblemSourceModule.Services.ProblemGenerators;

namespace NoK
{
    public class NoKStimulus : IStimulus
    {
        public string Id { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;

        public string Presentation { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }
}
