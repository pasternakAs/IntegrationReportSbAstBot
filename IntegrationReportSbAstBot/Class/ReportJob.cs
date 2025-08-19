using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Quartz;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.Class
{
    public class ReportJob : IJob
    {
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<ReportJob> _logger;
        private readonly ISubscriberService _subscriberService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        public ReportJob(ITelegramBotClient bot, ILogger<ReportJob> logger, ISubscriberService subscriberService)
        {
            _bot = bot;
            _logger = logger;
            _subscriberService = subscriberService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Начало выполнения ReportJob в {Time}", DateTime.Now);

            try
            {
                var subscribers = await _subscriberService.GetSubscribersAsync();

                if (subscribers.Count == 0)
                {
                    Console.WriteLine("Нет подписчиков для отправки сообщения");
                    return;
                }

                await _bot.SendMessage(
                  chatId: 6426583094, // @xamfess
                  text: $"TestMessage",
                  parseMode: ParseMode.Html);

                const string connStr = "Server=172.30.201.12;Database=CDB;Trusted_Connection=true;TrustServerCertificate=true;";
                var rows = new List<DocRow>();

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(@"
            SELECT docType, violations, inout, ObjectId, lastSendDate
            FROM dbo.docOOSdoc WITH (NOLOCK)
            WHERE (docType IN ('epProtocolEZK2020FinalPart', 'epProtocolEF2020FinalPart')
                   OR docType LIKE 'epNotificationE%')
              AND lastSendDate >= DATEADD(DAY, -1, GETDATE())
              AND state IN (-1, -2)
        ", conn))
                {
                    await conn.OpenAsync();
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        rows.Add(new DocRow
                        {
                            DocType = r.GetString(0),
                            Violations = r.IsDBNull(1) ? "" : r.GetString(1),
                            Direction = r.GetBoolean(2) ? "EIS --> ETP" : "ETP --> EIS",
                            ObjectId = r.GetValue(3).ToString() ?? "",
                            LastSendDate = r.GetDateTime(4)
                        });
                    }
                }

                if (rows.Count == 0) return;

                int total = rows.Count;

                var summaryRows = rows
                    .GroupBy(x => x.DocType)
                    .Select(g => $"<tr><td>{g.Key}</td><td>{g.Count()}</td></tr>");
                string summaryHtml = $@"
            <h3>Сводка по важным пакетам за последние сутки</h3>
            <table border='1' cellpadding='5' cellspacing='0'>
                <tr><th>Тип пакета</th><th>Количество</th></tr>
                {string.Join("", summaryRows)}
            </table><br/>";

                var detailRows = rows
                    .OrderByDescending(x => x.LastSendDate)
                    .Select(x =>
                        $"<tr><td>{x.DocType}</td><td>{x.Violations}</td><td>{x.Direction}</td><td>{x.ObjectId}</td><td>{x.LastSendDate:yyyy-MM-dd HH:mm:ss}</td></tr>");
                string detailsHtml = $@"
            <h3>Детализация по важным пакетам</h3>
            <table border='1' cellpadding='5' cellspacing='0'>
                <tr><th>Тип пакета</th><th>Ошибка</th><th>Направление</th><th>Процедура</th><th>Последняя дата отправки</th></tr>
                {string.Join("", detailRows)}
            </table>";

                string bodyHtml = summaryHtml + detailsHtml;

                // Отправляем сообщения всем подписчикам
                var messageText = $"Отчёт по важным пакетам ({total} шт.) за последние сутки";
                var tasks = subscribers.Select(chatId => SendReportToUserAsync(chatId, messageText, bodyHtml));
                await Task.WhenAll(tasks);
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
                Console.WriteLine($"Пользователь {chatId} заблокировал бота и был удален из списка");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения {chatId}: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="messageText"></param>
        /// <param name="bodyHtml"></param>
        /// <returns></returns>
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
                Console.WriteLine($"Пользователь {chatId} заблокировал бота и был удален из списка");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки отчета пользователю {chatId}: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="bodyHtml"></param>
        /// <returns></returns>
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
                Console.WriteLine($"Пользователь {chatId} заблокировал бота и был удален из списка");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения {chatId}: {ex.Message}");
            }
        }
    }
}