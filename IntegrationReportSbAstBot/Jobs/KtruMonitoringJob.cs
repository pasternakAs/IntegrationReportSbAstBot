using IntegrationReportSbAstBot.Interfaces;
using IntegrationReportSbAstBot.Services;
using Microsoft.Extensions.Logging;
using Quartz;
using Telegram.Bot;

namespace IntegrationReportSbAstBot.Class.Jobs
{
    /// <summary>
    /// Job для мониторинга статусов обработки пакетов КТРУ
    /// Проверяет пакеты, висящие в статусе 1 более 1 дня
    /// </summary>
    [DisallowConcurrentExecution]
    public class KtruMonitoringJob(
        KtruMonitoringService monitoringService,
        ITelegramBotClient botClient,
        ISubscriberService subscriberService,
        ILogger<KtruMonitoringJob> logger) : IJob
    {
        private readonly KtruMonitoringService _monitoringService = monitoringService;
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ISubscriberService _subscriberService = subscriberService;
        private readonly ILogger<KtruMonitoringJob> _logger = logger;

        /// <summary>
        /// Выполняет мониторинг пакетов КТРУ и отправляет алерты при необходимости
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Начало выполнения KtruMonitoringJob в {Time}", DateTime.Now);

            try
            {
                // Получаем только проблемные пакеты (висящие больше 1 дня)
                var problemPackages = await _monitoringService.GetProblematicKtruPackagesAsync();

                // Отправляем алерт только если есть проблемные пакеты
                if (problemPackages.Count != 0)
                {
                    var message = KtruMonitoringService.FormatMonitoringMessage(problemPackages);
                    await SendAlertToSubscribersAsync(message);
                    _logger.LogWarning("Обнаружено {Count} проблемных пакетов КТРУ", problemPackages.Count);
                }
                else
                {
                    _logger.LogInformation("KtruMonitoringJob: все пакеты обработаны успешно");
                }

                _logger.LogInformation("KtruMonitoringJob успешно завершен. Проблемных пакетов: {Count}",
                    problemPackages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении KtruMonitoringJob");
                await SendErrorNotificationAsync(ex);
            }
        }

        /// <summary>
        /// Отправляет алерт подписчикам
        /// <paramref name="message"/><paramref name="message"/>
        /// </summary>
        private async Task SendAlertToSubscribersAsync(string message)
        {
            try
            {
                var subscribers = await _subscriberService.GetSubscribersAsync();

                var tasks = subscribers.Select(chatId =>
                    _botClient.SendMessage(
                        chatId: chatId,
                        text: message,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html));

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке алертов подписчикам");
            }
        }

        /// <summary>
        /// Отправляет уведомление об ошибке администраторам
        /// </summary>
        private async Task SendErrorNotificationAsync(Exception ex)
        {
            try
            {
                // Здесь можно отправить уведомление администраторам
                _logger.LogError("Ошибка в KtruMonitoringJob: {Message}", ex.Message);
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, "Ошибка при отправке уведомления об ошибке");
            }
        }
    }
}