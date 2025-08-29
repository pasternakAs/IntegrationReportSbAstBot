using Dapper;
using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис управления авторизацией пользователей
    /// Обрабатывает запросы на доступ, одобрения и проверку прав
    /// </summary>
    /// <remarks>
    /// Инициализирует сервис авторизации
    /// </remarks>
    /// <param name="sqliteConnectionFactory">Фабрика подключений к базе данных</param>
    /// <param name="logger">Логгер для записи событий</param>
    public class AuthorizationService(ISqliteConnectionFactory sqliteConnectionFactory, ILogger<AuthorizationService> logger, IOptions<BotSettings> botSettings) : IAuthorizationService
    {
        private readonly ISqliteConnectionFactory _sqliteConnectionFactory = sqliteConnectionFactory;
        private readonly ILogger<AuthorizationService> _logger = logger;
        private readonly BotSettings _botSettings = botSettings.Value; // Добавляем настройки

        /// <summary>
        /// Проверяет, авторизован ли пользователь
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>True если пользователь авторизован и активен</returns>
        public async Task<bool> IsUserAuthorizedAsync(long userId)
        {
            // Проверяем, является ли пользователь администратором из конфигурации
            if (_botSettings.AdminUserIds.Contains(userId))
            {
                return true; // Админы всегда авторизованы
            }

            try
            {
                await using var connection = _sqliteConnectionFactory.CreateConnection();

                const string sql = "SELECT COUNT(1) FROM AuthorizedUsers WHERE UserId = @UserId AND IsActive = 1";
                var result = await connection.QuerySingleAsync<int>(sql, new { UserId = userId });

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки авторизации пользователя {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Создает запрос на авторизацию
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="chatId">ID чата</param>
        /// <param name="requestMessage">Сообщение с запросом</param>
        public async Task CreateAuthorizationRequestAsync(long userId, string userName, long chatId, string requestMessage)
        {
            if (string.IsNullOrWhiteSpace(userName)) userName = "Unknown";
            if (string.IsNullOrWhiteSpace(requestMessage)) requestMessage = "-";

            try
            {
                using var connection = _sqliteConnectionFactory.CreateConnection();

                const string sql = @"
                    INSERT INTO AuthorizationRequests 
                    (UserId, UserName, ChatId, RequestedAt, RequestMessage, IsApproved, IsProcessed)
                    VALUES 
                    (@UserId, @UserName, @ChatId, @RequestedAt, @RequestMessage, 0, 0)";

                await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    UserName = userName,
                    ChatId = chatId,
                    RequestedAt = DateTime.UtcNow,
                    RequestMessage = requestMessage.Trim()
                });

                _logger.LogInformation("Создан запрос на авторизацию для пользователя {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания запроса авторизации для пользователя {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Одобряет запрос на авторизацию
        /// </summary>
        /// <param name="requestId">ID запроса</param>
        /// <param name="adminId">ID администратора</param>
        public async Task ApproveAuthorizationRequestAsync(long requestId, long adminId)
        {
            await using var connection = _sqliteConnectionFactory.CreateConnection();
            await using var transaction = connection.BeginTransaction();

            try
            {
                // Получаем данные запроса
                const string getRequestSql = "SELECT UserId, UserName, ChatId FROM AuthorizationRequests WHERE Id = @RequestId";
                var request = await connection.QueryFirstOrDefaultAsync<AuthorizationRequest>(getRequestSql, new { RequestId = requestId }) ?? throw new InvalidOperationException("Запрос не найден");

                // Обновляем статус запроса
                const string updateRequestSql = @"
                    UPDATE AuthorizationRequests 
                    SET IsApproved = 1, IsProcessed = 1, ProcessedBy = @AdminId, ProcessedAt = @ProcessedAt
                    WHERE Id = @RequestId";

                await connection.ExecuteAsync(updateRequestSql, new
                {
                    RequestId = requestId,
                    AdminId = adminId,
                    ProcessedAt = DateTime.UtcNow
                });

                // Добавляем пользователя в авторизованные
                const string insertUserSql = @"
                    INSERT OR REPLACE INTO AuthorizedUsers 
                    (UserId, UserName, ChatId, AuthorizedAt, AuthorizedBy, IsActive)
                    VALUES 
                    (@UserId, @UserName, @ChatId, @AuthorizedAt, @AuthorizedBy, 1)";

                await connection.ExecuteAsync(insertUserSql, new
                {
                    request.UserId,
                    request.UserName,
                    request.ChatId,
                    AuthorizedAt = DateTime.UtcNow,
                    AuthorizedBy = adminId
                });

                transaction.Commit();

                _logger.LogInformation(
                           "Одобрен запрос на авторизацию #{RequestId} для пользователя {UserId} администратором {AdminId}",
                           requestId, request.UserId, adminId);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Ошибка одобрения запроса авторизации #{RequestId}", requestId);
                throw;
            }
        }

        /// <summary>
        /// Получает список ожидающих запросов
        /// </summary>
        /// <returns>Список запросов на авторизацию</returns>
        public async Task<List<AuthorizationRequest>> GetPendingRequestsAsync()
        {
            try
            {
                await using var connection = _sqliteConnectionFactory.CreateConnection();
                const string sql = "SELECT * " +
                    "FROM AuthorizationRequests " +
                    "WHERE IsProcessed = 0 " +
                    "ORDER BY RequestedAt ASC";

                var result = await connection.QueryAsync<AuthorizationRequest>(sql);

                return [.. result];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения списка запросов авторизации");
                return [];
            }
        }

        /// <summary>
        /// Получает список авторизованных пользователей
        /// </summary>
        /// <returns>Список авторизованных пользователей</returns>
        public async Task<List<AuthorizedUser>> GetAuthorizedUsersAsync()
        {
            try
            {
                await using var connection = _sqliteConnectionFactory.CreateConnection();
                const string sql = "SELECT * FROM AuthorizedUsers WHERE IsActive = 1 ORDER BY AuthorizedAt DESC";
                var result = await connection.QueryAsync<AuthorizedUser>(sql);

                return [.. result];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения списка авторизованных пользователей");
                return new List<AuthorizedUser>();
            }
        }

        /// <summary>
        /// Отзывает доступ у пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        public async Task RevokeUserAccessAsync(long userId)
        {
            try
            {
                using var connection = _sqliteConnectionFactory.CreateConnection();
                const string sql = "UPDATE AuthorizedUsers SET IsActive = 0 WHERE UserId = @UserId";

                await connection.ExecuteAsync(sql, new { UserId = userId });

                _logger.LogInformation("Отозван доступ у пользователя {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отзыва доступа у пользователя {UserId}", userId);
                throw;
            }
        }

        public async Task<AuthorizationRequest?> GetAuthorizationRequestById(long requestId)
        {
            try
            {
                await using var connection = _sqliteConnectionFactory.CreateConnection();
                const string sql = "SELECT * FROM AuthorizationRequests WHERE Id = @RequestId";

                return await connection.QueryFirstOrDefaultAsync<AuthorizationRequest>(sql, new { RequestId = requestId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения запроса авторизации #{RequestId}", requestId);
                return null;
            }
        }
    }
}