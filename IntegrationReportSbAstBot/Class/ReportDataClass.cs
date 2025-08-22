namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Представляет данные отчета для формирования и отправки пользователям
    /// Содержит сводную информацию, список пакетов и метаинформацию об отчете
    /// </summary>
    public class ReportDataClass
    {
        /// <summary>
        /// Краткий текстовый отчет для отображения в Telegram сообщении
        /// Содержит сводную информацию о количестве пакетов и основных данных
        /// </summary>
        /// <example>"Отчёт по важным пакетам (15 шт.) за последние сутки"</example>
        public string ShortReportText { get; set; }

        /// <summary>
        /// Общее количество пакетов документов в отчете
        /// Используется для формирования сводной статистики
        /// </summary>
        /// <example>15, 23, 7</example>
        public List<SummaryOfPackages> SummaryOfPackages { get; set; }

        /// <summary>
        /// Дата и время генерации отчета
        /// Временная метка создания отчета для отображения в заголовке
        /// </summary>
        /// <example>2024-01-15 14:30:00</example>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Список пакетов документов, включенных в отчет
        /// Содержит детализированную информацию о каждом пакете для HTML отчета
        /// </summary>
        /// <remarks>
        /// Может быть пустым списком, если нет данных для отчета
        /// </remarks>
        public List<PackageInfo> Packages { get; set; } = new List<PackageInfo>();
    }
}