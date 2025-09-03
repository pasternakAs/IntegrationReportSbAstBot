namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Данные мониторинга пакетов КТРУ
    /// </summary>
    public class KtruMonitoringData
    {
        /// <summary>
        /// Количество пакетов в статусе 1 (в обработке)
        /// </summary>
        public int PendingPackagesCount { get; set; }

        /// <summary>
        /// Дата проверки
        /// </summary>
        public DateTime CheckDate { get; set; }

        /// <summary>
        /// Дата начала периода мониторинга
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Детали проблемных пакетов (если нужно)
        /// </summary>
        public List<KtruPackageInfo> ProblemPackages { get; set; } = new();
    }
}