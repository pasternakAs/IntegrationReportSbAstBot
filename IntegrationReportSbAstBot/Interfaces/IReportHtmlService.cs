using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationReportSbAstBot.Class;

namespace IntegrationReportSbAstBot.Interfaces
{
    public interface IReportHtmlService
    {
        string GenerateHtmlReport(ReportDataClass reportData);
    }
}
