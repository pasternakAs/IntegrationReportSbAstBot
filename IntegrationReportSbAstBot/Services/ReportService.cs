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
    /// <remarks>
    /// Инициализирует новый экземпляр класса ReportService
    /// </remarks>
    /// <param name="sqlConnectionFactory">Фабрика подключений к базе данных</param>
    /// <param name="logger">Логгер для записи информации и ошибок</param>
    public class ReportService(ISqlConnectionFactory sqlConnectionFactory, ILogger<ReportService> logger) : IReportService
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory = sqlConnectionFactory;
        private readonly ILogger<ReportService> _logger = logger;

        /// <summary>
        /// Генерирует данные отчета по важным пакетам документов за последние сутки
        /// Выполняет два запроса: подсчет общего количества и получение детальной информации
        /// </summary>
        /// <returns>Асинхронная задача, возвращающая данные отчета ReportDataClass</returns>
        /// <exception cref="Exception">Выбрасывается при ошибках выполнения запросов к базе данных</exception>
        public async Task<ReportDataClass> GenerateReportAsync()
        {
            _logger.LogInformation("Генерируем данные отчета по важным пакетам документов за последние сутки");

            try
            {
                using var connection = _sqlConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Параметризованный запрос для безопасности
                var sql = @"
                SELECT COUNT(*) as Amount,
                doctype as TypeDocument
                FROM dbo.docOOSdoc WITH (NOLOCK)
                WHERE CreateDate >= @DateFrom 
                AND (docType IN ('epProtocolEZK2020FinalPart', 'epProtocolEF2020FinalPart')
                   OR docType LIKE 'epNotificationE%')
                AND state IN (-1, -2)
                GROUP BY doctype";

                var dateFrom = DateTime.UtcNow.AddDays(-1);

                var summaryOfPackages = (await connection.QueryAsync<SummaryOfPackages>(sql, new { DateFrom = dateFrom })).ToList();

                // Получаем сами пакеты
                var packagesSql = @"
                SELECT docType as DocumentType
                   , violations
                   , inout = CASE 
		            WHEN InOut = 0 THEN 'AST --> EIS' 
		            WHEN InOut = 1 THEN 'AST <-- EIS' 
	                END
                   , ObjectId
                   , lastSendDate
                FROM dbo.docOOSdoc WITH (NOLOCK)
                WHERE (docType IN ('epProtocolEZK2020FinalPart', 'epProtocolEF2020FinalPart')
                   OR docType LIKE 'epNotificationE%')
                   AND CreateDate >= @DateFrom
                   AND state IN (-1, -2)";

                var packages = (await connection.QueryAsync<PackageInfo>(packagesSql, new { DateFrom = dateFrom })).ToList();

                return new ReportDataClass
                {
                    SummaryOfPackages = summaryOfPackages ?? [],
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