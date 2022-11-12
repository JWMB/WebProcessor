using ProblemSource.Models.LogItems;

namespace ProblemSource.Models.Aggregates
{
    public class Answer
    {
        public int id { get; set; }
        public int problem_id { get; set; }
        public long time { get; set; }
        public bool correct { get; set; }
        public int response_time { get; set; }
        public string answer { get; set; } = string.Empty;
        public int tries { get; set; }

        public static Answer Create(AnswerLogItem answer)
        {
            return new Answer
            {
                answer = answer.answer,
                correct = answer.correct,
                response_time = answer.response_time,
                time = answer.time,
                tries = answer.tries
            };
        }
    }
}
