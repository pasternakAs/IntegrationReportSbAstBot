using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Data.Sqlite;

namespace IntegrationReportSbAstBot.Services
{
    public class BotStateService : IBotStateService
    {
        private readonly ISqliteConnectionFactory _sqliteConnectionFactory;

        public BotStateService(ISqliteConnectionFactory sqliteConnectionFactory)
        {
            _sqliteConnectionFactory = sqliteConnectionFactory;
            Initialize().Wait();
        }

        private async Task Initialize()
        {
            await using var connection = _sqliteConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS BotState (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                IsEnabled INTEGER NOT NULL
            );
            INSERT OR IGNORE INTO BotState (Id, IsEnabled) VALUES (1, 1);";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> IsEnabledAsync()
        {
            using var connection = _sqliteConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT IsEnabled FROM BotState WHERE Id = 1";

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }

        public async Task SetEnabledAsync(bool enabled)
        {
            using var connection = _sqliteConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE BotState SET IsEnabled = $enabled WHERE Id = 1";
            cmd.Parameters.Add(new SqliteParameter("$enabled", enabled ? 1 : 0));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}