using System.Data.Common;
using IntegrationReportSbAstBot.Class.Options;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Data.SqlClient;

namespace IntegrationReportSbAstBot.Data
{
    /// <summary>
    /// Фабрика для создания подключений к базе данных SQL Server
    /// Реализует паттерн Factory для обеспечения централизованного создания подключений
    /// </summary>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly DatabaseOptions _dbOptions;

        /// <summary>
        /// Инициализирует новый экземпляр класса SqlConnectionFactory
        /// </summary>
        /// <param name="options">Настройки подключения к базе данных, полученные из конфигурации</param>
        /// <exception cref="ArgumentNullException">Выбрасывается, если options равен null</exception>
        public SqlConnectionFactory(Microsoft.Extensions.Options.IOptions<DatabaseOptions> options)
        {
            _dbOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Создает и открывает новое подключение к базе данных SQL Server
        /// </summary>
        /// <returns>Открытое подключение к базе данных типа SqlConnection</returns>
        /// <remarks>
        /// Подключение должно быть disposed после использования для освобождения ресурсов
        /// </remarks>
        public DbConnection CreateConnection()
        {
            return new SqlConnection(_dbOptions.ConnectionString);
        }
    }
}