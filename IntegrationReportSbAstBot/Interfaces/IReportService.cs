using IntegrationReportSbAstBot.Class;

namespace IntegrationReportSbAstBot.Interfaces
{
    public interface IReportService
    {
        Task<ReportDataClass> GenerateReportAsync();
    }
}
