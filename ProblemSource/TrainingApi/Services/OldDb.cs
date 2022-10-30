using System.Data.Common;
using System.Data.SqlClient;
using System.Data;

namespace TrainingApi.Services
{
    public class OldDbRaw
    {
        private readonly string connectionString;

        public OldDbRaw(string connectionString)
        {
            this.connectionString = connectionString;
        }
        public async Task<List<T>> Read<T>(string query, Func<IDataReader, IReadOnlyCollection<DbColumn>, T> create)
        {
            var result = new List<T>();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            var columns = await reader.GetColumnSchemaAsync();
            while (await reader.ReadAsync())
                result.Add(create(reader, columns));
            return result;
        }
    }
}
