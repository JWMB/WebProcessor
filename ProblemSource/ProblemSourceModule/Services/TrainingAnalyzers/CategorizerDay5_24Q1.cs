using ProblemSource.Models;
using Microsoft.Extensions.Logging;
using static ProblemSourceModule.Services.TrainingAnalyzers.CategorizerDay5_23Q1;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
    public class CategorizerDay5_24Q1 : CategorizerDay5_23Q1
    {
        public CategorizerDay5_24Q1(IPredictNumberlineLevelService modelService, ILogger<CategorizerDay5_23Q1> log)
            : base(modelService, log)
        { }

        protected override dynamic? CreateTrigger(int triggerDay, PredictedNumberlineLevel.PerformanceTier tier, (double, double) rnds) =>
            new TrainingModCreator_24Q1().CreateTrigger(triggerDay, tier, rnds);
    }

    public class TrainingModCreator_24Q1 : ITrainingModCreator
    {
        public dynamic? CreateTrigger(int triggerDay, PredictedNumberlineLevel.PerformanceTier tier, (double, double) rnds)
        {
            var plan = rnds.Item1 switch
            {
                < 0.33 => Plans.NVR_Std,
                < 0.66 => Plans.NVR_High,
                _ => Plans.WM_Std
            };
            var trigger = TrainingSettings.CreateWeightChangeTrigger(plan, triggerDay);

            if (tier == PredictedNumberlineLevel.PerformanceTier.Low)
            {
                if (rnds.Item2 < 0.5)
                {
                    trigger.actionData.properties.phases = TrainingSettings.ConvertToDynamicOrThrow(new Dictionary<string, object> {
                        {
                            "numberline[\\w#]*",
                            new { problemGeneratorData = new { problemFile = new { path = "numberline_easy_ola_q123.csv" } } } // Note: client updated to include this file
                        } });
                }
            }
            return trigger;
        }
    }
}
