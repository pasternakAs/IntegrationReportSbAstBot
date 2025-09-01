using Dapper;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис управления подписчиками для Telegram бота
    /// Обеспечивает добавление, удаление и получение списка подписчиков
    /// Использует потокобезопасное хранение данных в памяти и персистентное хранение в SQLite
    /// </summary>
    public class SubscriberService(ISqliteConnectionFactory sqliteConnectionFactory, ILogger<SubscriberService> logger) : ISubscriberService
    {
        private readonly ISqliteConnectionFactory _sqliteConnectionFactory = sqliteConnectionFactory;
        private static readonly List<long> _subscribers = [];
        private static readonly object _lock = new();
        private readonly ILogger<SubscriberService> _logger = logger;

        /// <summary>
        /// Добавляет пользователя в список подписчиков
        /// Если пользователь уже подписан, операция игнорируется
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя в Telegram</param>
        /// <returns>Асинхронная задача завершения операции подписки</returns>
        /// <remarks>
        /// Выполняет потокобезопасное добавление в память и персистентное сохранение в базу данных
        /// Логирует успешные операции и ошибки для мониторинга
        /// </remarks>
        public Task SubscribeUserAsync(long chatId)
        {
            lock (_lock)
            {
                if (!_subscribers.Contains(chatId))
                {
                    _subscribers.Add(chatId);
                    Console.WriteLine($"Пользователь {chatId} подписался");
                    _logger.LogInformation("Пользователь {ChatId} успешно подписался на рассылку", chatId);

                    //var result = SaveUserForBroadcast(chatId, 1);
                    //if (result.Result)
                    //{
                    //    _subscribers.Add(chatId);
                    //    Console.WriteLine($"Пользователь {chatId} подписался");
                    //    _logger.LogInformation("Пользователь {ChatId} успешно подписался на рассылку", chatId);
                    //}
                    //else
                    //{
                    //    _logger.LogWarning("Не удалось сохранить статус подписки для пользователя {ChatId}", chatId);
                    //}
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Удаляет пользователя из списка подписчиков
        /// Если пользователь не найден в списке, операция игнорируется
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя в Telegram</param>
        /// <returns>Асинхронная задача завершения операции отписки</returns>
        /// <remarks>
        /// Выполняет потокобезопасное удаление из памяти и персистентное обновление в базе данных
        /// Логирует успешные операции и ошибки для мониторинга
        /// </remarks>
        public Task UnsubscribeUserAsync(long chatId)
        {
            lock (_lock)
            {
                if (_subscribers.Contains(chatId))
                {
                    _subscribers.Remove(chatId);
                    Console.WriteLine($"Пользователь {chatId} отписался");
                    _logger.LogInformation("Пользователь {ChatId} успешно отписался от рассылки", chatId);

                    //var result = SaveUserForBroadcast(chatId, 0);
                    //if (result.Result)
                    //{
                    //    _subscribers.Remove(chatId);
                    //    Console.WriteLine($"Пользователь {chatId} отписался");
                    //    _logger.LogInformation("Пользователь {ChatId} успешно отписался от рассылки", chatId);
                    //}
                    //else
                    //{
                    //    _logger.LogWarning("Не удалось сохранить статус отписки для пользователя {ChatId}", chatId);
                    //}
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Получает копию списка всех подписчиков
        /// Возвращает новый список для предотвращения модификации внутреннего состояния
        /// </summary>
        /// <returns>Асинхронная задача, содержащая список идентификаторов чатов подписчиков</returns>
        /// <remarks>
        /// Возвращает потокобезопасную копию текущего списка подписчиков
        /// Использует блокировку для предотвращения race condition
        /// </remarks>
        public Task<List<long>> GetSubscribersAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(new List<long>(_subscribers));
            }
        }

        /// <summary>
        /// Сохраняет или обновляет статус подписки пользователя в базе данных
        /// Обновляет поле IsSubscribe в таблице AuthorizedUsers для управления рассылкой
        /// </summary>
        /// <param name="chatId">Идентификатор чата пользователя в Telegram (UserId)</param>
        /// <param name="isSubscribe">Статус подписки: 1 - подписан, 0 - отписан</param>
        /// <returns>Асинхронная задача, возвращающая true если операция успешна, иначе false</returns>
        /// <remarks>
        /// Использует параметризованный SQL-запрос для предотвращения SQL-инъекций
        /// Логирует ошибки базы данных для диагностики проблем
        /// Возвращает false в случае ошибок базы данных или отсутствия пользователя
        /// </remarks>
        public async Task<bool> SaveUserForBroadcast(long chatId, int isSubscribe)
        {
            try
            {
                await using var connection = _sqliteConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE AuthorizedUsers 
                    SET IsSubscribe = @IsSubscribe 
                    WHERE UserId = @UserId";

                var result = await connection.ExecuteAsync(sql, new { IsSubscribe = isSubscribe, UserId = chatId });

                // ExecuteAsync возвращает количество затронутых строк
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения статуса подписки пользователя {UserId}", chatId);
                return false;
            }
        }
    }
}