using IntegrationReportSbAstBot.Interfaces;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис управления подписчиками для Telegram бота
    /// Обеспечивает добавление, удаление и получение списка подписчиков
    /// Использует потокобезопасное хранение данных в памяти
    /// </summary>
    public class SubscriberService : ISubscriberService
    {
        private static readonly List<long> _subscribers = [];
        private static readonly object _lock = new();

        /// <summary>
        /// Добавляет пользователя в список подписчиков
        /// Если пользователь уже подписан, операция игнорируется
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя в Telegram</param>
        /// <returns>Асинхронная задача завершения операции</returns>
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
        /// Удаляет пользователя из списка подписчиков
        /// Если пользователь не найден в списке, операция игнорируется
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя в Telegram</param>
        /// <returns>Асинхронная задача завершения операции</returns>
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
        /// Получает копию списка всех подписчиков
        /// Возвращает новый список для предотвращения модификации внутреннего состояния
        /// </summary>
        /// <returns>Асинхронная задача, содержащая список идентификаторов чатов подписчиков</returns>
        public Task<List<long>> GetSubscribersAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(new List<long>(_subscribers));
            }
        }

        /// <summary>
        /// Метод-заглушка для будущей реализации сохранения подписчиков в постоянное хранилище
        /// В текущей реализации выбрасывает NotImplementedException
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя в Telegram</param>
        /// <returns>Асинхронная задача</returns>
        /// <exception cref="NotImplementedException">Метод еще не реализован</exception>
        public async Task SaveUserForBroadcast(long chatId)
        {
            throw new NotImplementedException();
        }
    }
}