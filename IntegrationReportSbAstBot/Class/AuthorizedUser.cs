using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Авторизованный пользователь
    /// </summary>
    public class AuthorizedUser
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public long ChatId { get; set; }
        public DateTime AuthorizedAt { get; set; }
        public long AuthorizedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; }
    }
}
