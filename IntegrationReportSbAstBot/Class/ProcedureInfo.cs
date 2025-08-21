using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Class
{
    public class ProcedureInfo
    {
        public string Act { get; set; }
        public string ViolationsXML { get; set; }
        public int OOSDocId { get; set; }
        public string ProtocolNumber { get; set; }
        public string IndexNum { get; set; }
        public int State { get; set; }
        public string DocType { get; set; }
        public string OOSDocGuid { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastSendDate { get; set; }
        public string DocID { get; set; }
        public string WaitingDescription { get; set; }
    }
}
