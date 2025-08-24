namespace IntegrationReportSbAstBot.Class
{
    public class BotSettings
    {
        public bool IsEnabled { get; set; } = true;
        public List<long> AdminUserIds { get; set; } = new();
        public string UnauthorizedMessage { get; set; } = "❌ Доступ запрещен. Обратитесь к администратору.";
        public string MaintenanceMessage { get; set; } = "❌ Бот временно недоступен. Технические работы.";
    }
}