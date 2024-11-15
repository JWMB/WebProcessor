namespace ProblemSource.Services
{
    public interface ITrainingMessage
    {
        int TrainingId { get; }
    }

    public class TrainingSyncMessage : ITrainingMessage
    {
        public int TrainingId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTimeOffset ClientTimestamp { get; set; }
        public DateTimeOffset ReceivedTimestamp { get; set; }

        public object? Data { get; set; } 
    }
}
