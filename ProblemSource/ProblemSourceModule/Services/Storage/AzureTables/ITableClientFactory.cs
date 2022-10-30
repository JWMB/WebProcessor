﻿using Azure.Data.Tables;

namespace ProblemSource.Services.Storage.AzureTables
{
    public interface ITableClientFactory
    {
        TableClient Phases { get; }
        TableClient TrainingDays { get; }
        TableClient PhaseStatistics { get; }
    }

    public class TableClientFactory : ITableClientFactory
    {
        private readonly string prefix = "vektor";
        private readonly string connectionString = "UseDevelopmentStorage=true";
        private readonly TableClientOptions tableClientOptions;
        public TableClientFactory()
        {
            tableClientOptions = new TableClientOptions();
            tableClientOptions.Retry.MaxRetries = 1;
        }

        public async Task Init()
        {
            await Phases.CreateIfNotExistsAsync();
            await TrainingDays.CreateIfNotExistsAsync();
            await PhaseStatistics.CreateIfNotExistsAsync();
        }

        public TableClient Phases => CreateClient(nameof(Phases));
        public TableClient TrainingDays => CreateClient(nameof(TrainingDays));
        public TableClient PhaseStatistics => CreateClient(nameof(PhaseStatistics));

        private TableClient CreateClient(string name) => new TableClient(connectionString, $"{prefix}{name}", tableClientOptions);

        public static async Task<TableClientFactory> Create()
        {
            var tableFactory = new TableClientFactory();
            try
            {
                await tableFactory.Init();
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("127.0.0.1:"))
                    throw new Exception("Could not connect to Storage Emulator - have you started it? See Azurite, https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio");
                throw;
            }
            return tableFactory;
        }
    }
}