namespace WebJob.Works
{
    public class DummyWork : WorkBase
    {
        public override Task Run() => Task.CompletedTask;

        public override bool ShouldRun() => true;
    }
}
