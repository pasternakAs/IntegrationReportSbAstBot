namespace IntegrationReportSbAstBot.Class
{
    public class BotSettings
    {
        public bool IsEnabled { get; set; } = true;
        public List<long> AdminUserIds { get; set; } = new();
        public string MaintenanceMessage { get; set; } = "Бот временно выключен.";
    }
}