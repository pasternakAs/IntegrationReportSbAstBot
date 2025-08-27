using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class RequestAccessCommandHandler(IAuthorizationService authorizationService, ITelegramBotClient telegramBotClient) : ICommandHandler
    {
        private readonly ITelegramBotClient _botClient = telegramBotClient;
        private readonly IAuthorizationService _authorizationService = authorizationService;
        public string Command => "/requestaccess";

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From?.Id;
            var userName = message.From?.Username ?? message.From?.FirstName ?? $"User_{userId}";

            // Проверяем, не авторизован ли уже пользователь
            if (await _authorizationService.IsUserAuthorizedAsync(userId ?? 0))
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "✅ Вы уже авторизованы!",
                    cancellationToken: cancellationToken);
                return;
            }

            // Создаем запрос на авторизацию
            await _authorizationService.CreateAuthorizationRequestAsync(
                userId ?? 0,
                userName,
                chatId,
                $"Запрос доступа от пользователя {userName}");

            await _botClient.SendMessage(
                chatId: chatId,
                text: "📥 Ваш запрос на доступ отправлен администратору. Ожидайте подтверждения.",
                cancellationToken: cancellationToken);

            // Уведомляем администраторов
            var adminNotification = $@"📥 Новый запрос доступа к боту!
                            Пользователь: {userName}
                            ID: {userId}
                            Chat ID: {chatId}

                            Для одобрения используйте команду: /approve [request_id]
                            Для просмотра всех запросов: /listrequests
            ";

            foreach (var adminId in _botSettings.AdminUserIds)
            {
                try
                {
                    await _botClient.SendMessage(
                        chatId: adminId,
                        text: adminNotification,
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // Игнорируем ошибки отправки админам
                }
            }
        }
    }
}
