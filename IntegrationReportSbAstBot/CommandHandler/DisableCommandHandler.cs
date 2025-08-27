using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class DisableCommandHandler : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IBotStateService _botStateService;

        public string Command => "/disable";

        public DisableCommandHandler(ITelegramBotClient botClient, IBotStateService botStateService)
        {
            _botClient = botClient;
            _botStateService = botStateService;
        }

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            await _botStateService.SetEnabledAsync(false);

            await _botClient.SendMessage(
                message.Chat.Id,
                "⛔ Бот отключён. Доступны только команды админа.",
                cancellationToken: cancellationToken);
        }
    }
}
