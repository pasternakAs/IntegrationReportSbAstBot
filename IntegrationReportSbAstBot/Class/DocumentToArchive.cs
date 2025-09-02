using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Модель документа для архивирования
    /// </summary>
    public class DocumentToArchive
    {
        public int OOSDocId { get; set; }
        public string ObjectId { get; set; }
        public int InOut { get; set; }
        public int IndexNum { get; set; }
    }
}
