using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Class
{
    public class PackageInfo
    {
        public string DocumentType { get; set; }
        public string Violations { get; set; }
        public string InOut { get; set; }
        public int ObjectId { get; set; }
        public DateTime LastSendDate { get; set; }
    }
}
