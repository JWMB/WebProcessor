namespace PluginModuleBase
{
    public interface IProcessingMiddlewarePipelineRepository
    {
        void Register(string key, IProcessingMiddleware pipeline);
        Task<IProcessingMiddleware?> Get(string? key);
    }
}
