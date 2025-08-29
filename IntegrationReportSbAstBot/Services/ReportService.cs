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

        public async Task<ReportDataClass> GenerateReportAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Генерация отчета начата {Time}", DateTime.Now);

            try
            {
                using var connection = _sqlConnectionFactory.CreateConnection();

                var dateFrom = DateTime.Now.AddDays(-1);

                // Сводка
                const string summarySql = @"
                SELECT COUNT(*) as Amount,
                       docType as TypeDocument
                FROM dbo.docOOSdoc WITH (NOLOCK)
                WHERE CreateDate >= @DateFrom 
                  AND (docType LIKE 'epNotificationE%' 
                       OR docType LIKE 'cpContract%' 
                       OR docType LIKE '%FinalPart%')
                  AND state IN (-1, -2)
                GROUP BY docType";

                var summary = (await connection.QueryAsync<SummaryOfPackages>(
                    summarySql, new { DateFrom = dateFrom })).ToList();

                // Детализация
                const string detailsSql = @"
                SELECT docType as DocumentType,
                       violations,
                       inout = CASE 
                                    WHEN InOut = 0 THEN 'AST --> EIS' 
                                    WHEN InOut = 1 THEN 'AST <-- EIS' 
                               END,
                       ObjectId,
                       lastSendDate
                FROM dbo.docOOSdoc WITH (NOLOCK)
                WHERE CreateDate >= @DateFrom
                  AND (docType LIKE 'epNotificationE%' 
                       OR docType LIKE 'cpContract%' 
                       OR docType LIKE '%FinalPart%')
                  AND state IN (-1, -2)";

                var packages = (await connection.QueryAsync<PackageInfo>(
                    detailsSql, new { DateFrom = dateFrom })).ToList();

                return new ReportDataClass
                {
                    SummaryOfPackages = summary ?? [],
                    Packages = packages,
                    GeneratedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации отчета");
                throw;
            }
        }
    }
}