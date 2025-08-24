using IntegrationReportSbAstBot.Class;

namespace IntegrationReportSbAstBot.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса управления авторизацией пользователей
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Проверяет, авторизован ли пользователь
        /// </summary>
        Task<bool> IsUserAuthorizedAsync(long userId);

        /// <summary>
        /// Создает запрос на авторизацию
        /// </summary>
        Task CreateAuthorizationRequestAsync(long userId, string userName, long chatId, string requestMessage);

        /// <summary>
        /// Одобряет запрос на авторизацию
        /// </summary>
        Task ApproveAuthorizationRequestAsync(long requestId, long adminId);

        /// <summary>
        /// Получает список ожидающих запросов
        /// </summary>
        Task<List<AuthorizationRequest>> GetPendingRequestsAsync();

        /// <summary>
        /// Получает список авторизованных пользователей
        /// </summary>
        Task<List<AuthorizedUser>> GetAuthorizedUsersAsync();

        /// <summary>
        /// Отзывает доступ у пользователя
        /// </summary>
        Task RevokeUserAccessAsync(long userId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        Task<AuthorizationRequest> GetAuthorizationRequestById(long requestId);
    }
}