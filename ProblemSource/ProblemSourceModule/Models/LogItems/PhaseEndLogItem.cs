namespace ProblemSource.Models.LogItems
{
    public class PhaseEndLogItem : LogItem
    {
        public static string _type = "PHASE_END";
        public string phase { get; set; } = "";
        public int noOfQuestions { get; set; }
        public int noOfCorrect { get; set; } //corrects
        public int noOfIncorrect { get; set; } //incorrects

        public decimal score { get; set; }
        public decimal targetScore { get; set; }
        public decimal planetTargetScore { get; set; }
        public bool wonRace { get; set; }
        public bool completedPlanet { get; set; }
    }
}
