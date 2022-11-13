namespace ProblemSource.Models.LogItems
{
    public class UserStatePushLogItem : LogItem, IUserGeneratedState
    {
        public ExerciseStats exercise_stats { get; set; } = new ExerciseStats();
        public object? user_data { get; set; }
    }
}
