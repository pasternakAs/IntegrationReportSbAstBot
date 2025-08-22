using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Запрос на авторизацию пользователя
    /// </summary>
    public class AuthorizationRequest
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public long ChatId { get; set; }
        public DateTime RequestedAt { get; set; }
        public string RequestMessage { get; set; }
        public bool IsApproved { get; set; }
        public bool IsProcessed { get; set; }
        public long? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
