using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Class.Options
{
    public class TelegramOptions
    {
        public string BotToken { get; set; } = string.Empty;
    }

    public class QuartzJobOptions
    {
        public string CronSchedule { get; set; } = string.Empty;
    }

    public class DatabaseOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int CommandTimeout { get; set; } = 30;
    }
}