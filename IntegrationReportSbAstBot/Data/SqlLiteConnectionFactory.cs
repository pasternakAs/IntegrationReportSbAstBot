using System.Data.Common;
using IntegrationReportSbAstBot.Class.Options;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Data.Sqlite;

namespace IntegrationReportSbAstBot.Data
{
    public class SqlLiteConnectionFactory : IDbConnectionFactory
    {
        private readonly DatabaseOptions _dbOptions;

        public SqlLiteConnectionFactory(Microsoft.Extensions.Options.IOptions<DatabaseOptions> options)
        {
            _dbOptions = options.Value;
        }

        public DbConnection CreateConnection()
        {
            var connection = new SqliteConnection(_dbOptions.ConnectionString);
            connection.Open();
            return connection;
        }
    }
}
