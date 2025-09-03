using Dapper;
using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationReportSbAstBot.Services
{
    public class KtruMonitoringService(ISqlConnectionFactory sqlConnectionFactory, ILogger<KtruMonitoringService> logger)
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory = sqlConnectionFactory;
        private readonly ILogger<KtruMonitoringService> _logger = logger;

        /// <summary>
        /// Проверяет статус обработки пакетов КТРУ за последний месяц
        /// </summary>
        /// <returns>Данные мониторинга</returns>
        public async Task<List<KtruPackageInfo>> GetProblematicKtruPackagesAsync()
        {
            try
            {
                await using var connection = _sqlConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Один запрос для получения пакетов, висящих больше 1 дня
                const string sql = @"
                    SELECT 
                        Id as PackageId,
                        CreateDate,
                        DATEDIFF(day, CreateDate, GETDATE()) as DaysPending,
                        FileName as PackageSize -- или другое поле для размера, если нужно
                    FROM [CDB].[dbo].[UnIntFileLoad]
                    WHERE entitytype = 'nsiKTRUs' 
                    AND loadstatus = 1
                    AND createdate > DATEADD(MONTH, -1, GETDATE()) -- за последний месяц
                    AND DATEDIFF(day, CreateDate, GETDATE()) > 1 -- висят больше 1 дня
                    ORDER BY CreateDate ASC";

                var problemPackages = (await connection.QueryAsync<KtruPackageInfo>(sql)).ToList();

                _logger.LogInformation("Мониторинг КТРУ: найдено {Count} проблемных пакетов", problemPackages.Count);

                return problemPackages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при мониторинге пакетов КТРУ");
                throw;
            }
        }

        /// <summary>
        /// Проверяет, есть ли проблемные пакеты
        /// </summary>
        /// <returns>True если есть проблемные пакеты</returns>
        public async Task<bool> HasProblematicPackagesAsync()
        {
            var packages = await GetProblematicKtruPackagesAsync();
            return packages.Any();
        }

        /// <summary>
        /// Форматирует данные мониторинга в текстовое сообщение
        /// </summary>
        /// <param name="problemPackages">Список проблемных пакетов</param>
        /// <returns>Отформатированное сообщение</returns>
        public string FormatMonitoringMessage(List<KtruPackageInfo> problemPackages)
        {
            var message = new System.Text.StringBuilder();

            message.AppendLine("📊 <b>Мониторинг пакетов КТРУ</b>");
            message.AppendLine($"⏰ Дата проверки: {DateTime.Now:dd.MM.yyyy HH:mm}");
            message.AppendLine();

            if (problemPackages.Count == 0)
            {
                message.AppendLine("✅ <b>Все пакеты КТРУ обработаны успешно</b>");
                message.AppendLine("ℹ️ Нет пакетов, висящих более 1 дня");
                return message.ToString();
            }

            message.AppendLine($"🚨 <b>Обнаружено проблемных пакетов: {problemPackages.Count}</b>");
            message.AppendLine("⚠️ Эти пакеты висят в статусе обработки более 1 дня:");
            message.AppendLine();

            // Показываем первые 15 пакетов
            foreach (var package in problemPackages.Take(15))
            {
                message.AppendLine($"• ID: {package.PackageId}");
                message.AppendLine($"  Создан: {package.CreateDate:dd.MM.yyyy HH:mm}");
                message.AppendLine($"  В ожидании: {package.DaysPending} дней");
                message.AppendLine();
            }

            if (problemPackages.Count > 15)
            {
                message.AppendLine($"... и еще {problemPackages.Count - 15} пакетов");
            }

            return message.ToString();
        }
    }
}