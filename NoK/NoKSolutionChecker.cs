using LLM;
using ProblemSourceModule.Services.ProblemGenerators;

namespace NoK
{
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

            var isCorrect  = task.CheckResponseIsCorrect(response.ResponseText);
            if (isCorrect == true)
            {
				return new SimpleSolutionAnalysis { IsCorrect = true };
			}

            if (completionService != null)
            {
                var prompt =
    $"""
Din uppgift är att försöka hjälpa användaren att lösa en matteuppgift.

Om användaren gett ett korrekt svar, ska ditt svar inledas med "Korrekt"
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
## Lösningar:
{string.Join("\n", task.Answer)}

## Lösningsförslag:
{string.Join("\n", task.Solution)}

## Ledtrådar:
{string.Join("\n", task.Hint)}
---

Användaren svarade med följande:
---
{response.ResponseText}
---

Återigen, lös inte problemet, utan ge användaren tips och ledtrådar om hur man kan tänka för att lösa det!
""";
                var completion = await completionService.GetChatCompletion(prompt);
                if (completion?.Trim().ToLower().StartsWith("korrekt") == true)
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
}
