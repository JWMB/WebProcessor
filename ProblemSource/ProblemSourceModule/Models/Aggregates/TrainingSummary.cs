using ProblemSource.Models.Aggregates;

namespace ProblemSourceModule.Models.Aggregates
{
    public class TrainingSummary
    {
        public int Id { get; set; }
        public int TrainedDays { get; set; }
        public decimal AvgResponseMinutes { get; set; }
        public decimal AvgRemainingMinutes { get; set; }
        public decimal AvgAccuracy { get; set; }
        public DateTimeOffset FirstLogin { get; set; }
        public DateTimeOffset LastLogin { get; set; }

        public decimal AvgDaysPerWeek
        {
            get
            {
                if (TrainedDays == 0) return 0;
                var diff = Math.Max(1, (int)(LastLogin - FirstLogin).TotalDays);
                if (diff == 0) return 0;
                return (decimal)TrainedDays / diff * 7;
            }
        }
        

        internal static TrainingSummary Create(int userId, IEnumerable<TrainingDayAccount> trainingDays)
        {
            return new TrainingSummary
            {
                Id = userId,
                TrainedDays = trainingDays.Count(),
                AvgResponseMinutes = trainingDays.Average(o => 1M * o.ResponseMinutes),
                AvgRemainingMinutes = trainingDays.Average(o => 1M * o.RemainingMinutes),
                AvgAccuracy = trainingDays.Average(o => o.NumRaces == 0 ? 0 : 1M * o.NumRacesWon / o.NumRaces),
                FirstLogin = trainingDays.Min(o => o.StartTime),
                LastLogin = trainingDays.Max(o => o.StartTime)
            };
        }
    }
}
