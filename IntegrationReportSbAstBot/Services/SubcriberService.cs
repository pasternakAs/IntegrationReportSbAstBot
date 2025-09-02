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
        public async Task SubscribeUserAsync(long chatId, bool isGroup = false, string chatName = null)
        {
            try
            {
                // Сохраняем подписчика в базу данных и проверяем результат
                var isSaved = await SaveSubscriberToDatabase(chatId, isGroup, chatName, true);

                if (isSaved)
                {
                    lock (_lock)
                    {
                        if (!_subscribers.Contains(chatId))
                        {
                            _subscribers.Add(chatId);
                        }
                    }

                    _logger.LogInformation("{ChatType} {ChatId} успешно подписался на рассылку",
                        isGroup ? "Группа" : "Пользователь", chatId);
                }
                else
                {
                    _logger.LogWarning("Не удалось сохранить подписку для чата {ChatId} в базу данных", chatId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подписке чата {ChatId}", chatId);
                // Даже если ошибка БД, добавляем в память для временной работы
                lock (_lock)
                {
                    if (!_subscribers.Contains(chatId))
                    {
                        _subscribers.Add(chatId);
                    }
                }
            }
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
        public async Task UnsubscribeUserAsync(long chatId)
        {
            try
            {
                // Обновляем статус подписчика в базе данных и проверяем результат
                var isSaved = await SaveSubscriberToDatabase(chatId, false, null, false);

                if (isSaved)
                {
                    lock (_lock)
                    {
                        _subscribers.Remove(chatId);
                    }

                    _logger.LogInformation("Чат {ChatId} успешно отписался от рассылки", chatId);
                }
                else
                {
                    _logger.LogWarning("Не удалось сохранить отписку для чата {ChatId} в базу данных", chatId);
                    // Удаляем из памяти даже если БД не обновилась
                    lock (_lock)
                    {
                        _subscribers.Remove(chatId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отписке чата {ChatId}", chatId);
                // Удаляем из памяти даже при ошибке БД
                lock (_lock)
                {
                    _subscribers.Remove(chatId);
                }
            }
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
        /// Сохраняет или обновляет информацию о подписчике в базе данных
        /// </summary>
        /// <param name="chatId">Идентификатор чата</param>
        /// <param name="isGroup">Флаг группы</param>
        /// <param name="chatName">Название чата</param>
        /// <param name="isActive">Статус активности подписки</param>
        /// <returns>Асинхронная задача, возвращающая true если операция успешна, иначе false</returns>
        /// <remarks>
        /// Использует параметризованный SQL-запрос для предотвращения SQL-инъекций
        /// Логирует ошибки базы данных для диагностики проблем
        /// Возвращает false в случае ошибок базы данных или отсутствия пользователя
        /// </remarks>
        public async Task<bool> SaveSubscriberToDatabase(long chatId, bool isGroup, string chatName, bool isActive)
        {
            try
            {
                await using var connection = _sqliteConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = @"
                    INSERT OR REPLACE INTO Subscribers 
                    (ChatId, IsGroup, ChatName, SubscribedAt, IsActive, LastUpdated)
                    VALUES 
                    (@ChatId, @IsGroup, @ChatName, 
                     CASE WHEN EXISTS(SELECT 1 FROM Subscribers WHERE ChatId = @ChatId) 
                          THEN (SELECT SubscribedAt FROM Subscribers WHERE ChatId = @ChatId)
                          ELSE @CurrentTime END,
                     @IsActive, @CurrentTime)";

                var result = await connection.ExecuteAsync(sql, new
                {
                    ChatId = chatId,
                    IsGroup = isGroup ? 1 : 0,
                    ChatName = chatName,
                    IsActive = isActive ? 1 : 0,
                    CurrentTime = DateTime.UtcNow.ToString("o")
                });

                // ExecuteAsync возвращает количество затронутых строк
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения подписчика {ChatId} в базу данных", chatId);
                return false;
            }
        }

        /// <summary>
        /// Получает всех активных подписчиков из базы данных
        /// </summary>
        /// <returns>Список идентификаторов активных подписчиков</returns>
        public async Task<List<long>> GetActiveSubscribersFromDatabaseAsync()
        {
            try
            {
                await using var connection = _sqliteConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                const string sql = "SELECT ChatId FROM Subscribers WHERE IsActive = 1";
                var result = await connection.QueryAsync<long>(sql);

                return [.. result];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения активных подписчиков из базы данных");
                return [];
            }
        }

        /// <summary>
        /// Синхронизирует список подписчиков в памяти с базой данных
        /// </summary>
        /// <returns>Асинхронная задача завершения синхронизации</returns>
        public async Task SyncSubscribersAsync()
        {
            try
            {
                var databaseSubscribers = await GetActiveSubscribersFromDatabaseAsync();

                lock (_lock)
                {
                    // Очищаем текущий список и добавляем подписчиков из базы
                    _subscribers.Clear();
                    _subscribers.AddRange(databaseSubscribers);
                }

                _logger.LogInformation("Список подписчиков синхронизирован. Всего подписчиков: {Count}", databaseSubscribers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка синхронизации подписчиков");
            }
        }
    }
}