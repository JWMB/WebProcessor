using System.Net;
using System.Net.Http.Json;

namespace Common.LLM
{
	public class SimpleApiClient
	{
		private readonly string baseUri;
		private readonly Func<HttpClient> clientFactory;

		public SimpleApiClient(string baseUri, Func<HttpClient> clientFactory)
		{
			this.baseUri = baseUri;
			this.clientFactory = clientFactory;
		}

		public record PostConfig(object Payload, Action<HttpRequestMessage>? Modify = null)
		{
			public PostConfig(object Payload, IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers)
				: this(Payload, msg => SetHeaders(msg, Headers))
			{ }

			public PostConfig(object Payload, IEnumerable<(string, string)> Headers)
				: this(Payload, msg => SetHeaders(msg, Headers.ToDictionary(o => o.Item1, o => (IEnumerable<string>)new[] { o.Item2 })))
			{ }

			static void SetHeaders(HttpRequestMessage msg, IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers)
			{
				foreach (var kv in Headers)
					msg.Headers.Add(kv.Key, kv.Value);
			}
		}

		public async Task<TResult> PostJson<TResult, TError>(string path, PostConfig config, CancellationToken cancellation = default)
		{
			var client = clientFactory();
			using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUri}{path}");
			req.Content = JsonContent.Create(config.Payload);
			config.Modify?.Invoke(req);

			HttpResponseMessage response;
			try
			{
				response = await client.SendAsync(req, cancellation);
			}
			catch (Exception ex)
			{
				throw;
			}
			
			if (!response.IsSuccessStatusCode)
			{
				var result = response.Content == null ? "N/A" : await response.Content.ReadAsStringAsync();
				Exception ex;
				if (typeof(TError) == typeof(string))
					ex = new TypedException<string>(response.StatusCode) { Value = result };
				else if (typeof(TError) == typeof(object))
					ex = new TypedException<object>(response.StatusCode) { Value = result };
				else
					ex = new TypedException<TError>(response.StatusCode, result) { Value = System.Text.Json.JsonSerializer.Deserialize<TError>(result) };
				throw ex;
			}

			var content = await response.Content.ReadFromJsonAsync<TResult>(cancellation);
			if (content == null)
				throw new Exception("Invalid response");
			return content;
		}

		public class TypedException<T> : Exception
		{
			public TypedException(HttpStatusCode code, string? message = null) : base(message)
			{
				Code = code;
			}
			public HttpStatusCode Code { get; set; }
			public T? Value { get; set; }
		}
	}

}
