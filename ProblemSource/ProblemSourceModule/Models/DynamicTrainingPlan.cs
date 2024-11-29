using Common;
using Newtonsoft.Json.Linq;
using ProblemSourceModule.Models;
using System.Dynamic;

namespace ProblemSource.Models
{
    public class DynamicTrainingPlan : TrainingPlan
    {
        public enum ValueTypeEnum
        {
            Runs,
            Time,
            Wins,
            Level,
        }
        public ValueTypeEnum valueType { get; set; } = ValueTypeEnum.Runs;

        public int minUnlockedExercises { get; set; } = 1;
        public int maxUnlockedExercises { get; set; } = 1;

        public List<GameDefinition> hiddenExercises { get; set; } = new();
        public bool preventSameExerciseTwice { get; set; }

        public List<Group> groups { get; set; } = new();

        public class Group
        {
            public string id { get; set; } = "";
            public decimal weight { get; set; }
            public List<dynamic> exercises { get; set; } = new();
        }

        private DynamicNodeParent treeRoot = new();

        public override List<ProposedGameInfo> getProposedGames(List<string> proposedGameIds, ExerciseStats stats)
        {
            if (stats.gameRuns.Any())
            {
                //if this was first time the game was used, the following will adjust amassed weight values
                treeRoot.SetAdjustmentIfFirstTime(stats.gameRuns[^1].gameId, stats);
            }

            //Remove those that haven't been unlocked or have been locked
            //TODO: wait, only those that have been locked? We might have "decidedFutureExercise", but not unlocked it - don't remove those, right..?
            proposedGameIds = proposedGameIds.Where(id => {
                var found = treeRoot.GetLeafById(id); //var gameDef = this.getGameDefById(id);
                return found?.IsUnlockedNotLocked(stats) == true;
            }).ToList();

            if (proposedGameIds.Count < minUnlockedExercises)
            {
                var nextLeaf = treeRoot.GetNextLeaf(stats);
                if (nextLeaf?.Game == null)
                {
                    throw new Exception("nextLeaf is null");
                }
                //if (DynamicTrainingPlan._dbgLastGameId == nextLeaf.Game.id)
                //    //var tmp = Logger.fullLogAsString;
                //DynamicTrainingPlan._dbgLastGameId = nextLeaf.game.id;
                var upcoming = new List<string> { nextLeaf.Game.id };
                //Remove new upcoming games that were already in proposedGameIds:
                upcoming = upcoming.Where(_ => proposedGameIds.IndexOf(_) < 0).ToList();
                proposedGameIds.AddRange(upcoming);
                if (proposedGameIds.Count < minUnlockedExercises)
                {
                    //console.warn("proposedGameIds.length < this.minUnlockedExercises", proposedGameIds.length, this.minUnlockedExercises);
                }
            }
            return proposedGameIds.Select(_ => new ProposedGameInfo(_, true)).ToList();
        }

        public List<GameDefinition> getAvailableGames(ExerciseStats stats)
        {
            var leaves = treeRoot.GetAvailableLeaves(stats);
            return leaves.Select(_ => _.Game).ToList();
        }

        public override List<GameDefinition> getDefinedGames()
        {
            var inTree = treeRoot.GetLeaves().Select(_ => _.Game);
            foreach (var item in inTree)
                item.invisible = false;
            foreach (var item in hiddenExercises)
                item.invisible = true;

            return inTree.Concat(hiddenExercises).ToList();
        }

        public void setWeightsFromPreviousChange(ExerciseStats stats, Dictionary<string, decimal> newTargetWeights)
        {
            foreach (var kv in newTargetWeights)
            {
                var node = treeRoot.GetDescendantById(kv.Key);
                if (node != null)
                    node.weight = kv.Value;
            }
        }

        public void changeWeights(ExerciseStats stats, Dictionary<string, decimal> newTargetWeights) {
            //Called from e.g. trigger.
            //if (!stats.trainingPlanSettings.groupWeightChanges) {
            //    stats.trainingPlanSettings.groupWeightChanges = [];
            //}
            var allGroupsId = "ALL_GROUPS";
            if (newTargetWeights.TryGetValue(allGroupsId, out var allGroups))
            {
                var tmp = treeRoot.Children.ToDictionary(o => o.id, o => allGroups);
                foreach (var kv in newTargetWeights.Where(o => o.Key != allGroupsId))
                    tmp.Add(kv.Key, kv.Value);
                newTargetWeights = tmp;
            }
            //if (!stats.trainingPlanSettings.changes) {
            //    stats.trainingPlanSettings.changes = [];
            //}
            //stats.trainingPlanSettings.changes.push(<TrainingPlanChange>{ timestamp: Date.now(), type: ObjectUtils.getClassNameFromConstructor(TrainingWeightChangeTriggerAction), change: newTargetWeights });
            treeRoot.ChangeWeights(stats, newTargetWeights);
        }


