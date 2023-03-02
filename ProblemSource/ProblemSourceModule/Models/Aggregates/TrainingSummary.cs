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

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is TrainingSummary typed == false)
                return false;
            return TrainedDays == typed.TrainedDays && LastLogin == typed.LastLogin && Id == typed.Id;
        }


        internal static TrainingSummary Create(int userId, IEnumerable<TrainingDayAccount> trainingDays)
        {
            var result = new TrainingSummary
            {
                Id = userId,
                TrainedDays = trainingDays.Count()
            };

            if (trainingDays.Any())
            {
                result.AvgResponseMinutes = trainingDays.Average(o => 1M * o.ResponseMinutes);
                result.AvgRemainingMinutes = trainingDays.Average(o => 1M * o.RemainingMinutes);
                result.AvgAccuracy = trainingDays.Average(o => o.NumRaces == 0 ? 0 : 1M * o.NumRacesWon / o.NumRaces);
                result.FirstLogin = trainingDays.Min(o => o.StartTime);
                result.LastLogin = trainingDays.Max(o => o.StartTime);
            }

            result.FirstLogin = EnsureSerializableDateTime(result.FirstLogin);
            result.LastLogin = EnsureSerializableDateTime(result.LastLogin);

            return result;

            // TODO: this is a quick fix - move this to ExpandableTableEntityConverter (AzureTable-specific handling logic)
            DateTimeOffset EnsureSerializableDateTime(DateTimeOffset value) => value.Year >= 1900 ? value : new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
