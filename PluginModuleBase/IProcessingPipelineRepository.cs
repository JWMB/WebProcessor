namespace PluginModuleBase
{
    public interface IProcessingPipelineRepository
    {
        void Register(string key, IProcessingPipeline pipeline);
        Task<IProcessingPipeline?> Get(string? key);
    }
}
