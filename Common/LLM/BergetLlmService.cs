namespace Common.LLM
{
	public class BergetLlmService : ILlmService
	{
		private readonly Config config;
		private readonly SimpleApiClient client;
		public record Config(string ApiKey);

		public BergetLlmService(LlmModelSpecification modelSpecification, LlmServiceSpecification serviceSpecification, Func<HttpClient> clientFactory)
		{
			ModelSpecification = modelSpecification;

			client = new SimpleApiClient(serviceSpecification.ResourceUri, clientFactory);
			config = new Config(serviceSpecification.ApiKey);
		}

		public LlmModelSpecification ModelSpecification { get; private set; }

		public int CountTokens(string text) => new SimpleTokenCounter().CountTokens(text);

		public string Get(string prompt, int maxTokens = 1000, double temperature = 0, TimeSpan timeout = default, CancellationToken cancellation = default, Action<LlmContentReceivedData> contentReceived = null)
		{
			var content = new
			{
				model = ModelSpecification.DeploymentName, //"magistral",
				messages = new[]
				{
				  //new { role = "system", content = "You are a creative writing assistant." },
				  new { role = "user", content = prompt }
				},
				temperature = temperature,
				max_tokens = maxTokens
			};

			var reqConfig = new SimpleApiClient.PostConfig(content,
				req => req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey));
			var result = client.PostJson<Root, string>("", reqConfig).Result;
			return result.choices.First().message.content;
		}

		public async Task<LlmResult?> Invoke(string prompt, LlmCallOptions? options = null)
		{
			var content = new
			{
				model = ModelSpecification.DeploymentName, //"magistral",
				messages = new[]
				{
				  new { role = "user", content = prompt }
				},
				temperature = options?.Temperature ?? 0,
				max_tokens = options?.MaxTokens ?? 3000
			};
			var tmp = System.Text.Json.JsonSerializer.Serialize(content);

			var reqConfig = new SimpleApiClient.PostConfig(content,
				req => req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey));
			var result = await client.PostJson<Root, string>("", reqConfig);
			if (result?.choices == null)
				return null;
			return new LlmResult { Completion = result.choices.First().message.content, Tokens = (-1, -1), Model = content.model ?? "N/A" };
		}

		public class Choice
		{
			public Message message { get; set; } = new();
			public string finish_reason { get; set; } = "";
			public int index { get; set; }
		}

		public class Function
		{
			public string name { get; set; } = "";
			public string arguments { get; set; } = "";
		}

		public class FunctionCall
		{
			public string name { get; set; }
			public string arguments { get; set; }
		}

		public class Message
		{
			public string role { get; set; } = "";
			public string content { get; set; } = "";
			public FunctionCall? function_call { get; set; }
			public List<ToolCall>? tool_calls { get; set; }
		}

		public class Root
		{
			public string id { get; set; }
			public string @object { get; set; }
			public long created { get; set; }
			public string model { get; set; }
			public List<Choice> choices { get; set; }
			public Usage? usage { get; set; }
		}

		public class ToolCall
		{
			public string id { get; set; }
			public string type { get; set; }
			public Function function { get; set; }
		}

		public class Usage
		{
			public int prompt_tokens { get; set; }
			public int completion_tokens { get; set; }
			public int total_tokens { get; set; }
			public int co2_grams { get; set; }
		}
	}
}
