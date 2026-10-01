using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // /[action]
	public class TelemetryController : ControllerBase
    {
		private readonly IMongoDatabase db;
		private readonly ILogger<AggregatesController> _logger;

        public TelemetryController(IMongoDatabase db, ILogger<AggregatesController> logger)
        {
			this.db = db;
			_logger = logger;
        }

		[Authorize]
		[HttpGet]
		public async Task<List<TelemetryItem>> Get(string? id)
		{
			var collection = db.GetCollection<TelemetryItem>();
			return await (await collection.FindAsync(o => true)).ToListAsync();
		}

		[HttpPost]
        public async Task Post(ExtendedError error)
        {
            var collection = db.GetCollection<TelemetryItem>();
			var doc = new TelemetryItem { Username = error.Username, Error = error };
			try
			{
				await collection.InsertOneAsync(doc);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex.Message);
			}
		}
    }

	public static class IMongoDatabaseExtensions
	{
		public static IMongoCollection<T> GetCollection<T>(this IMongoDatabase db) => db.GetCollection<T>(typeof(T).Name);
	}

    public class TelemetryItem : Document
    {
		public string? Username { get; set; }
		public ExtendedError? Error { get; set; }
    }


	public class ClientInfo
	{
		public string? UserAgent { get; set; }
		public string? Platform { get; set; }
		public string? Language { get; set; }
		public string? ScreenResolution { get; set; }
		public int? ColorDepth { get; set; }
		//timezone: Intl.DateTimeFormat().resolvedOptions().timeZone
	}

	public class ExtendedError
	{
		public string? Username { get; set; }
		public ClientInfo? ClientInfo { get; set; }
		public string? LocalTime { get; set; }
		public string? Message { get; set; }
		public string? Name { get; set; }
		public int? ColumnNumber { get; set; }
		public int? LineNumber { get; set; }
		public string? FileName { get; set; }

		public string? Stack { get; set; }
		public object? Cause { get; set; }
		public int? HttpStatus { get; set; }

		public override string ToString() => $"{Name} '{Message}' {Stack}";
	}

	public class Document
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public string Name { get; set; } = string.Empty;

		public DateTime Created { get; set; } = DateTime.UtcNow;
		public DateTime Modified { get; set; } = DateTime.UtcNow;

		bool isDirty = false;
		//[JsonIgnore]
		//[Newtonsoft.Json.JsonIgnore]
		[BsonIgnore]
		//[SuppressMessage("Style", "S3237")]
		public bool IsDirty => isDirty;
		public void SetDirty(bool? dirtyFlag = null) => isDirty = dirtyFlag ?? true;

		bool isNew = false;
		[BsonIgnore]
		public bool IsNew
		{
			get => isNew; set => isNew = false; // TODO: setter makes little sense - maybe something related to serialization? Can we change this?
		}
		public void SetNew(bool nextNew = true) => isDirty = isNew = nextNew;

		public virtual void Sanitize(Func<string, string> sanitize) { }

		public string Cls { get => GetType().Name; }
		public override bool Equals(object? obj) => obj is Document d ? d.Id == Id : false;
		public override int GetHashCode() => Id.GetHashCode();

		public override string ToString() => $"{GetType().Name}: {Name} #{Id}";
	}
}