using IntegrationReportSbAstBot.Class;

namespace IntegrationReportSbAstBot.Interfaces
{
    public interface IReportHtmlService
    {
        string GenerateHtmlReport(ReportDataClass reportData);
    }
}
