using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды одобрения запросов на авторизацию
    /// Позволяет администраторам одобрять запросы пользователей на доступ к функциональности бота
    /// </summary>
    public class ApproveCommandHandler(ITelegramBotClient botClient, ILogger<ApproveCommandHandler> logger, IAuthorizationService authorizationService) : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ILogger<ApproveCommandHandler> _logger = logger;
        private readonly IAuthorizationService _authorizationService = authorizationService;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/approve" - команда для одобрения запросов на авторизацию</value>
        public string Command => "/approve";

        /// <summary>
        /// Обрабатывает команду одобрения запроса на авторизацию
        /// Проверяет параметры команды, одобряет запрос и уведомляет пользователя
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду и параметры</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки команды</returns>
        /// <remarks>
        /// Формат команды: /approve [request_id]
        /// Только пользователи из списка администраторов могут выполнять эту команду
        /// После одобрения пользователь получает уведомление о предоставлении доступа
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var adminChatId = message.Chat.Id;
            var adminId = message.From?.Id ?? 0;
            var fullCommand = message.Text ?? "";

            // Извлекаем request_id из команды
            var parts = fullCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts?.Length < 2 || !long.TryParse(parts[1], out var requestId))
            {
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: "❌ Использование: /approve [request_id]\nИспользуйте /listrequests для просмотра ожидающих запросов.",
                    cancellationToken: cancellationToken);
                return;
            }

            try
            {
                // Одобряем запрос на авторизацию
                await _authorizationService.ApproveAuthorizationRequestAsync(requestId, adminId);

                // Уведомляем администратора об успешном одобрении
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"✅ Запрос #{requestId} одобрен!",
                    cancellationToken: cancellationToken);

                // Уведомляем пользователя об одобрении
                await NotifyUserAboutApproval(requestId, cancellationToken);
            }
            catch (Exception ex)
            {
                // Логируем ошибку и уведомляем администратора
                _logger.LogError(ex, "Ошибка одобрения запроса на авторизацию #{RequestId} администратором {AdminId}", requestId, adminId);

                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"❌ Ошибка одобрения запроса: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Уведомляет пользователя о положительном результате рассмотрения его запроса на авторизацию
        /// Отправляет персональное сообщение пользователю, чей запрос был одобрен
        /// </summary>
        /// <param name="requestId">Идентификатор запроса на авторизацию</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения отправки уведомления</returns>
        /// <remarks>
        /// В случае ошибки отправки уведомления ошибка логируется, но не прерывает основной поток выполнения
        /// Это предотвращает сбои в основном процессе одобрения при проблемах с уведомлением
        /// </remarks>
        private async Task NotifyUserAboutApproval(long requestId, CancellationToken cancellationToken)
        {
            try
            {
                // Получаем информацию о запросе для получения ChatId пользователя
                var request = await _authorizationService.GetAuthorizationRequestById(requestId);
                if (request != null)
                {
                    // Отправляем уведомление пользователю об одобрении доступа
                    await _botClient.SendMessage(
                        chatId: request.ChatId,
                        text: "✅ Ваш запрос на доступ к боту одобрен! Теперь вы можете использовать все функции бота.",
                        cancellationToken: cancellationToken);

                    _logger.LogInformation("Пользователь {UserId} уведомлен об одобрении запроса #{RequestId}", request.UserId, requestId);
                }
                else
                {
                    _logger.LogWarning("Не удалось найти запрос #{RequestId} для уведомления пользователя", requestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка уведомления пользователя об одобрении запроса #{RequestId}", requestId);
            }
        }
    }
}