        public static DynamicTrainingPlan Create(JObject obj)
        {
            var dp = obj.ToObject<DynamicTrainingPlan>();
            if (dp == null)
                throw new Exception("Couldn't deserialize TrainingPlan");

            foreach (var group in dp.groups)
            {
                var node = new DynamicNodeParent { id = group.id, weight = group.weight };
                node.Parent = dp.treeRoot;

                foreach (var item in group.exercises)
                {
                    var gameDef = ((JObject)item.data).ToObject<GameDefinition>();
                    if (gameDef == null)
                        throw new Exception("Couldn't deserialize GameDefinition");
                    var leaf = new DynamicNodeLeaf { id = (string)item["id"], weight = Convert.ToDecimal(item["weight"]), Game = gameDef };
                    leaf.unlockCriteria = ParseCriteria(item.unlockCriteria);
                    leaf.lockCriteria = ParseCriteria(item.lockCriteria);

                    leaf.Parent = node;
                }
            }
            return dp;

            ICriteria? ParseCriteria(dynamic? value)
            {
                if (value == null) return null;
                switch ((string)value.__type)
                {
                    case "PlanetCompleteCriteria": return new PlanetCompleteCriteria { exerciseId = (string)value.exerciseId };
                    case "DayCriteria": return new DayCriteria { day = Convert.ToInt32(value.day) };
                    case "ExerciseLevelHighestCriteria":return new ExerciseLevelHighestCriteria { exerciseId = (string)value.exerciseId, level = Convert.ToDecimal(value.level) };
                    default: throw new NotImplementedException($"{value.__type}");
                }
            }
        }
    }

    public abstract class DynamicNode
    {
        public string id = "";
        public decimal weight;

        public ICriteria? unlockCriteria;
        public ICriteria? lockCriteria;

        public bool IsUnlockedNotLocked(ExerciseStats stats)
        {
            return (unlockCriteria == null || unlockCriteria.isFulfilled(stats))
                && (lockCriteria == null || lockCriteria.isFulfilled(stats));
        }
        protected DynamicNodeParent? _parent;
        public DynamicNodeParent? Parent
        { 
            set
            {
                if (_parent != null)
                    _parent.Children.Remove(this);

                _parent = value;

                if (_parent != null)
                    _parent.Children.Add(this);
            }
            get => _parent;
        }

        public IEnumerable<DynamicNode> Siblings => _parent?.Children.Where(_ => _.id != id) ?? Enumerable.Empty<DynamicNode>();

        public int Depth => (Parent?.Depth ?? 0) + 1;

        public DynamicNodeParent[] Ancestors
        {
            get
            {
                var result = new List<DynamicNodeParent>();
                var p = this;
                while (p.Parent != null)
                {
                    result.Add(p.Parent);
                    p = p.Parent;
                }
                return result.ToArray();
            }
        }

        protected static void SetAmassedAdjustmentValueInSettings(string id, decimal value, ExerciseStats stats) =>
            stats.trainingPlanSettings.initialGroupWeights[id] = value;

        protected static decimal? GetAmassedAdjustmentValueInSettings(string id, ExerciseStats stats) =>
            stats.trainingPlanSettings.initialGroupWeights.TryGetValue(id, out var val) ? val : null;

        //export type CalcAmassedWeight = (gameId: string, stats: ExerciseStats) => number;
        public abstract decimal CalcAmassedWeight(Func<string, ExerciseStats, decimal> calcFunction, bool actualTrainedTimeOnly, ExerciseStats stats);
        //public abstract bool IsLeaf();
    }

    public class DynamicNodeLeaf : DynamicNode
    {
        public override decimal CalcAmassedWeight(Func<string, ExerciseStats, decimal> calcFunction, bool actualTrainedTimeOnly, ExerciseStats stats)
        {
            var actual = calcFunction(id, stats);
            return actual + (actualTrainedTimeOnly ? 0 : (GetAmassedAdjustmentValueInSettings(id, stats) ?? 0));
        }

