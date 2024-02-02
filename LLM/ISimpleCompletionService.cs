namespace LLM
{
    public interface ISimpleCompletionService
    {
        Task<string> GetChatCompletion(string prompt);
    }

    public class AzureOpenAICompletionService : ISimpleCompletionService
    {
        private readonly Config config;

        public record Config(Uri Endpoint, string Key, string Deployment);
        public AzureOpenAICompletionService(Config config)
        {
            this.config = config;
        }

        public async Task<string> GetChatCompletion(string prompt)
        {
            var api = new OpenAI_API.OpenAIAPI(new OpenAI_API.APIAuthentication(config.Key));
            api.ApiVersion = "2023-12-01-preview";
            api.ApiUrlFormat = $"{config.Endpoint}openai/deployments/{config.Deployment}/{{1}}?api-version={{0}}";
            var result = await api.Chat.CreateChatCompletionAsync(new OpenAI_API.Chat.ChatRequest
            {
                Temperature = 0,
                MaxTokens = 250,
                Messages = new[] { new OpenAI_API.Chat.ChatMessage(OpenAI_API.Chat.ChatMessageRole.System, prompt) }
            });
            return result.Choices.FirstOrDefault()?.Message.TextContent ?? "";
            //var service = new OpenAiCompletionService(new OpenAiCompletionService.EndpointConfig { Endpoint = config.Endpoint, Deployment = config.Deployment, Key = config.Key });
            //var response = await service.GetChatCompletions(
            //    new[] { new ChatMessage(prompt, ChatRole.System) },
            //    new CompletionSettings { Temperature = 0 });
            //return response.GetResponseText() ?? "";
        }
    }
}
