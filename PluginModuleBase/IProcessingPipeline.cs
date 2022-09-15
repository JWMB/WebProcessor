namespace PluginModuleBase
{
    public interface IProcessingPipeline
    {
        Task<object?> Process(object input);
    }

}