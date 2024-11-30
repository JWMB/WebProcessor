using Common;
using ProblemSource.Models;
using ProblemSourceModule.Models;
using static ProblemSource.Models.DynamicTrainingPlan;
using System.Numerics;
using static ProblemSource.Models.ExerciseStats;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;
using System;
using ProblemSource.Models.LogItems;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ProblemSourceModule.Models
{
    public interface IPlanetInfo
    {
        long decided_at { get; set; }
        long? unlockedTimestamp { get; set; }
        int? numMedals { get; set; }
        //isCompleted?: boolean;
        int[] gameRunsRefs { get; set; } //{ gameId: string, started: number }[];
        string gameId { get; set; }
    }

    public class PlanetInfo : IPlanetInfo
    {
        public long decided_at { get; set; } = 0;
        public long? unlockedTimestamp { get; set; } = 0;
        public int? numMedals { get; set; } = 0;
        public int[] gameRunsRefs { get; set; } = new int[0];
        public string gameId { get; set; } = "";

        public GameDefinition? nextGame;
        public List<GameRunStats> gameRuns = new();
        public bool wasJustUnlocked = false;
            //public indexInPlan: number = -1; //TODO: not really necessary

        public PlanetInfo(JObject? init = null, IEnumerable<GameDefinition>? gameDefs = null, List<GameRunStats>? allGameRuns = null)
        {
            if (init != null)
            {
                var thisProps = GetType().GetProperties();
                foreach (var prop in init.Properties()) //init.GetType().GetProperties())
                {
                    var thisProp = thisProps.FirstOrDefault(o => o.Name == prop.Name); // && o.PropertyType == prop.PropertyType);
                    if (thisProp != null)
                    {
                        var srcVal = prop.Children().First();
                        try
                        {
                            var converted = srcVal.ToObject(thisProp.PropertyType);
                            //var converted = Convert.ChangeType(srcVal, thisProp.PropertyType);
                            thisProp.SetValue(this, converted);
                        }
                        catch (Exception ex)
                        {
                            // TODO: log
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                //Object.keys(init).forEach(k => this[k] = init[k]);
            }
            if (gameDefs?.Any() == true && allGameRuns?.Any() == true)
            {
                Init(gameDefs, allGameRuns);
            }
        }

        public void Init(IEnumerable<GameDefinition> gameDefs, List<GameRunStats> allGameRuns)
        {
            nextGame = gameDefs.FirstOrDefault(_ => _.id == gameId);
            gameRuns = gameRunsRefs.Select(_ => allGameRuns[_]).ToList();
            //this.gameRuns = this.gameRunsRefs.map(_ => { return allGameRuns.findFirst(gr => gr.gameId === _.gameId && gr.started_at === _.started); });
        }

        public IPlanetInfo serialize(ExerciseStats stats)
        {
            return new PlanetInfo {
                gameId = gameId,
                decided_at = decided_at,
                gameRunsRefs = gameRuns.Select(_ => stats.gameRuns.IndexOf(_)).ToArray(), //{ return { gameId: _.gameId, started: _.started_at } }),
                numMedals = numMedals,
                unlockedTimestamp = unlockedTimestamp
            };
        }

        public bool visibleOnMenu => nextGame?.invisible == false;
        public bool isUnlocked
        {
            get => unlockedTimestamp > 0;
            set
            {
                if (decided_at == 0)
                    decided_at = DateTime.Now.ToUnixTimestamp();
                unlockedTimestamp = value ? DateTime.Now.ToUnixTimestamp() : 0;
            }
        }

        public bool isCompleted => _isCompletedOverride != null ? _isCompletedOverride.Value : numMedals >= 3;

        public long lastUsed => gameRuns.Any() ? gameRuns.Max(_ => _.started_at) : 0;

        public decimal highestLevel => gameRuns.Any() ? gameRuns.Max(_ => _.highestLevel) : 0;

        public int numRunsWon => gameRuns.Any() ? gameRuns.Count(_ => _.won) : 0;

        public int numRuns => gameRuns.Count();

        private bool? _isCompletedOverride = null;
        public void setOverrideIsCompleted(bool? value)
        {
            _isCompletedOverride = value;
        }

        public override string ToString() => $"{gameId} {numRunsWon}/{numRuns} compl:{isCompleted} maxLvl:{highestLevel} visible:{visibleOnMenu}";
    }

    public class PlanetBundler
    {
        private static List<PlanetInfo> _planetInfos = new(); // was null
        public static void init() => _planetInfos = new();

        private static PlanetInfo? _currentPlanet;
        public static PlanetInfo currentPlanet => _currentPlanet == null ? throw new NullReferenceException() : _currentPlanet;


        public static List<PlanetInfo> getPlanetsOnlyUsedGameId(string gameId, bool useSharedId = true)
        {
            if (useSharedId)
                gameId = ExerciseStats.getSharedId(gameId);

            //Get only those planets where all gameRuns' are of gameId
            return _planetInfos.Where(planet => 
                planet.gameRuns.FirstOrDefault(gr => (useSharedId ? ExerciseStats.getSharedId(gr.gameId) : gr.gameId) != gameId)
                == null).ToList();
        }
        public static List<PlanetInfo> getPreviouslyCalcedPlanets(TrainingPlan? tp = null, ExerciseStats? exerciseStats = null)
        {
            if (_planetInfos?.Any() != true && tp != null && exerciseStats != null)
                _planetInfos = PlanetBundler.deserializePlanets(tp, exerciseStats);
            return _planetInfos ?? [];
        }

        public static void decidedFuturePlanet(PlanetInfo planet)
        {
            _planetInfos.Add(planet);
        }

        //This one is odd; can't the concerned party stash the former value somewhere instead..?
        private static int previousMedals = 0;

        public static int getPreviousMedalCount() => // This can only be used directly after new medal count is set (used on win screen)
            PlanetBundler.previousMedals;

        public static bool getIsPlanetComplete(string planetId)
        {
            var p = _planetInfos.LastOrDefault(_ => _.gameId == planetId);
            return p?.isCompleted == true;
        }

        public static int getTotalPlanetsCompleted() => _planetInfos.Count(_ => _.isCompleted);

        public static void startGame(string id) => setCurrentPlanet(id);

        public static void setCurrentPlanet(string value)
        {
            var available = _planetInfos.Where(_ => _.isUnlocked && !_.isCompleted);
            var found = available.LastOrDefault(_ => _.gameId == value);
            if (found == null)
            {
                found = _planetInfos.LastOrDefault(_ => _.gameId == value);
                if (found != null)
                {
                    //Logger.warn("Planet completed / not unlocked: " + value);
                }
                else
                {
                    //not available as a planet, probably a "hidden" game (test)
                    //TODO: check if this is the case
                    //throw Error("Planet not found: " + value);
                    
                    //Logger.warn("planet not found: " + value);
                }
            }
            _currentPlanet = found;
        }
        public static void setCurrentPlanet(int value)
        {
            if (value < 0 || value >= _planetInfos.Count)
                throw new Exception("Planet index not found: " + value);
            _currentPlanet = _planetInfos[value];

        }
        public static void setCurrentPlanet(PlanetInfo value) => _currentPlanet = value;

        private static List<PlanetInfo> deserializePlanets(TrainingPlan tp, ExerciseStats stats)
        {
            //TODO: better transfer mechanism between _planetInfos and GameState.exerciseStats
            stats.planetInfos ??= new List<object>();
            var definedGames = tp.getDefinedGames();
            return stats.planetInfos.OfType<JObject>().Select(_ => new PlanetInfo(_, definedGames, stats.gameRuns)).ToList();
        }

        private static bool _registeredSessionReset = false;
        public static List<PlanetInfo> getPlanets(ExerciseStats exerciseStats, TrainingSettings trainingSettings, TrainingPlan tp, bool forceRecalc = false)
        {
            if (!_registeredSessionReset)
            {
                // TODO (high):
                //GameState.sessionResetted.add(() => {
                //    init();
                //});
                _registeredSessionReset = true;
                init();
            }

            if (_planetInfos.Any())
            {
                if (forceRecalc)
                    _planetInfos = new();
                else
                    return _planetInfos;
            }

            //if (tp == null)
            //    tp = TrainingPlan.create(null);
    
            _planetInfos = PlanetBundler.deserializePlanets(tp, exerciseStats);

            // console.log("planetInfos", PlanetBundler._planetInfos);
            // console.log("exerciseStats", exerciseStats);

            //var isDevCheatMode = (<CognitionMattersApp>App.instance).isDev;
            //if (isDevCheatMode) {
            //    if (!PlanetBundler._warnedOfDevCheatMode) {
            //        Logger.warn("Dev cheat mode, target criteria lowered");
            //        PlanetBundler._warnedOfDevCheatMode = true;
            //    }
            //    tp.changeTargetEndCriteriaForTesting();

            //    //for quick testing, set any started planet to complete
            //    PlanetBundler._planetInfos.filter(_ => !_.isCompleted && _.lastUsed > 0)
            //        .forEach(p => {
            //            var gr1 = p.gameRuns[0];
            //            for (var i = p.numMedals; i < 3; i++) {
            //                var gr = new GameRunStats({ gameId: gr1.gameId, won: true, trainingTime: gr1.trainingTime, started_at: gr1.started_at });
            //                GameState.exerciseStats.gameRuns.push(gr);
            //                p.gameRuns.push(gr); //p.gameRunsRefs.push({ gameId: gr.gameId, started: gr.started_at });
            //            }
            //            p.numMedals = 3;
            //        });
            //    PlanetBundler.serializePlanets(GameState.exerciseStats);
            //    PlanetBundler._planetInfos = PlanetBundler.deserializePlanets(tp, GameState.exerciseStats);
            //}

            var bundler = new PlanetBundler();
            var planets = bundler.recalcPlanets(tp, exerciseStats, trainingSettings);
            return planets;
        }

        public static string getGameIdFromPlanetId(string id)
        {
            var split = id.Split("#");
            if (split.Length > 1)
            {
                if (int.TryParse(split[1], out var cnt) && cnt.ToString() == split[1])
                    return split[0];
            }
            return id;
        }

        public List<PlanetInfo> recalcPlanets(TrainingPlan tp, ExerciseStats stats, TrainingSettings trainingSettings) //, object? selectionParameters = null)
        {
            var gameStats = stats.GetAllGameStats();

            var existingPlanets = new List<PlanetInfo>(_planetInfos); //PlanetBundler.convertGameStatsToPlanets(gameStats);

            var tmpNotCompletedPlanets = existingPlanets.Where(_ => !_.isCompleted);
            //In free mode, planet might not have received a nextGame, use gameRuns to set:
            foreach (var item in tmpNotCompletedPlanets.Where(_ => _.nextGame == null))
            {
                item.nextGame = new GameDefinition();
                if (item.gameRuns.Any())
                    item.nextGame.id = item.gameRuns[0].gameId;
            }

            var notCompletedPlanetGameIds = tmpNotCompletedPlanets
                .Select(_ => _.nextGame!.id)
                .Select(_ => PlanetBundler.getGameIdFromPlanetId(_))
                .ToList();
            var available = tp.getProposedGames(notCompletedPlanetGameIds, stats); //, selectionParameters);

            //Linear: available will contain all games (also completed ones)
            //Dynamic: only non-completed.
            //TODO: how to keep order? In linear, we can complete 3rd exercise,  but it should still be in 3rd place
            //In Dynamic, we want to keep order of completed/already unlocked planets, and only use available to add planets

            //Linear plan may have the #<number> suffix already from plan (tenpals#1 etc) 
            var availablePureGameIds = available.Select(_ => PlanetBundler.getGameIdFromPlanetId(_.id));
            var removed = notCompletedPlanetGameIds.Where(id => availablePureGameIds.FirstOrDefault(_ => _ == id) == null).ToList();

            //planets that shouldn't be available - set them to completed if they have been used, remove them if not:
            var unavailable = existingPlanets.Where(_ => !_.isCompleted).Where(_ => _.nextGame != null && removed.IndexOf(_.nextGame.id) >= 0);
            foreach (var _ in unavailable.Where(_ => _.isUnlocked == true))
                _.setOverrideIsCompleted(true); // _.numMedals = 3);
            foreach (var _ in unavailable.Where(_ => _.isUnlocked == false))
                existingPlanets.Remove(_);

            //flag newly unlocked planets:
            foreach (var _ in existingPlanets.Where(_ => !_.isCompleted && !_.isUnlocked))
            {
                if (available.Exists(a => a.id == _.gameId && a.unlocked))
                {
                    _.isUnlocked = true;
                    _.wasJustUnlocked = true;
                }
            }

            PlanetInfo fAddPlanet(ProposedGameInfo pgi)
            {
                var planet = new PlanetInfo();
                planet.isUnlocked = pgi.unlocked;
                planet.wasJustUnlocked = planet.isUnlocked;
                //PlanetBundler.getOrCreatePlanet(_.id).unlocked_at = Date.now();
                planet.numMedals = 0;
                planet.nextGame = new GameDefinition{ id = pgi.id };

                if (pgi.id.IndexOf("#") < 0) {
                    var numPreviousPlanets = (int)Math.Floor(gameStats.GetValueOrDefault(pgi.id, new()).Where(p => p.won).Count() * 1M / 3);
                    planet.nextGame.id = pgi.id + "#" + (numPreviousPlanets + 1);
                }
                //In old system, we didn't write data until actually entering game
                //TODO: writing data!!
                //TODO: switch to linear test stats / phase structure, add some part that indicates which phases are with which planet
                    decidedFuturePlanet(planet);
                    return planet;
                };

            //available that are not in existing:
            var notInExisting = available.Where(_ =>
                existingPlanets.Where(p => !p.isCompleted)
                    .FirstOrDefault(p => p.nextGame != null && (p.nextGame.id == _.id || PlanetBundler.getGameIdFromPlanetId(p.nextGame.id) == _.id)) == null);
            var planets = existingPlanets.Concat(notInExisting.Select(_ => fAddPlanet(_))).ToList();

            var definedGames = tp.getDefinedGames();

            foreach (var planet in planets)
            {
                var nextGameId = (planet.nextGame?.id != null) ? planet.nextGame.id : planet.gameId; //.gameRuns[0].gameId;
                                                                                                               //In allowFreeChoice, a planet might not have nextGame defined, if so use last gameRun for id.
                var findId = nextGameId; //planet.nextGame.id;
                var gameDef = definedGames.FirstOrDefault(_ => _.id == findId);
                if (gameDef == null)
                {
                    findId = PlanetBundler.getGameIdFromPlanetId(nextGameId);
                    gameDef = definedGames.FirstOrDefault(_ => _.id == findId);
                    if (gameDef == null)
                    {
                        if (planet.gameRuns.Any())
                        {
                            findId = planet.gameRuns[0].gameId;
                            gameDef = definedGames.FirstOrDefault(_ => _.id == findId);
                        }
                        if (gameDef == null)
                        {
                            gameDef = definedGames.FirstOrDefault(_ => PlanetBundler.getGameIdFromPlanetId(_.id) == findId);
                            if (gameDef == null)
                            {
                                gameDef = definedGames.FirstOrDefault(_ => PlanetBundler.getGameIdFromPlanetId(_.id).Split('#')[0] == findId);
                                if (gameDef == null)
                                {
                                    //console.error("Game def prob", findId, definedGames, definedGames.map(o => PlanetBundler.getGameIdFromPlanetId(o.id)));
                                    throw new Exception("No game definition found for " + nextGameId);
                                }
                            }
                        }
                    }
                }
                planet.nextGame = gameDef; //fakeData;
            }

            foreach (var _ in planets.Where(_ => _.nextGame != null))
                _.gameId = _.nextGame!.id;

            planets = planets.Where(_ => _.visibleOnMenu).ToList();

            if (trainingSettings.customData?.unlockAllPlanets == true)
            {
                foreach (var o in planets)
                {
                    o.setOverrideIsCompleted(false);
                    o.unlockedTimestamp = 1;
                    o.wasJustUnlocked = false;
                }
            }

            PlanetBundler.serializePlanets(stats);

            return planets;
        }

        private static void serializePlanets(ExerciseStats stats)
        {
            stats.planetInfos = _planetInfos.Select(_ => _.serialize(stats)).Select(o => (object)o).ToList();

            //if (stats.planetInfos.Count() > 10)
            //    console.log("KSKS");
            //console.log("PlanetInfos compression");
            //JsonCompression.testCompression((<any>stats).planetInfos, {
            //    "gameId": {
            //        "compress": { "type": "dictionary", "name": "game" }
            //    },
            //    //"gameRunsRefs": {
            //    //    "compress": { "type": "subList" },
            //    //    "properties": { "gameId": { "compress": { "type": "dictionary", "name": "game" } } }
            //    //}
            //});
            ////console.log("GameRuns compression");
            ////JsonCompression.testCompression(stats.gameRuns);
        }

        public static void logTestPhase(ProblemSource.Models.Aggregates.Phase phase, //{ endCriteriaManager: EndCriteriaManager, getPlayerScore: () => number } //PhaseBase //medalMode: MedalModes, 
            ExerciseStats stats, PhaseEndLogItem peItem, TrainingSettings trainingSettings)
        {
            // TODO (high): called via GameState.testPhaseLoggers.push(PlanetBundler.logTestPhase);

            //var id = TestStatistics.instance.currentGameId; //phase.test.id;
            //var endCriteriaManager = phase.endCriteriaManager;
            ////TestStatistics.instance.currentPhase.
            //var medalMode = phase.medalMode;
            //var score = phase.getPlayerScore();

            //var planet = PlanetBundler.currentPlanet;
            //if (planet == null) //e.g. hidden tests are not associated with planets
            //    return;

            //var wonRace = endCriteriaManager.endType == EndType.TARGET;

            //var runStats = TestStatistics.instance.runStats;
            //planet.gameRuns.Add(runStats);
            ////planet.gameRunsRefs.push({ gameId: id, started: runStats.started_at });

            //if (trainingSettings.customData?.medalMode != null)
            //    medalMode = trainingSettings.customData.medalMode;

            //var numMedals = 0;
            //PlanetBundler.previousMedals = planet.numMedals ?? 0;
            //if (medalMode == "ALWAYS")
            //{
            //    numMedals = Math.Min(3, PlanetBundler.previousMedals + 1);
            //}
            //else
            //{
            //    if (medalMode == "THREE_WINS")
            //    {
            //        var numWon = planet.gameRuns.Count(_ => _.won);
            //        numMedals = Math.Min(3, numWon);
            //    }
            //    else if (medalMode == "ONE_WIN")
            //    {
            //        var numWon = planet.gameRuns.Count(_ => _.won);
            //        numMedals = numWon > 0 ? 3 : 0;
            //    }
            //    else if (medalMode == "TARGET_SCORE")
            //    {
            //        var targetScore = endCriteriaManager.getTargetScore();  //TODO: get max value if different availabale target scores
            //        var scoreFract = score / targetScore;
            //        numMedals = scoreFract >= 1 ? 3
            //            : (scoreFract >= 0.66 ? 2
            //                : (scoreFract > 0.33 ? 1 : 0));
            //    }
            //}
            //planet.numMedals = numMedals; //test.medalCount 

            ////// save time stamp (used to decide if guide phases should be skipped)
            ////test.lastTimeStamp = Date.now(); //TODO: set both in GameState::saveState() and GameState::logTestPhase(), remove one of them

            //peItem.planetTargetScore = endCriteriaManager.getMaxTargetScore();
            //peItem.completedPlanet = planet.isCompleted;

            //PlanetBundler.serializePlanets(stats);
        }
    }

    /*
        export class PlanetBundler {

            private static _warnedOfDevCheatMode = false;


            public static convertGameStatsToPlanets(gameStats: KeyValMap<string, GameRunStats[]>): PlanetInfo[] {
                //TODO: this shouldn't be used anymore!
                var planets = <PlanetInfo[]>[];
                //Since getAllGameStats currently uses old game stats data format to emulate "new" format, 
                //we don't have anything that indicates how phases should be grouped into planets
                var groupedByCreatedTime = new KeyValMap<number, GameRunStats[]>();
                gameStats.keys.forEach((k, i) => {
                    if (gameStats.values[i].length == 0) {
                        return;
                    }

                    var list = gameStats.values[i].sort((a, b) => a.started_at - b.started_at);
                    var timeForReset = list[0].started_at;
                    for (var i = 1; i < list.length; i++) {
                        var diff = Math.abs(list[i].started_at - list[i - 1].started_at);
                        if (diff < 10) {
                            list[i].started_at = timeForReset;
                        }
                    }
                    list.forEach(gr => {
                        var group = groupedByCreatedTime.getOrAddByKey(gr.started_at, []);
                        group.push(gr);
                    });
                });

                groupedByCreatedTime.keys.forEach((k, i) => {
                    var grList = groupedByCreatedTime.values[i];
                    //Make sure they're separated by game (unlockedTimeStamp is actually "createdAtTimeStamp" and several can be created simultaneously "ahead of time"):
                    var groupedByGameId = new KeyValMap<string, GameRunStats[]>();
                    grList.forEach(gr => {
                        var list = groupedByGameId.getOrAddByKey(gr.gameId, []);
                        list.push(gr);
                    });
                    groupedByGameId.keys.forEach((gameId, i2) => {
                        var grs = groupedByGameId.values[i2]; //.filter(_ => _.trainingTime >= 0)
                        var planet = new PlanetInfo();
                        //TODO: currently there's no flag for this, could use "fake" negative training time as flag
                        planet.unlockedTimestamp = grs.filter(_ => _.trainingTime > 0).length > 0 ? Date.now() : 0;

                        planet.numMedals = grs.filter(_ => _.won).length;
                        //planet.isCompleted = planet.numMedals >= 3;

                        //planet.visibleOnMenu - is set later (depends on training plan)
                        planet.wasJustUnlocked = false;
                        planet.nextGame = <GameDefinition>{ id: grs[0].gameId }; //gameId
                        planets.push(planet);
                    });
                });
                return planets;
            }
        }*/
}
