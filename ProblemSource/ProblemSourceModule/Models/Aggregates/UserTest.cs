using ProblemSource.Models.LogItems;

namespace ProblemSource.Models.Aggregates
{
    public class UserTest
    {
        public int score { get; set; }
        public int target_score { get; set; }
        public int planet_target_score { get; set; }
        public bool won_race { get; set; }
        public bool completed_planet { get; set; }
        public bool? ended { get; set; }

        public static UserTest Create(PhaseEndLogItem phaseEnd)
        {
            return new UserTest
            {
                completed_planet = phaseEnd.completedPlanet,
                planet_target_score = (int)phaseEnd.planetTargetScore,
                score = (int)phaseEnd.score,
                target_score = (int)phaseEnd.targetScore,
                won_race = phaseEnd.wonRace,
                ended = true // TODO: ?
            };
        }
    }
}
