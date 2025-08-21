using System.Text;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Задача Quartz для периодической генерации и отправки отчетов подписчикам через Telegram бота
    /// Выполняет сбор данных, формирование HTML отчета и рассылку всем активным подписчикам
    /// </summary>
    public class ReportJob : IJob
    {
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<ReportJob> _logger;
        private readonly ISubscriberService _subscriberService;
        private readonly IReportService _reportService;
        private readonly IReportHtmlService _reportHtmlService;

        /// <summary>
        /// Инициализирует новый экземпляр класса ReportJob
        /// </summary>
        /// <param name="bot">Клиент Telegram бота для отправки сообщений</param>
        /// <param name="logger">Логгер для записи информации о выполнении задачи</param>
        /// <param name="subscriberService">Сервис управления подписчиками</param>
        /// <param name="reportService">Сервис генерации данных отчета</param>
        /// <param name="reportHtmlService">Сервис формирования HTML отчета</param>
        public ReportJob(ITelegramBotClient bot, ILogger<ReportJob> logger, ISubscriberService subscriberService, IReportService reportService, IReportHtmlService reportHtmlService)
        {
            _bot = bot;
            _logger = logger;
            _subscriberService = subscriberService;
            _reportService = reportService;
            _reportHtmlService = reportHtmlService;
        }

        /// <summary>
        /// Выполняет основную логику задачи: генерирует отчет и отправляет его всем подписчикам
        /// </summary>
        /// <param name="context">Контекст выполнения задачи Quartz</param>
        /// <returns>Асинхронная задача</returns>
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Начало выполнения ReportJob в {Time}", DateTime.Now);

            try
            {
                var subscribers = await _subscriberService.GetSubscribersAsync();

                if (subscribers.Count == 0)
                {
                    _logger.LogInformation("Нет подписчиков для отправки сообщения");
                    return;
                }


                // Генерируем данные отчета
                var generateReportData = await _reportService.GenerateReportAsync();
                // Формируем сообщение
                var messageText = $"📈 Отчёт по важным пакетам ({generateReportData.TotalCount} шт.) за последние сутки";

                // Формируем HTML отчет
                var htmlReport = _reportHtmlService.GenerateHtmlReport(generateReportData);
                var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);

                // Сохраняем HTML в временный файл
                await File.WriteAllTextAsync(filePath, htmlReport, Encoding.UTF8);

                // Отправляем отчеты всем подписчикам
                var tasks = subscribers.Select(chatId => SendReportToUserAsync(chatId, messageText, filePath));
                await Task.WhenAll(tasks);

                // Удаляем временный файл
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                _logger.LogInformation($"Отчет отправлен {subscribers.Count} подписчикам");
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
        /// Отправляет текстовое сообщение пользователю Telegram
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя</param>
        /// <param name="messageText">Текст сообщения для отправки</param>
        /// <returns>Асинхронная задача</returns>
        private async Task SendMessageAsync(long chatId, string messageText)
        {
            try
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: messageText,
                    parseMode: ParseMode.Html);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 403)
            {
                // Пользователь заблокировал бота
                await _subscriberService.UnsubscribeUserAsync(chatId);
                _logger.LogInformation($"Пользователь {chatId} заблокировал бота и был удален из списка");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки сообщения {chatId}");
            }
        }

        /// <summary>
        /// Отправляет полный отчет пользователю: сначала текстовое сообщение, затем HTML документ
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя</param>
        /// <param name="messageText">Текстовое сообщение с краткой информацией</param>
        /// <param name="bodyHtml">Путь к HTML файлу отчета</param>
        /// <returns>Асинхронная задача</returns>
        private async Task SendReportToUserAsync(long chatId, string messageText, string bodyHtml)
        {
            try
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: messageText,
                    parseMode: ParseMode.Html);

                // Затем отправляем документ
                await SendDocumentAsync(chatId, bodyHtml);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 403)
            {
                // Пользователь заблокировал бота
                await _subscriberService.UnsubscribeUserAsync(chatId);
                _logger.LogInformation($"Пользователь {chatId} заблокировал бота и был удален из списка");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки отчета пользователю {chatId}");
            }
        }

        /// <summary>
        /// Отправляет HTML документ отчета пользователю Telegram
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя</param>
        /// <param name="bodyHtml">Путь к HTML файлу или содержимое файла</param>
        /// <returns>Асинхронная задача</returns>
        private async Task SendDocumentAsync(long chatId, string bodyHtml)
        {
            try
            {
                string fileName = $"report_{DateTime.Now:yyyyMMdd}.html";

                // Если bodyHtml это путь к файлу
                if (File.Exists(bodyHtml))
                {
                    using var fileStream = new FileStream(bodyHtml, FileMode.Open, FileAccess.Read);
                    await _bot.SendDocument(
                        chatId: chatId,
                        document: new InputFileStream(fileStream, fileName),
                        caption: "Отчет в формате HTML");
                }
                else
                {
                    // Если bodyHtml это содержимое файла
                    var fileBytes = System.Text.Encoding.UTF8.GetBytes(bodyHtml);
                    using var stream = new MemoryStream(fileBytes);
                    await _bot.SendDocument(
                        chatId: chatId,
                        document: new InputFileStream(stream, fileName),
                        caption: "Отчет в формате HTML");
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