﻿using LLM;
using NoK.Models;
using NoK.Models.Raw;
using ProblemSourceModule.Services.ProblemGenerators;

namespace NoK
{
    public class NoKDomain : IProblemDomain
    {
        public NoKDomain(NoKStimuliRepository.Config config, ISimpleCompletionService? completionService = null)
        {
            var repo = new NoKStimuliRepository(config);
            StimuliRepository = repo;
            SolutionChecker = new NoKSolutionChecker(repo, completionService);
        }
        public ISolutionChecker SolutionChecker { get; init; }
        public IStimuliRepository StimuliRepository { get; init; }
    }

    public class NoKSolutionChecker : ISolutionChecker
    {
        private readonly NoKStimuliRepository repo;
        private readonly ISimpleCompletionService? completionService;

        public NoKSolutionChecker(NoKStimuliRepository repo, ISimpleCompletionService? completionService = null)
        {
            this.repo = repo;
            this.completionService = completionService;
        }

        public async Task<ISolutionAnalysis> Check(IStimulus stimulus, IUserResponse response)
        {
            var task = await repo.GetSubtask(stimulus.Id);
            if (task == null)
                throw new Exception($"Task not found: {stimulus.Id}");

            if (task.Answer.Contains(response.ResponseText))
            {
                return new SimpleSolutionAnalysis { IsCorrect = true };
            }

            if (completionService != null)
            {
                var prompt =
    $"""
Din uppgift är att försöka hjälpa användaren att lösa en matteuppgift.

Om användaren gett ett korrekt svar, ska du svara "OK"
Om användaren gett ett felaktigt svar, ska du ge en ledtråd för hur problemet kan lösas, och försöka beskriva vilka misstag användaren verkar ha gjort.
Ge inte det korrekta svaret, ge istället ledtrådar som kan hjälpa användaren att själv lösa problemet.
T.ex., om problemet var "3+5*8" och användaren svarar "64" så kan du svara "Glöm inte att multiplikation går före addition"

Här är problemet som användaren fått:
---
{task.Parent?.Body}
{task.Question}
---

Här är lite material relaterat till problemet:
---
{string.Join("\n", task.Solution)}
{string.Join("\n", task.Hint)}
{string.Join("\n", task.Answer)}
---

Användaren svarade med följande:
---
{response.ResponseText}
---

Återigen, lös inte problemet, utan ge användaren tips och ledtrådar om hur man kan tänka för att lösa det!
""";
                var completion = await completionService.GetChatCompletion(prompt);
                if (completion?.Trim().ToUpper() == "OK")
                    return new SimpleSolutionAnalysis { IsCorrect = true };
                return new SimpleSolutionAnalysis { IsCorrect = false, Feedback = completion ?? "" };
            }

            return new SimpleSolutionAnalysis { IsCorrect = false };
            //if (task.Parent is AssignmentMultiChoice mc)
            //{ }
            //else if (task.Parent is Assignment rg)
            //{ }
            //else
            //    throw new NotImplementedException();
        }

        public IUserResponse Deserialize(object obj) => IProblemDomain.DeserializeWithId<SimpleUserResponse>(obj);
    }

    public class NoKStimuliRepository : IStimuliRepository
    {
        public record Config(string AssignmentResource);

        private List<IAssignment> assignments;
        private readonly Config config;

        public NoKStimuliRepository(Config config)
        {
            var subparts = RawConverter.ReadRaw(File.ReadAllText(config.AssignmentResource));
            assignments = subparts.SelectMany(o => o.Assignments).ToList();
            this.config = config;
        }

        public async Task<Subtask?> GetSubtask(string id)
        {
            var src = await GetSource(id); //assignments.SingleOrDefault(o => o.Id == int.Parse(split[0]));
            if (src == null)
                return null;

            var split = id.Split('/');

            Subtask? task = null;
            if (split.Length > 1)
            {
                var index = int.Parse(split[1]);
                task = src.Tasks.SingleOrDefault(o => o.Index == index);
                //if (index < src.Tasks.Count)
                //{
                //    task = src.Tasks[index];
                //}
                //else
                //{ }
            }
            else if (src.Tasks.Any())
            {
                task = src.Tasks.First();
            }
            return task;
        }

        public Task<IAssignment?> GetSource(string id)
        {
            var split = id.Split('/');
            return Task.FromResult(assignments.SingleOrDefault(o => o.Id == int.Parse(split[0])));
        }

        public async Task<IStimulus?> GetById(string id)
        {
            var src = await GetSubtask(id);
            return src == null ? null : SubtaskToStimulus(src);
        }

        private IStimulus SubtaskToStimulus(Subtask src)
        {
            return new NoKStimulus
            {
                Presentation = src.Parent?.Body ?? "",
                Question = src.Question ?? "",
                Id = src.Id, // $"{src.Parent.Id}/{src.Index}",
                SourceId = $"{nameof(NoKStimuliRepository)};{config.AssignmentResource}",
            };
        }

        public Task<List<IStimulus>> GetAll()
        {
            var regular = assignments.OfType<Assignment>().ToList();
            var withAnswers = regular.Where(o => o.Tasks.Any(p => p.Answer.Count == 1)).ToList();
            var withNonNumericAnswers = withAnswers.SelectMany(o => o.Tasks).Where(o => decimal.TryParse(o.Answer.Single(), out var _) == false).ToList();
            //var withNumericAnswers = withAnswers.Except(withNonNumericAnswers.Select(o => o.Parent)).ToList();
            //var toExpose = withNumericAnswers;

            return Task.FromResult(withNonNumericAnswers.Select(SubtaskToStimulus).ToList()); // toExpose.SelectMany(o => o.Tasks).Select(SubtaskToStimulus).ToList());
        }

        public async Task<List<string>> GetAllIds()
        {
            return (await GetAll()).Select(o => o.Id).ToList(); //withNumericAnswers.SelectMany(o => o.Tasks.Select((_, i) => $"{o.Id}/{i}")).ToList());
        }

        public IStimulus Deserialize(object obj) => IProblemDomain.DeserializeWithId<NoKStimulus>(obj);
    }

    public class NoKStimulus : IStimulus
    {
        public string Id { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;

        public string Presentation { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }
}
