namespace ProblemSource.Services
{
    public interface IEventDispatcher
    {
        public Task Dispatch(object o);
    }

    public class NullEventDispatcher : IEventDispatcher
    {
        public Task Dispatch(object o) => Task.CompletedTask;
    }
}