        //public override bool IsLeaf() => true;

        public required GameDefinition Game { get; init; }
    }

    public class DynamicNodeParent : DynamicNode
    {
        public override decimal CalcAmassedWeight(Func<string, ExerciseStats, decimal> calcFunction, bool actualTrainedTimeOnly, ExerciseStats stats)
        {
            var amassed = Children.Select(_ => _.CalcAmassedWeight(calcFunction, actualTrainedTimeOnly, stats));
            return amassed.Sum() + (actualTrainedTimeOnly ? 0 : (GetAmassedAdjustmentValueInSettings(id, stats) ?? 0));
        }

        //public override bool IsLeaf() => false;

        public Func<string, ExerciseStats, decimal> amassedWeightFunc = //Default function: by training time
        (string gameId, ExerciseStats stats) =>
        {
            //Not by shared id, because we calc amassed for each id (both tangram#intro and tangram)
            //if by shared, same value would be counted multiple times
            return stats.GetGameStats(gameId).trainingTime; //getGameStatsSharedId(gameId)
        };

        private List<DynamicNode> _children = new();
        public List<DynamicNode> Children
        {
            get => _children;
            set
            {
                foreach (var item in value)
                    item.Parent = this; //.setParent(this);
                _children = value;
            }
        }

        public DynamicNodeLeaf? GetLeafById(string id) => GetDescendantById(id) as DynamicNodeLeaf;

        public DynamicNode? GetDescendantById(string id)
        {
            var found = Children.FirstOrDefault(o => o.id == id);
            if (found != null)
                return found;

            foreach (var item in Children.OfType<DynamicNodeParent>())
            {
                var found2 = item.GetDescendantById(id);
                if (found2 != null)
                    return found2;
            }
            return null;
        }

        public List<DynamicNode> GetAvailableChildren(ExerciseStats stats, string[]? exceptThese = null)
        {
            return Children.Where(_ => _.IsUnlockedNotLocked(stats) && exceptThese?.Contains(_.id) != true).ToList();
        }


        private void recGetLeaves(string[] exceptThese, List<DynamicNodeLeaf> result)
        {
            var children = this.Children.Where(_ => exceptThese.Contains(_.id) == false);
            result.AddRange(children.OfType<DynamicNodeLeaf>());
            foreach (var item in children.OfType<DynamicNodeParent>())
                item.recGetLeaves(exceptThese, result);
        }

        public List<DynamicNodeLeaf> GetLeaves(string[]? exceptThese = null)
        {
            var result = new List<DynamicNodeLeaf>();
            recGetLeaves(exceptThese ?? new string[0], result);
            return result;
        }

        private void recGetAvailableLeaves(ExerciseStats stats, string[] exceptThese, List<DynamicNodeLeaf> result)
        {
            var children = GetAvailableChildren(stats, exceptThese);
            result.AddRange(children.OfType<DynamicNodeLeaf>());
            //foreach (var child in children.Where(_ => _.isLeaf()))
            //    result.push(< DynamicNodeLeaf > _));
            foreach (var c in children.OfType<DynamicNodeParent>())
                c.recGetAvailableLeaves(stats, exceptThese, result);
            //children.Where(_ => !_.isLeaf).forEach(_ => (< DynamicNodeParent > _).recGetAvailableLeaves(stats, exceptThese, result));
        }

        public List<DynamicNodeLeaf> GetAvailableLeaves(ExerciseStats stats, string[]? exceptThese = null)
        {
            var result = new List<DynamicNodeLeaf>();
            recGetAvailableLeaves(stats, exceptThese ?? new string[0], result);
            return result;
        }


        private DynamicNode? GetNextChild(ExerciseStats stats, string[]? excludeThese = null)
        {
            var available = GetAvailableChildren(stats, excludeThese);
            return GetNextChildFromList(available, stats);
        }

