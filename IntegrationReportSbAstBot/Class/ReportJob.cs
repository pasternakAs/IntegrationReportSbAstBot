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
        private readonly TelegramBotClient _bot;
        private readonly ILogger<ReportJob> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        public ReportJob(TelegramBotClient bot, ILogger<ReportJob> logger)
        {
            _bot = bot;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Начало выполнения ReportJob в {Time}", DateTime.Now);

            try
            {
                const string connStr = "Server=.;Database=CDB;Trusted_Connection=True;";
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
                            Direction = r.GetInt32(2) == 1 ? "EIS --> ETP" : "EIS <-- ETP",
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

                await _bot.SendMessage(
                    chatId: 123456789, // замените
                    text: $"Отчёт по важным пакетам ({total} шт.) за последние сутки",
                    parseMode: ParseMode.Html);

                string file = $"report_{DateTime.Now:yyyyMMdd}.html";
                await File.WriteAllTextAsync(file, bodyHtml);

                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                await _bot.SendDocument(
                    chatId: 6426583094,//@xamfess
                    document: InputFile.FromStream(fs, Path.GetFileName(file)));
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
    }
}