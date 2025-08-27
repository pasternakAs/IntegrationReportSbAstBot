namespace IntegrationReportSbAstBot.Services
{
    public interface IBotStateService
    {
        Task<bool> IsEnabledAsync();
        Task SetEnabledAsync(bool enabled);
    }
}