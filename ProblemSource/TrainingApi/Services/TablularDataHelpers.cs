using CsvHelper.Configuration;
using System.Collections;

namespace TrainingApi.Services
{
	public static class TablularDataHelpers
	{
		public static void WriteFile<T>(IEnumerable<T> items, string file)
		{
			using var stream = File.OpenWrite(file);
			Write(items, stream);
		}

		public static string WriteToString<T>(IEnumerable<T> items)
		{
			using var stream = new MemoryStream();
			using var writer = new StreamWriter(stream);
			var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { NewLine = "\n", Delimiter = "\t" };
			using var csvWriter = new CsvHelper.CsvWriter(writer, config);
			Write(items, csvWriter);

			stream.Position = 0;
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		public static void Write<T>(IEnumerable<T> rows, Stream stream)
		{
			using var writer = new StreamWriter(stream);
			Write(rows, writer);
		}

		public static void Write<T>(IEnumerable<T> rows, StreamWriter writer)
		{
			var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { NewLine = "\n", Delimiter = "\t" };
			using var csvWriter = new CsvHelper.CsvWriter(writer, config);
		}

		public static void Write<T>(IEnumerable<T> rows, CsvHelper.CsvWriter writer)
		{
			//configureWriter?.Invoke(csvWriter);

			if (typeof(T).IsAssignableTo(typeof(IEnumerable)))
			{
				var kvs = rows.Select(r => r as IEnumerable)
					.Where(o => o != null)
					.Select(o => {
						var e = o.GetEnumerator();
						var tmp = new List<KeyValuePair<string, object?>>();
						while (e.MoveNext())
							if (e.Current is KeyValuePair<string, object?> kv)
								tmp.Add(kv);
							else if (e.Current is KeyValuePair<string, string?> kv2)
								tmp.Add(KeyValuePair.Create(kv2.Key, (object?)kv2.Value));

						return tmp;
					})
					.Where(o => o != null)
					.ToList();

				var headings = kvs.SelectMany(o => o.Select(p => p.Key)).Distinct().ToList();
				foreach (var heading in headings)
					writer.WriteField(heading); // changeHeader?.Invoke(heading) ?? 
				writer.NextRecord();
				foreach (var item in kvs)
				{
					foreach (var heading in headings)
						writer.WriteField(item.FirstOrDefault(o => o.Key == heading).Value);
					writer.NextRecord();
				}
			}
			else
			{
				writer.WriteRecords(rows);
			}
			writer.Flush();
		}
	}
}
