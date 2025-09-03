namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Информация о проблемном пакете КТРУ
    /// </summary>
    public class KtruPackageInfo
    {
        /// <summary>
        /// Идентификатор пакета
        /// </summary>
        public int PackageId { get; set; }

        /// <summary>
        /// Дата создания пакета
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Количество дней в ожидании
        /// </summary>
        public int DaysPending { get; set; }

        /// <summary>
        /// Размер пакета (если доступен)
        /// </summary>
        public DateTime? UpdateDate { get; set; }
    }
}
