namespace IntegrationReportSbAstBot.Interfaces
{
    public interface IBotStateService
    {
        Task<bool> IsEnabledAsync();
        Task SetEnabledAsync(bool enabled);
    }
}