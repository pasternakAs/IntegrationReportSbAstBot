using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationReportSbAstBot.Interfaces;

namespace IntegrationReportSbAstBot.Services
{
    public class SubscriberService : ISubscriberService
    {
        private static readonly List<long> _subscribers = [];
        private static readonly object _lock = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public Task SubscribeUserAsync(long chatId)
        {
            lock (_lock)
            {
                if (!_subscribers.Contains(chatId))
                {
                    _subscribers.Add(chatId);
                    Console.WriteLine($"Пользователь {chatId} подписался");
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public Task UnsubscribeUserAsync(long chatId)
        {
            lock (_lock)
            {
                _subscribers.Remove(chatId);
                Console.WriteLine($"Пользователь {chatId} отписался");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<List<long>> GetSubscribersAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(new List<long>(_subscribers));
            }
        }

        /// <summary>
        /// Сохранить новго пользователя
        /// </summary>
        /// <param name="chatId">id chat user</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task SaveUserForBroadcast(long chatId)
        {
            throw new NotImplementedException();
        }
    }
}