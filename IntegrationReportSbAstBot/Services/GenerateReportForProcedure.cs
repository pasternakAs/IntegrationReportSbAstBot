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
                using var connection = _sqlConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                var sql = @"
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

                return result.ToList();
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
            sb.AppendLine($"<b>Документы по процедуре: {objectId}</b>\n");

            foreach (var doc in documents)
            {
                sb.AppendLine($"📝 <b>Тип:</b> {doc.DocType}");
                sb.AppendLine($"🔢 <b>Номер протокола:</b> {doc.ProtocolNumber ?? "Нет"}");
                sb.AppendLine($"🔄 <b>Направление:</b> {doc.Act}");
                sb.AppendLine($"📊 <b>Статус:</b> {GetStateDescription(doc.State)}");
                sb.AppendLine($"📅 <b>Создан:</b> {doc.CreateDate:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"📤 <b>Отправлен:</b> {doc.LastSendDate:dd.MM.yyyy HH:mm}");

                if (!string.IsNullOrEmpty(doc.ViolationsXML))
                {
                    sb.AppendLine($"⚠️ <b>Нарушения:</b> Есть нарушения");
                }

                sb.AppendLine($"🔗 <b>ID:</b> {doc.OOSDocId}");
                sb.AppendLine();
            }

            sb.AppendLine($"<i>Всего документов: {documents.Count}</i>");

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
                0 => "Обработан",
                1 => "В обработке",
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
    }
}