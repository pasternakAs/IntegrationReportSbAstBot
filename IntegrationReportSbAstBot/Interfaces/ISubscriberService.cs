namespace IntegrationReportSbAstBot.Interfaces
{
    public interface ISubscriberService
    {
        Task SubscribeUserAsync(long chatId, bool isGroup = false, string chatName = null);
        Task UnsubscribeUserAsync(long chatId);
        Task<List<long>> GetSubscribersAsync();
        Task SyncSubscribersAsync();
    }
}
