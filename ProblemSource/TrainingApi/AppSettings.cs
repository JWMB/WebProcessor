using Common;
using ProblemSource;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage.MongoDb;
using TrainingApi.Background;
using TrainingApi.RealTime;

namespace TrainingApi
{
    public class AppSettings
    {
        public AzureTableConfig AzureTable { get; set; } = new();
        public RealTimeConfig RealTime { get; set; } = new();
        public string? SyncUrls { get; set; } = "";

        public Services.MfaService.Config MfaConfig { get; set; } = new("Vektor");
		public StorageConfig Storage { get; set; } = new();
        public List<LlmServiceSpecification> LlmServices { get; set; } = new();
		public List<LlmModelSpecification> LlmModels { get; set; } = new();

        public ProcessorSettings Processors { get; set; } = new();
		//public InMemoryApiKeyRepository.Config ApiKeyConfig { get; set; } = new([]);
		//public IEnumerable<ApiKeyUser> ApiKeyUsers { get; set; } = new List<ApiKeyUser>();
	}

    public class ProcessorSettings
    {
		public ExportFullTrialsProcessor.Config FullTrialsExport { get; set; } = new ExportFullTrialsProcessor.Config { ExportFilePath = string.Empty };
		public CleanupDuplicates.Config CleanupDuplicates { get; set; } = new CleanupDuplicates.Config { };
	}
}
