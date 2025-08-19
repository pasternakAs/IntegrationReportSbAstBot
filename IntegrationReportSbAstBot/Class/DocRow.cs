using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Class
{
    internal class DocRow
    {
        public string DocType { get; set; }
        public string Violations { get; set; }
        public string Direction { get; set; }
        public string ObjectId { get; set; }
        public DateTime LastSendDate { get; set; }
    }
}
