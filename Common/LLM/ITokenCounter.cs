namespace Common.LLM
{
	public interface ITokenCounter
	{
		public int CountTokens(string text, string? modelOrTokenizerName = null);
	}

	public class SimpleTokenCounter : ITokenCounter
	{
		public int CountTokens(string text, string? modelOrTokenizerName = null)
		{
			return text.Length / 4;
		}
	}

	//public class TiktokenCounter : ITokenCounter
	//{
	//	private static Dictionary<string, Microsoft.ML.Tokenizers.TiktokenTokenizer?> tokenizers = new();
	//	private readonly string modelOrEncoding;

	//	public TiktokenCounter(string modelOrEncoding)
	//	{
	//		this.modelOrEncoding = modelOrEncoding;
	//	}

	//	private static List<(string ModelStartsWith, string Encoding)> modelNameStartToEncoding = new List<(string, string)>{
	//		("gpt-5", "o200k_base"),
	//		("gpt-4o", "o200k_base"),
	//		("gpt-", "cl100k_base"),
	//		("text-davinci", "p50k_base"),
	//	};
	//	private string GetEncodingFromModelOrEncoding(string input)
	//	{
	//		input = input.ToLower().Trim();
	//		var found = modelNameStartToEncoding.FirstOrDefault(o => input.StartsWith(o.ModelStartsWith));
	//		if (found.Encoding != default)
	//			return found.Encoding;
	//		found = modelNameStartToEncoding.FirstOrDefault(o => input == o.Encoding);
	//		if (found.Encoding != default)
	//			return input;
	//		throw new NotImplementedException(input);
	//	}

	//	private Microsoft.ML.Tokenizers.TiktokenTokenizer? GetOrCreate(string modelOrEncoding)
	//	{
	//		var encoding = GetEncodingFromModelOrEncoding(modelOrEncoding);
	//		if (!tokenizers.TryGetValue(encoding, out var tokenizer))
	//		{
	//			try
	//			{
	//				tokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForEncoding(encoding);
	//				tokenizers.Add(encoding, tokenizer);
	//			}
	//			catch (Exception ex)
	//			{
	//				Log.Warning($"No tokenizer found for encoding '{encoding}' (modelOrEncoding={modelOrEncoding})");
	//				tokenizers.Add(encoding, null);
	//			}
	//		}
	//		return tokenizer;
	//	}

	//	public int CountTokens(string text, string? modelOrEncoding = null) =>
	//		GetOrCreate(modelOrEncoding ?? this.modelOrEncoding)?.CountTokens(text) ?? 0;
	//}
}
