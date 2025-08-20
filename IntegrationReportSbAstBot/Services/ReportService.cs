using Dapper;
using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationReportSbAstBot.Services
{
    public class ReportService : IReportService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<ReportService> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionFactory"></param>
        /// <param name="logger"></param>
        public ReportService(IDbConnectionFactory connectionFactory, ILogger<ReportService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<ReportDataClass> GenerateReportAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Параметризованный запрос для безопасности
                var sql = @"
                SELECT COUNT(*) as TotalCount
                FROM Packages 
                WHERE CreatedAt >= @DateFrom 
                AND IsImportant = 1";

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
              AND lastSendDate >= DATEADD(DAY, -1, GETDATE())
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