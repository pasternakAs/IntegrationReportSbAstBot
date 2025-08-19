using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationReportSbAstBot.Interfaces
{
    public interface ISubscriberService
    {
        Task SubscribeUserAsync(long chatId);
        Task UnsubscribeUserAsync(long chatId);
        Task<List<long>> GetSubscribersAsync();
    }
}
