using IntegrationReportSbAstBot.Interfaces;
using IntegrationReportSbAstBot.Services;
using Microsoft.Extensions.Logging;
using Quartz;
using Telegram.Bot;

namespace IntegrationReportSbAstBot.Class.Jobs
{
    /// <summary>
    /// Job для автоматического архивирования документов с ошибками валидации
    /// Выполняется каждые 30 минут
    /// </summary>
    [DisallowConcurrentExecution]
    public class ArchiveDocumentsJob(
        DocumentArchiveService archiveService,
        ITelegramBotClient botClient,
        ISubscriberService subscriberService,
        ILogger<ArchiveDocumentsJob> logger) : IJob
    {
        private readonly DocumentArchiveService _archiveService = archiveService;
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ISubscriberService _subscriberService = subscriberService;
        private readonly ILogger<ArchiveDocumentsJob> _logger = logger;

        /// <summary>
        /// Выполняет архивирование документов и отправляет отчет подписчикам
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Начало выполнения ArchiveDocumentsJob в {Time}", DateTime.Now);

            try
            {
                // Выполняем архивирование документов
                var archivedCount = await _archiveService.ArchiveDocumentsWithKindErrorsAsync();

                if (archivedCount > 0)
                {
                    // Формируем сообщение
                    var message = archivedCount > 0
                        ? $"✅ Автоматическое архивирование завершено\nАрхивировано документов: {archivedCount}"
                        : "ℹ️ Автоматическое архивирование завершено\nНет документов для архивирования";

                    // Отправляем сообщение подписчикам
                    await SendNotificationToSubscribersAsync(message);
                }

                _logger.LogInformation("ArchiveDocumentsJob успешно завершен. Архивировано: {Count} документов", archivedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении ArchiveDocumentsJob");

                // Уведомляем о ошибке (только админам)
                await SendErrorNotificationAsync(ex);
            }
        }

        /// <summary>
        /// Отправляет уведомление подписчикам
        /// </summary>
        private async Task SendNotificationToSubscribersAsync(string message)
        {
            try
            {
                var subscribers = await _subscriberService.GetSubscribersAsync();

                var tasks = subscribers.Select(chatId =>
                    _botClient.SendMessage(
                        chatId: chatId,
                        text: message));

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомлений подписчикам");
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
                // Пока просто логируем
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, "Ошибка при отправке уведомления об ошибке");
            }
        }
    }
}