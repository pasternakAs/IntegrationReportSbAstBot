using Dapper;
using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис для генерации отчетов по пакетам документов
    /// Выполняет запросы к базе данных, собирает данные и формирует структуру отчета
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<ReportService> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр класса ReportService
        /// </summary>
        /// <param name="connectionFactory">Фабрика подключений к базе данных</param>
        /// <param name="logger">Логгер для записи информации и ошибок</param>
        public ReportService(IDbConnectionFactory connectionFactory, ILogger<ReportService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Генерирует данные отчета по важным пакетам документов за последние сутки
        /// Выполняет два запроса: подсчет общего количества и получение детальной информации
        /// </summary>
        /// <returns>Асинхронная задача, возвращающая данные отчета ReportDataClass</returns>
        /// <exception cref="Exception">Выбрасывается при ошибках выполнения запросов к базе данных</exception>
        public async Task<ReportDataClass> GenerateReportAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Параметризованный запрос для безопасности
                var sql = @"
                SELECT COUNT(*) as TotalCount
                FROM dbo.docOOSdoc WITH (NOLOCK)
                WHERE CreatedAt >= @DateFrom 
                AND (docType IN ('epProtocolEZK2020FinalPart', 'epProtocolEF2020FinalPart')
                   OR docType LIKE 'epNotificationE%')
                AND state IN (-1, -2)";

                var dateFrom = DateTime.UtcNow.AddDays(-1);

                var totalCount = await connection.QuerySingleAsync<int>(sql, new { DateFrom = dateFrom });

                // Получаем сами пакеты
                var packagesSql = @"
            SELECT docType
                   , violations
                   , inout
                   , ObjectId
                   , lastSendDate
            FROM dbo.docOOSdoc WITH (NOLOCK)
            WHERE (docType IN ('epProtocolEZK2020FinalPart', 'epProtocolEF2020FinalPart')
                   OR docType LIKE 'epNotificationE%')
              AND lastSendDate >= @DateFrom
              AND state IN (-1, -2)";

                var packages = (await connection.QueryAsync<PackageInfo>(packagesSql, new { DateFrom = dateFrom })).ToList();

                return new ReportDataClass
                {
                    TotalCount = totalCount,
                    Packages = packages,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации отчета");
                throw;
            }
        }
    }
}