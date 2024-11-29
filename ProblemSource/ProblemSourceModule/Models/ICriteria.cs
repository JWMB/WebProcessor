using Newtonsoft.Json.Linq;
using ProblemSource.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProblemSource.Models.ExerciseStats;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProblemSourceModule.Models
{
    public interface ICriteria
    {
        bool isFulfilled(ExerciseStats stats);
    }

    //Todo: And/or will generate different results if their lists are empty, what does a empty list mean?
    public class BooleanAndCriteria : ICriteria
    {
        public ICriteria[] subCriteria = [];
        public bool isFulfilled(ExerciseStats stats)
		{
			return subCriteria.Any() == false
				? false// Todo: Keep or discard this?
				: subCriteria.Any(o => o.isFulfilled(stats) == false) == false;
		}
	}

    public class BooleanOrCriteria : ICriteria
    {
        public ICriteria[] subCriteria = [];
        public bool isFulfilled(ExerciseStats stats)
        {
            return subCriteria.Any() == false
                ? false // Todo: Keep or discard this?
                : subCriteria.Any(o => o.isFulfilled(stats) == true) == true;
        }
    }

    public class BooleanNotCriteria : ICriteria
    {
        public ICriteria? subCriteria;
        public bool isFulfilled(ExerciseStats stats) => subCriteria == null || !subCriteria.isFulfilled(stats);
    }

    public enum MagnitudeComparisonType
    {
        Equal,
        NotEqual,
        Greater,
        Smaller,
        EqualOrGreater,
        EqualOrSmaller,
    }

    //Todo: Use bit flags?
    public class MagnitudeComparer
    {
        public static bool compare(MagnitudeComparisonType comparisonType, decimal value1, decimal value2)
        {
            if (comparisonType == MagnitudeComparisonType.Equal && value1 == value2)
                return true;
            if (comparisonType == MagnitudeComparisonType.NotEqual && value1 != value2)
                return true;
            if (comparisonType == MagnitudeComparisonType.Greater && value1 > value2)
                return true;
            if (comparisonType == MagnitudeComparisonType.Smaller && value1 < value2)
                return true;
            if (comparisonType == MagnitudeComparisonType.EqualOrGreater && value1 >= value2)
                return true;
            if (comparisonType == MagnitudeComparisonType.EqualOrSmaller && value1 <= value2)
                return true;
            return false;
        }
    }

    public class ExerciseLevelCriteria : ICriteria
    {
        public string exerciseId = "";
		public decimal level = 0;
		public MagnitudeComparisonType magnitudeComparison = MagnitudeComparisonType.EqualOrGreater;

        public bool isFulfilled(ExerciseStats stats)
        {
            return MagnitudeComparer.compare(magnitudeComparison, stats.GetGameStatsSharedId(exerciseId).lastLevel, level );
        }
    }

    public class ExerciseResponseTimeCriteria : ICriteria
    {
        public string exerciseId = "";
        public long time = 0;
        public MagnitudeComparisonType magnitudeComparison = MagnitudeComparisonType.EqualOrGreater;

        public bool isFulfilled(ExerciseStats stats)
        {
            return MagnitudeComparer.compare(magnitudeComparison, stats.GetGameStatsSharedId(exerciseId).trainingTime, time * 1000 );
        }
    }

    public class ExerciseLevelHighestCriteria : ICriteria
    {
        public string exerciseId = "";
        public decimal level = 0;
        public MagnitudeComparisonType magnitudeComparison = MagnitudeComparisonType.EqualOrGreater;

        public bool isFulfilled(ExerciseStats stats)
        {
            return MagnitudeComparer.compare(magnitudeComparison, stats.GetGameStatsSharedId(exerciseId).highestLevel, level);
        }
    }

    public class PlanetCompleteCriteria : ICriteria
    {
        public string exerciseId = "";
        //public magnitudeComparison: MagnitudeComparisonType = MagnitudeComparisonType.EqualOrGreater;

        public bool isFulfilled(ExerciseStats stats)
        {
            //stats.GetGameStats(exerciseId)
            var planets = PlanetBundler.getPlanetsOnlyUsedGameId(exerciseId, false);
            return planets?.FirstOrDefault(o => o.isCompleted) != null; //planets.length ? planets.findFirst(_ => _.isCompleted) != null : false;
            //return MagnitudeComparer.compare(this.magnitudeComparison, stats.getGameStatsSharedId(this.exerciseId).numRuns, this.runs);
        }
    }

    public class ExerciseRunsCriteria : ICriteria
    {
        public string exerciseId = "";
		public int runs  = 0;
		public MagnitudeComparisonType magnitudeComparison = MagnitudeComparisonType.EqualOrGreater;

        public bool isFulfilled(ExerciseStats stats)
        {
            return MagnitudeComparer.compare(magnitudeComparison, stats.GetGameStatsSharedId(exerciseId).numRuns, runs);
        }
    }

    public class ExerciseUnlocksCriteria : ICriteria
    {
        public string exerciseId = "";
		public int unlocks = 0;
		public MagnitudeComparisonType magnitudeComparison = MagnitudeComparisonType.EqualOrGreater;

        public bool isFulfilled(ExerciseStats stats)
        {
            //TODO: this criterion /note SPELLING/ is number of unlocked islands/planets?
            //Seems to be used in #intro / regular pairs, for locking #intro, but at the same moment the regular uses ExerciseRunsCriteria to unlock..?
            return MagnitudeComparer.compare(magnitudeComparison,
                stats.GetGameStats(exerciseId).numRuns,
                //Object.keys(stats.getExerciseStatsById(this.exerciseId)).length,
                //Object.keys(stats.tests).filter(o => o.substr(0, this.exerciseId.length) == this.exerciseId).length,
                unlocks);
        }
    }

    // Doesn't seem to be used
    //public class TimeCriteria : ICriteria
    //{
    //    public long time = 0;
    //    public MagnitudeComparisonType magnitudeComparison = MagnitudeComparisonType.EqualOrGreater;

    //    public bool isFulfilled(ExerciseStats stats)
    //    {
    //        return MagnitudeComparer.compare(magnitudeComparison, Date.now() - GameState.trainingTimeStartTime, time * 1000);
    //        //TODO: GameState static reference
    //    }
    //}

    public class DayCriteria : ICriteria
    {
        public int day = 0;
        public MagnitudeComparisonType magnitudeComparison = MagnitudeComparisonType.EqualOrGreater;

        public bool isFulfilled(ExerciseStats stats)
        {
            //TODO: used GameState.getTrainingDay() GameState static reference
            // TODO: if we've just entered main screen in a new session, trainingDay will show last training day...
            var trainingDay = stats.GetAllGameStats().Values.SelectMany(o => o.Select(p => p.trainingDay)).Max();
            return MagnitudeComparer.compare(magnitudeComparison, trainingDay, day);
        }
    }


    public class FulfilledCriteria : ICriteria
    {
        public bool isFulfilled(ExerciseStats stats) => true;
    }

    public class RejectedCriteria : ICriteria
    {
        public bool isFulfilled(ExerciseStats stats) => false;
    }

    /*
	//Todo: Seal Criterias into namespace to prevent accessing other types of classes by mistake
    */
}
