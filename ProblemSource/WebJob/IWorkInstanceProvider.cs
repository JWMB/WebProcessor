namespace WebJob
{
    public interface IWorkInstanceProvider
    {
        IEnumerable<WorkBase> Get();
    }
}