        private DynamicNode? GetNextChildFromList(List<DynamicNode> available, ExerciseStats stats)
        {
            if (available.Any() == false || available.Where(_ => _.weight > 0).Any() == false)
            {
                return null;
            }
            //var fNormalize = (values: number[]) => {
            //    var sum = Math.max(1, values.sum());
            //    return values.map(_ => _ / sum);
            //};
            var amassed = available
                .Select(_ => _.CalcAmassedWeight(amassedWeightFunc, true, stats) + (GetAmassedAdjustmentValueInSettings(_.id, stats) ?? 0))
                .ToList();
            var relativeToTargets = amassed.Select((v, i) => available[i].weight == 0 ? decimal.MaxValue : v / available[i].weight).ToList();

            var minValue = relativeToTargets.Min(); // Math.min.apply(null, relativeToTargets);
            var chosen = available[relativeToTargets.IndexOf(minValue)];

            return chosen;
        }

        public void SetAdjustmentIfFirstTime(string id, ExerciseStats stats)
        {
            if (GetAmassedAdjustmentValueInSettings(id, stats) == null)
            {
                //No adjustment present in settings, so add it
                var node = GetDescendantById(id);
                if (node != null)
                    SetAdjustmentsInBranch(node, stats);
            }
        }

        private void SetAdjustmentsInBranch(DynamicNode node, ExerciseStats stats)
        {
            //TODO: should not take node as an argument - should operate on itself
            if (!node.IsUnlockedNotLocked(stats))
                return; //No point in adjusting this if it has been locked again

            var ancestors = node.Ancestors.ToList();
            var hierarchy = ancestors.Cast<DynamicNode>().Concat(new[] { node }).ToList();

            for (var i = 1; i < ancestors.Count; i++)
            {
                var a = ancestors[i];
                var availableChildren = a.GetAvailableChildren(stats);
                var childrenUsedBefore = availableChildren.Where(_ => GetAmassedAdjustmentValueInSettings(_.id, stats) != null);
                if (childrenUsedBefore.Any() == false)
                {
                    //this node hasn't been used before
                    //set this node's parent relative adjustment value
                    ancestors[i - 1].AdjustForChild(hierarchy[i].id, stats);
                }
            }
            var ii = ancestors.Count - 1;
            ancestors[ii].AdjustForChild(hierarchy[ii + 1].id, stats);
        }

        protected readonly record struct AmassedAndTarget(string GameId, decimal Target, decimal Amassed);

        protected List<AmassedAndTarget> GetAmassedAndTargetWeights(IEnumerable<DynamicNode> selectedChildren, ExerciseStats stats)
        {
            return selectedChildren.Select(c => new AmassedAndTarget(c.id, c.weight, c.CalcAmassedWeight(this.amassedWeightFunc, false, stats))).ToList();
        }

        private void AdjustForChild(string childId, ExerciseStats stats)
        {
            var available = GetAvailableChildren(stats);

            var tmp = GetAmassedAndTargetWeights(available, stats);

            var adjustment = 0M;
            var otherWeights = tmp.Where(_ => _.GameId != childId);
            var otherAmassedSum = otherWeights.Select(_ => _.Amassed).Sum();
            if (otherAmassedSum > 0)
            {
                var amassedPerTarget = otherAmassedSum / otherWeights.Sum(_ => _.Target);
                var childWeights = tmp.FirstOrDefault(_ => _.GameId == childId);
                if (childWeights.GameId.Any() == false) // == null)
                {
                    //This child is no longer available (locked)
                    return;
                }
                adjustment = childWeights.Target * amassedPerTarget - childWeights.Amassed;
                if (Math.Round(adjustment).ToString().Length > 3)
                { //remove decimals if 4 value digits (save space)
                    adjustment = Math.Round(adjustment);
                }
            }
            SetAmassedAdjustmentValueInSettings(childId, adjustment, stats);
        }

        public DynamicNodeLeaf? GetNextLeaf(ExerciseStats stats, string[]? excludeThese = null)
        {
            var available = GetAvailableChildren(stats, excludeThese);
            while (true)
            {
                if (!available.Any())
                    return null;

                var child = GetNextChildFromList(available, stats);
                if (child == null)
                    return null;

                var asLeaf = child as DynamicNodeLeaf;
                if (asLeaf != null)
                {
                    return asLeaf;
                }
                var subResult = ((DynamicNodeParent)child).GetNextLeaf(stats, excludeThese);
                if (subResult != null)
                {
                    return subResult;
                }
                available.Remove(child); //available.splice(available.indexOf(child), 1);
            }
        }

