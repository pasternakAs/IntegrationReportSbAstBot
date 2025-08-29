using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Data.Sqlite;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис управления состоянием бота
    /// Обеспечивает хранение и управление глобальным состоянием включения/выключения бота
    /// Использует локальную базу данных SQLite для персистентного хранения состояния
    /// </summary>
    public class BotStateService : IBotStateService
    {
        private readonly ISqliteConnectionFactory _sqliteConnectionFactory;

        /// <summary>
        /// Инициализирует новый экземпляр сервиса управления состоянием бота
        /// </summary>
        /// <param name="sqliteConnectionFactory">Фабрика подключений к локальной базе данных SQLite</param>
        public BotStateService(ISqliteConnectionFactory sqliteConnectionFactory)
        {
            _sqliteConnectionFactory = sqliteConnectionFactory;
        }

        /// <summary>
        /// Инициализирует структуру таблицы состояния бота в базе данных
        /// Создает таблицу BotState при первом запуске и добавляет запись по умолчанию
        /// </summary>
        /// <returns>Асинхронная задача завершения инициализации</returns>
        /// <remarks>
        /// Таблица BotState содержит единственную запись с Id=1 для хранения глобального состояния
        /// По умолчанию бот включен (IsEnabled = 1)
        /// Используется ограничение CHECK (Id = 1) для гарантии единственной записи
        /// </remarks>
        public async Task InitializeAsync()
        {
            await using var connection = _sqliteConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS BotState (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                IsEnabled INTEGER NOT NULL
            );
            INSERT OR IGNORE INTO BotState (Id, IsEnabled) VALUES (1, 1);";

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Проверяет текущее состояние включения бота
        /// </summary>
        /// <returns>Асинхронная задача, возвращающая true если бот включен, иначе false</returns>
        /// <remarks>
        /// Читает состояние из таблицы BotState где Id = 1
        /// Возвращает true если значение IsEnabled равно 1, иначе false
        /// При отсутствии записи возвращает false
        /// </remarks>
        public async Task<bool> IsEnabledAsync()
        {
            await using var connection = _sqliteConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT IsEnabled FROM BotState WHERE Id = 1";

            var result = await cmd.ExecuteScalarAsync();
            return result is long value && value == 1;
        }

        /// <summary>
        /// Устанавливает состояние включения/выключения бота
        /// </summary>
        /// <param name="enabled">True для включения бота, false для выключения</param>
        /// <returns>Асинхронная задача завершения установки состояния</returns>
        /// <remarks>
        /// Обновляет значение IsEnabled в таблице BotState для записи с Id = 1
        /// Использует параметризованный запрос для предотвращения SQL-инъекций
        /// True преобразуется в 1, false в 0 для хранения в INTEGER поле
        /// </remarks>
        public async Task SetEnabledAsync(bool enabled)
        {
            await using var connection = _sqliteConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE BotState SET IsEnabled = $enabled WHERE Id = 1";
            cmd.Parameters.Add(new SqliteParameter("$enabled", enabled ? 1 : 0));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}