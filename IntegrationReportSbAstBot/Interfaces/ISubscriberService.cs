namespace IntegrationReportSbAstBot.Interfaces
{
    public interface ISubscriberService
    {
        Task SubscribeUserAsync(long chatId);
        Task UnsubscribeUserAsync(long chatId);
        Task<List<long>> GetSubscribersAsync();
    }
}