        public void ChangeWeights(ExerciseStats stats, Dictionary<string, decimal> newTargetWeights)
        {
            //if (!newTargetWeights)
            //    return;

            foreach (var kv in newTargetWeights)
            {
                var node = GetDescendantById(kv.Key);
                if (node == null)
                {
                    //Logger.warn("DynamicNode not found for weight change: " + k);
                    return;
                }
                var newTargetWeight = kv.Value;

                var oldAmassedAdjustment = GetAmassedAdjustmentValueInSettings(kv.Key, stats);
                if (oldAmassedAdjustment != null && node.Parent != null)
                {
                    var aAt = node.Parent.GetAmassedAndTargetWeights(node.Parent.Children, stats);

                    var exceptThis = aAt.Where(_ => _.GameId != kv.Key);
                    var amassedSumExceptThis = exceptThis.Select(_ => _.Amassed).Sum();
                    var targetSumExceptThis = exceptThis.Select(_ => _.Target).Sum();
                    var amassedPerTarget = amassedSumExceptThis / targetSumExceptThis;

                    var oldTotalAmassed = aAt.FirstOrDefault(_ => _.GameId == kv.Key).Amassed;
                    var newTotalAmassed = newTargetWeight * amassedPerTarget;
                    var change = newTotalAmassed - oldTotalAmassed;

                    SetAmassedAdjustmentValueInSettings(kv.Key, oldAmassedAdjustment.Value + change, stats);
                }
                node.weight = newTargetWeight;
            }
        }


        public object debugTargetWeights(dynamic? structure = null, decimal weightFactor = 1)
        {
            if (structure == null) structure = new ExpandoObject();

            var totalWeight = Children.Select(_ => _.weight).Sum();

            foreach (var child in Children)
            {
                var inGroupWeight = child.weight / totalWeight;
                dynamic subStructure = new { inGroupWeight = Math.Round(100M * inGroupWeight, 3), totalWeight = Math.Round(100M * inGroupWeight * weightFactor, 3) };
                structure[child.id] = subStructure;
                if (child is DynamicNodeParent aParent)
                {
                    subStructure.children = new ExpandoObject();
                    aParent.debugTargetWeights(subStructure["children"], inGroupWeight * weightFactor);
                }
            }
            return structure;
        }


        public Dictionary<string, List<decimal>> getDebugTrainedTimeInfo(ExerciseStats stats, int startDay, int endDay)
        {
            var perGame = stats.GetAllGameStats();
            var csvs = new Dictionary<string, List<decimal>>();

            for (int i = startDay; i <= endDay; i++)
                FRec(this, i, i);

            return csvs;

            long FRec(DynamicNodeParent parent, int startDay, int endDay)
            {
                var raws = parent.Children.Select(c => {
                    var raw = c is DynamicNodeLeaf leaf
                        ? perGame.GetValueOrDefault(c.id, new List<GameRunStats>())
                            .Where(_ => _.trainingDay >= startDay && _.trainingDay <= endDay)
                            .Select(_ => _.trainingTime).SumOrDefault()
                        : FRec((DynamicNodeParent)c, startDay, endDay);
                    return raw;
                }).ToList();

                var sum = raws.Sum();
                foreach (var item in parent.Children.Select((o, i) => new { Item = o, Index = i }))
                {
                    var key = string.Join(".", item.Item.Ancestors.Select(_ => _.id).Skip(1)) + "." + item.Item.id; //.slice(1).join("") + c.id;
                    if (!csvs.TryGetValue(key, out var value))
                    {
                        value = new();
                        csvs.Add(key, value);
                    }
                    value.Add(sum == 0 ? 0 : Math.Round(100M * raws[item.Index] / sum));
                }
                return sum;
            }
        }
    }

    /*
export class DynamicNodeParent extends DynamicNode {
    private __preTypify(definition: any) {
        if (definition.exercises) {
            definition.children = definition.exercises.map(_ => {
                _.__type = ObjectUtils.getClassNameFromConstructor(DynamicNodeLeaf);
                _.game = _.data;
                delete _.data;
                _.game.__type = ObjectUtils.getClassNameFromConstructor(GameDefinition);
                _.game.id = _.id;
                return _;
            });
            delete definition.exercises;
        } else if (definition.children) {
            definition.children = definition.children.map(_ => {
                if (!_.__type || _.__type === "DynamicGroup") {
                    //TODO: check if _ has any children, if not it's a leaf
                    _.__type = ObjectUtils.getClassNameFromConstructor(DynamicNodeParent);
                }
                return _;
            });
        }
        return definition;
    }
}
 */
}
