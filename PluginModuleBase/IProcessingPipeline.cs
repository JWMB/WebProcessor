namespace PluginModuleBase
{
    public interface IProcessingPipeline
    {
        Task<object?> Process(object input, System.Security.Claims.ClaimsPrincipal? user);
    }

}