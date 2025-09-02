namespace IntegrationReportSbAstBot.Class
{
    public class Subscriber
    {
        public string ChatName { get; set; } = string.Empty;
        public long ChatId { get; set; }
        public DateTime SubscribedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsActive { get; set; }
        public bool IsGroup { get; set; }
    }
}