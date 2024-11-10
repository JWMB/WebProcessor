namespace ProblemSourceModule.Services.ProblemGenerators
{
    public interface IStimuliGenerator
    {
        Task<IStimulus> Generate();
    }
}
