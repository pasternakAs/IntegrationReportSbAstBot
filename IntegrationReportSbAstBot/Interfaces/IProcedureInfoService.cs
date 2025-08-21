using IntegrationReportSbAstBot.Class;

namespace IntegrationReportSbAstBot.Interfaces
{
    /// <summary>
    /// Сервис для работы с информацией о процедурах
    /// </summary>
    public interface IProcedureInfoService
    {
        /// <summary>
        /// Получает список документов по номеру процедуры
        /// </summary>
        /// <param name="objectId">Номер процедуры</param>
        /// <param name="inOut">Направление документов (0 - исходящие, 1 - входящие, null - все)</param>
        /// <returns>Список документов процедуры</returns>
        public Task<List<ProcedureInfo>> GetProcedureInfoAsync(string objectId, int? inOut = null);

        /// <summary>
        /// Форматирует список документов в текстовое представление для Telegram
        /// </summary>
        /// <param name="objectId">Номер процедуры</param>
        /// <param name="documents">Список документов</param>
        /// <returns>Отформатированный текст</returns>
        public string FormatProcedureDocuments(string objectId, List<ProcedureInfo> documents);

        /// <summary>
        /// Разбивает длинное сообщение на части для отправки в Telegram
        /// </summary>
        /// <param name="message">Исходное сообщение</param>
        /// <returns>Список частей сообщения</returns>
       public List<string> SplitMessage(string message);
    }
}