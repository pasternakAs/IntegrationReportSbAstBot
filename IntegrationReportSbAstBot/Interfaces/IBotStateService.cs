namespace IntegrationReportSbAstBot.Interfaces
{
    public interface IBotStateService
    {
        Task InitializeAsync();
        Task<bool> IsEnabledAsync();
        Task SetEnabledAsync(bool enabled);
    }
}