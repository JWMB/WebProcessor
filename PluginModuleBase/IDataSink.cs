namespace PluginModuleBase
{
    public interface IDataSink
    {
        Task Log(string uuid, object data);
	}

	public class NullDataSink : IDataSink
	{
		public Task Log(string uuid, object data) => Task.CompletedTask;
	}
}
