using System.Net;
using System.Text;
using Dapper;
using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис для генерации отчетов по информации о процедурах
    /// Выполняет запросы к базе данных, форматирует данные и готовит отчеты для отправки через Telegram
    /// </summary>
    public class GenerateReportForProcedure(ILogger<GenerateReportForProcedure> logger, ISqlConnectionFactory sqlConnectionFactory) : IProcedureInfoService
    {
        private readonly ILogger<GenerateReportForProcedure> _logger = logger;
        private readonly ISqlConnectionFactory _sqlConnectionFactory = sqlConnectionFactory;

        /// <summary>
        /// Получает информацию о документах процедуры из базы данных
        /// Выполняет SQL-запрос к представлению v_docOOSDoc с фильтрацией по ObjectId
        /// </summary>
        /// <param name="objectId">Идентификатор объекта процедуры</param>
        /// <param name="inOut">Направление документов (0 - исходящие, 1 - входящие, null - все)</param>
        /// <returns>Асинхронная задача, возвращающая список информации о документах процедуры</returns>
        /// <exception cref="Exception">Выбрасывается при ошибках выполнения запроса к базе данных</exception>
        public async Task<List<ProcedureInfo>> GetProcedureInfoAsync(string objectId, int? inOut = null)
        {
            try
            {
                await using var connection = _sqlConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                SELECT
	            [act] = CASE 
		            WHEN docOut.InOut = 0 THEN 'AST --> EIS' 
		            WHEN docOut.InOut = 1 THEN 'AST <-- EIS' 
	                END,
	            [violationsXML] = TRY_CAST(docOut.violations AS NVARCHAR(MAX)),
	            docOut.OOSDocId,
	            [protocolNumber] =  COALESCE(
		            CAST(docOut.data AS XML).value('(//*:protocolNumber)[1]', 'NVARCHAR(MAX)'),
		            CAST(docOut.data AS XML).value('(//*:canceledProtocolNumber)[1]', 'NVARCHAR(MAX)'),
		            CAST(docOut.data AS XML).value('(//*:docNumber)[1]', 'NVARCHAR(MAX)'),
		            CAST(docOut.data AS XML).value('(//*:docNumberExternal)[1]', 'NVARCHAR(MAX)'),
		            dbo.xml2Str(CAST(docOut.data AS XML).query('//foundation/order/foundationProtocolNumber'))
	            ),
	            docOut.indexNum,
	            docOut.state,
	            docOut.docType,
	            [OOSDocGuid] = LOWER(docOut.OOSDocGuid),
	            docOut.CreateDate,
	            docOut.LastSendDate,
	            docOut.docID,
	            docOut.WaitingDescription
                FROM v_docOOSDoc docOut WITH (NOLOCK)
                WHERE docOut.ObjectId = @pcode
                  AND docOut.InOut = ISNULL(@inout, docOut.InOut)
                  AND state <> -3
                ORDER BY docOut.ObjectId, docOut.OOSDocId ASC";

                // Выполняем запрос с параметрами
                var parameters = new { pcode = objectId, inout = inOut };
                var result = await connection.QueryAsync<ProcedureInfo>(sql, parameters);

                return [.. result];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запросе информации по процедуре {ProcedureNumber}", objectId);
                throw;
            }
        }

        /// <summary>
        /// Форматирует список документов процедуры в HTML-сообщение для Telegram
        /// Преобразует структурированные данные в человекочитаемый формат с использованием HTML-тегов
        /// </summary>
        /// <param name="objectId">Идентификатор объекта процедуры</param>
        /// <param name="documents">Список документов процедуры для форматирования</param>
        /// <returns>Отформатированная строка с информацией о документах</returns>
        public string FormatProcedureDocuments(string objectId, List<ProcedureInfo> documents)
        {
            var sb = new StringBuilder();

            sb.Append($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Отчет по пакетам</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        table {{ border-collapse: collapse; width: 100%; margin-bottom: 20px; }}
                        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                        th {{ background-color: #f2f2f2; }}
                        tr:nth-child(even) {{ background-color: #f9f9f9; }}
                    </style>
                </head>
                <body>
                    <h1>Отчет по процедуре</h1>
                ");

            sb.AppendLine($"<b>Документы по процедуре: {objectId}</b>\n");
            sb.Append($"<i>Всего документов: {documents.Count}</i>\n");

            sb.Append(GenerateDetailTable(documents));

            sb.Append("</body></html>");

            return sb.ToString();
        }

        /// <summary>
        /// Преобразует числовой код состояния документа в текстовое описание
        /// Используется для отображения понятного статуса в отчетах
        /// </summary>
        /// <param name="state">Числовой код состояния документа</param>
        /// <returns>Текстовое описание состояния</returns>
        private static string GetStateDescription(int state)
        {
            return state switch
            {
                -1 => "Ошибка",
                -2 => "Предупреждение",
                1 => "Провалидирован",
                2 => "Ожидает принятия",
                0 => "В обработке",
                3 => "Принят",
                _ => state.ToString()
            };
        }

        /// <summary>
        /// Разбивает длинное сообщение на части для отправки через Telegram
        /// Telegram ограничивает длину сообщения 4096 символами
        /// </summary>
        /// <param name="message">Исходное сообщение для разбиения</param>
        /// <returns>Список частей сообщения, каждая не более 4096 символов</returns>
        public List<string> SplitMessage(string message)
        {
            var parts = new List<string>();
            const int maxLength = 4096;

            // Если сообщение короче лимита, возвращаем как есть
            if (message.Length <= maxLength)
            {
                parts.Add(message);
                return parts;
            }

            // Разбиваем по строкам для сохранения целостности
            var lines = message.Split('\n');
            var currentPart = new StringBuilder();

            foreach (var line in lines)
            {
                // Если добавление строки превысит лимит, сохраняем текущую часть
                if (currentPart.Length + line.Length + 1 > maxLength)
                {
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                }

                currentPart.AppendLine(line);
            }

            // Добавляем последнюю часть если она не пуста
            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
            }

            return parts;
        }

        /// <summary>
        /// Генерирует детализированную таблицу по каждому пакету.
        /// Для каждого пакета выводятся:
        ///  - идентификатор процедуры (ObjectId),
        ///  - тип пакета (DocumentType),
        ///  - ошибка (Violations),
        ///  - направление (InOut),
        ///  - последняя дата отправки (LastSendDate).
        /// </summary>
        /// <param name="listProcedureInfo">Данные по пакетам.</param>
        /// <returns>HTML-код таблицы.</returns>
        private static string GenerateDetailTable(List<ProcedureInfo> listProcedureInfo)
        {
            var sb = new StringBuilder();

            sb.Append(@"
                <h2>Детализация</h2>
                <table>
                    <thead>
                        <tr>
                            <th>Тип</th>
                            <th>Номер протокола</th>
                            <th>Направление</th>
                            <th>Статус</th>
                            <th>Тип статуса</th>
                            <th>Создан</th>
                            <th>Отправлен</th>
                        </tr>
                    </thead>
                    <tbody>");

            foreach (var procedure in listProcedureInfo.OrderByDescending(p => p.LastSendDate))
            {
                sb.Append($@"
                <tr>
                    <td>{WebUtility.HtmlEncode(procedure.DocType.ToString())}</td>
                    <td>{WebUtility.HtmlEncode(procedure.ProtocolNumber)}</td>
                    <td>{WebUtility.HtmlEncode(procedure.Act)}</td>
                    <td>{WebUtility.HtmlEncode(procedure.State.ToString())}</td>
                    <td>{WebUtility.HtmlEncode(GetStateDescription(procedure.State))}</td>
                    <td>{procedure.CreateDate:dd.MM.yyyy HH:mm}</td>
                    <td>{procedure.LastSendDate:dd.MM.yyyy HH:mm}</td>
                </tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }
    }
}