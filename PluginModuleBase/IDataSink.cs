namespace PluginModuleBase
{
    public interface IDataSink
    {
        Task Log(string uuid, object data);
    }
}
