using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Class
{
    public class ReportDataClass
    {
        public string ShortReportText { get; set; }
        public int TotalCount { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<PackageInfo> Packages { get; set; }
    }
}
