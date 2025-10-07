using System.Text;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class GetErrorIntegrationsCommandHandler(IReportService reportService, IReportHtmlService reportHtmlService, ISubscriberService subscriberService, ILogger<GetErrorIntegrationsCommandHandler> logger, ITelegramBotClient bot) : IAuthorizedCommandHandler
    {
        private readonly IReportService _reportService = reportService;
        private readonly IReportHtmlService _reportHtmlService = reportHtmlService;
        private readonly ISubscriberService _subscriberService = subscriberService;
        private readonly ILogger<GetErrorIntegrationsCommandHandler> _logger = logger;
        private readonly ITelegramBotClient _bot = bot;

        public string Command => "/geterrorintegration";

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var subscribers = await _subscriberService.GetSubscribersAsync();

                if (subscribers.Count == 0)
                {
                    _logger.LogInformation("Нет подписчиков для отправки сообщения");
                    return;
                }

                var generateReportData = await _reportService.GenerateReportAsync(null, cancellationToken);
                // Формируем HTML отчет
                var htmlReport = _reportHtmlService.GenerateHtmlReport(generateReportData);
                var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);

                // Сохраняем HTML в временный файл
                await File.WriteAllTextAsync(filePath, htmlReport, Encoding.UTF8, cancellationToken);

                // Формируем сообщение
                var messageText = $"по важным пакетам в количестве ({generateReportData.SummaryOfPackages.Sum(x => x.Amount)} шт.)";

                // Отправляем отчеты всем подписчикам
                var tasks = subscribers.Select(chatId => SendDocumentAsync(chatId, filePath, messageText));
                await Task.WhenAll(tasks);

                // Удаляем временный файл
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении ReportJob");
            }
            finally
            {
                _logger.LogInformation("Завершение ReportJob в {Time}", DateTime.Now);
            }
        }

        /// <summary>
        /// Отправляет HTML документ отчета пользователю Telegram
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя</param>
        /// <param name="pathFile">Путь к HTML файлу или содержимое файла</param>
        /// <returns>Асинхронная задача</returns>
        private async Task SendDocumentAsync(long chatId, string pathFile, string textMessage)
        {
            try
            {
                string fileName = $"report_{DateTime.Now:yyyyMMdd}.html";

                // Если bodyHtml это путь к файлу
                if (File.Exists(pathFile))
                {
                    using var fileStream = new FileStream(pathFile, FileMode.Open, FileAccess.Read);
                    await _bot.SendDocument(
                        chatId: chatId,
                        document: new InputFileStream(fileStream, fileName),
                        caption: "📈 Отчет в формате HTML " + textMessage);
                }
                else
                {
                    // Если bodyHtml это содержимое файла
                    var fileBytes = Encoding.UTF8.GetBytes(pathFile);
                    await using var stream = new MemoryStream(fileBytes);
                    await _bot.SendDocument(
                        chatId: chatId,
                        document: new InputFileStream(stream, fileName),
                        caption: "📈 Отчет в формате HTML " + textMessage);
                }
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 403)
            {
                // Пользователь заблокировал бота
                await _subscriberService.UnsubscribeUserAsync(chatId);
                _logger.LogInformation($"Пользователь {chatId} заблокировал бота и был удален из списка");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки документа {chatId}");
            }
        }
    }
}