//using PluginModuleBase;
//using System.Collections;
//using System.Collections.Concurrent;

//namespace WebApi.Services
//{
//    public class ProcessingPipelineRepository : IProcessingPipelineRepository
//    {
//        private static readonly ConcurrentDictionary<string, IProcessingPipeline> _pipelineMap = new();

//        public Task<IProcessingPipeline?> Get(string? key)
//        {
//            return Task.FromResult(
//                key == null ? null :
//                    _pipelineMap.TryGetValue(key.ToLower(), out var found) ? found : null
//                );
//        }

//        public void Register(string key, IProcessingPipeline pipeline)
//        {
//            _pipelineMap.TryAdd(key.ToLower(), pipeline);
//            //_pipelineMap[key.ToLower()] = pipeline;
//        }
//    }

//    //public Task Test()
//    //{
//    //    IProcessingNode node = null;

//    //    node.Outputs.Single(o => o is IObservable<int>).OnReceiveData
//    //    //node.Inputs.First().WriteAsync
//    //}

//    //public interface IProcessingNode
//    //{
//    //    Dictionary<string, Input> Inputs { get; }
//    //    Dictionary<string, Input> Outputs { get; }
//    //}

//    //public class ConnectorCollection : IEnumerable<Connector<object>>
//    //{
//    //    public IEnumerator<Connector<object>> GetEnumerator()
//    //    {
//    //        throw new NotImplementedException();
//    //    }

//    //    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//    //}
//    //public class Connector<T> : IObservable<T>
//    //{
//    //    public event EventHandler OnReceiveData;

//    //    public IDisposable Subscribe(IObserver<T> observer)
//    //    {
//    //        throw new NotImplementedException();
//    //    }
//    //}

//    //public class XNode<TInput, TOutput>
//    //{
//    //}
//    //public class SsNode : XNode<XInputs, XInputs>
//    //{
//    //}
//    //public class XInputs
//    //{
//    //    public IntInput Height { get; set; } = new();
//    //}
//    //public class IntInput : Input<int>
//    //{ }

//    //public class Input
//    //{ }
//    //public class Input<T>
//    //{

//    //}
//}
