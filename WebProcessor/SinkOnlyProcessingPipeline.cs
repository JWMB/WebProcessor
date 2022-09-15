using PluginModuleBase;

namespace WebApi
{
    public class SinkOnlyProcessingPipeline : IProcessingPipeline
    {
        private readonly IDataSink sink;

        public SinkOnlyProcessingPipeline(IDataSink sink)
        {
            this.sink = sink;
        }

        public async Task<object?> Process(object input)
        {
            // TODO: a client and/or user id
            await sink.Log("unknown", input);
            return null;
        }
    }
}
