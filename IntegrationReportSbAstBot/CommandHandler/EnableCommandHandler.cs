using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class EnableCommandHandler : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IBotStateService _botStateService;
        public string Command => "/enabled";

        public EnableCommandHandler(ITelegramBotClient botClient, IBotStateService botStateService)
        {
            _botClient = botClient;
            _botStateService = botStateService;
        }

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            await _botStateService.SetEnabledAsync(true);

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Бот включён.",
                cancellationToken: cancellationToken);
        }
    }
}
