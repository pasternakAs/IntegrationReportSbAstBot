using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class StartCommandHandler(ITelegramBotClient botClient, ILogger<StartCommandHandler> logger, IAuthorizationService authorizationService) : ICommandHandler
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ILogger<StartCommandHandler> _logger = logger;
        private readonly IAuthorizationService _authorizationService = authorizationService;

        public string Command => "/start";

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From?.Id;
            var userName = message.From?.Username ?? message.From?.FirstName ?? "Unknown";

            var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(userId ?? 0);
            var status = isAuthorized ? "✅ Вы авторизованы" : "❌ Вы не авторизованы";

            var welcomeMessage = $@"👋 Привет, {userName}!
                        🤖 Это бот для внутреннего использования СберА.
                        🔒 {status}"
                       + (isAuthorized ? "" : "📝 Для запроса доступа используйте команду: /requestaccess");

            await _botClient.SendMessage(
                chatId: chatId,
                text: welcomeMessage,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Отправлено приветственное сообщение пользователю {User}", message.Chat.Id);
        }
    }
}