using System.Text;
using Dapper;
using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationReportSbAstBot.Services
{
    public class GenerateReportForProcedure : IProcedureInfoService
    {
        private readonly ILogger _logger;
        private readonly IDbConnectionFactory _connectionFactory;

        public GenerateReportForProcedure(ILogger logger, IDbConnectionFactory dbConnectionFactory)
        {
            _logger = logger;
            _connectionFactory = dbConnectionFactory;
        }

        public async Task<List<ProcedureInfo>> GetProcedureInfoAsync(string objectId, int? inOut = null)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
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
	        docOut.WaitingDescription,
        FROM v_docOOSDoc docOut WITH (NOLOCK)
        WHERE docOut.ObjectId = @pcode
          AND docOut.InOut = ISNULL(@inout, docOut.InOut)
          AND state <> -3
        ORDER BY docOut.ObjectId, docOut.OOSDocId ASC";

                var result = await connection.QueryAsync<ProcedureInfo>(sql, new { ProcedureNumber = objectId });

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запросе информации по процедуре {ProcedureNumber}", objectId);
                throw;
            }
        }

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

        private string GetStateDescription(int state)
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

        public List<string> SplitMessage(string message)
        {
            var parts = new List<string>();
            const int maxLength = 4096;

            if (message.Length <= maxLength)
            {
                parts.Add(message);
                return parts;
            }

            var lines = message.Split('\n');
            var currentPart = new StringBuilder();

            foreach (var line in lines)
            {
                if (currentPart.Length + line.Length + 1 > maxLength)
                {
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                }

                currentPart.AppendLine(line);
            }

            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
            }

            return parts;
        }
    }
}