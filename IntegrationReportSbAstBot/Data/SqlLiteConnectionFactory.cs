using System.Data.Common;
using IntegrationReportSbAstBot.Class.Options;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Data.Sqlite;

namespace IntegrationReportSbAstBot.Data
{
    /// <summary>
    /// Фабрика подключений к локальной базе данных SQLite
    /// Используется для хранения авторизационных данных
    /// </summary>
    public class SqlLiteConnectionFactory : ISqliteConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Инициализирует фабрику подключений SQLite
        /// </summary>
        /// <param name="options">Настройки базы данных SQLite</param>
        public SqlLiteConnectionFactory(Microsoft.Extensions.Options.IOptions<SqliteOptions> options)
        {
            _connectionString = options.Value.ConnectionString;
            InitializeDatabase();
        }

        /// <summary>
        /// Создает новое подключение к базе данных SQLite
        /// </summary>
        /// <returns>Подключение к базе данных SQLite</returns>
        public DbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// Инициализирует структуру базы данных при первом запуске
        /// </summary>
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Создаем таблицу запросов на авторизацию
            const string createRequestsTable = @"
                CREATE TABLE IF NOT EXISTS AuthorizationRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    UserName TEXT,
                    ChatId INTEGER NOT NULL,
                    RequestedAt TEXT NOT NULL,
                    RequestMessage TEXT,
                    IsApproved INTEGER DEFAULT 0,
                    IsProcessed INTEGER DEFAULT 0,
                    ProcessedBy INTEGER,
                    ProcessedAt TEXT
                )";

            // Создаем таблицу авторизованных пользователей
            const string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS AuthorizedUsers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER UNIQUE NOT NULL,
                    UserName TEXT,
                    ChatId INTEGER NOT NULL,
                    AuthorizedAt TEXT NOT NULL,
                    AuthorizedBy INTEGER NOT NULL,
                    IsActive INTEGER DEFAULT 1,
                    Notes TEXT,
                    IsSubscribe INTEGER DEFAULT 0
                )";

            const string createSubscribeTable = @"
                    CREATE TABLE IF NOT EXISTS Subscribers (
                        ChatId INTEGER PRIMARY KEY,
                        IsGroup INTEGER DEFAULT 0,
                        ChatName TEXT,
                        SubscribedAt TEXT NOT NULL,
                        IsActive INTEGER DEFAULT 1,
                        LastUpdated TEXT NOT NULL
                    )";

            using var commandCreateRequestsTable = new SqliteCommand(createRequestsTable, connection);
            commandCreateRequestsTable.ExecuteNonQuery();

            using var commandCreateUsersTable = new SqliteCommand(createUsersTable, connection);
            commandCreateUsersTable.ExecuteNonQuery();

            using var commandSubscribeTable = new SqliteCommand(createSubscribeTable, connection);
            commandSubscribeTable.ExecuteNonQuery();
        }
    }
}