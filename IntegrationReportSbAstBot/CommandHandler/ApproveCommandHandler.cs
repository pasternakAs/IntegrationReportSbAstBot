using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class ApproveCommandHandler(ITelegramBotClient botClient, ILogger<ApproveCommandHandler> logger, IAuthorizationService authorizationService) : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ILogger<ApproveCommandHandler> _logger = logger;
        private readonly IAuthorizationService _authorizationService = authorizationService;

        public string Command => "/approve";

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
                await _authorizationService.ApproveAuthorizationRequestAsync(requestId, adminId);

                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"✅ Запрос #{requestId} одобрен!",
                    cancellationToken: cancellationToken);

                // Уведомляем пользователя об одобрении
                await NotifyUserAboutApproval(requestId, cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"❌ Ошибка одобрения запроса: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task NotifyUserAboutApproval(long requestId, CancellationToken cancellationToken)
        {
            try
            {
                // Получаем информацию о запросе
                var request = await _authorizationService.GetAuthorizationRequestById(requestId);
                if (request != null)
                {
                    await _botClient.SendMessage(
                        chatId: request.ChatId,
                        text: "✅ Ваш запрос на доступ к боту одобрен! Теперь вы можете использовать все функции бота.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка уведомления пользователя об одобрении запроса #{RequestId}", requestId);
            }
        }
    }
}