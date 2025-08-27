using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class UnsubscribeCommandHandler : IAuthorizedCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISubscriberService _subscriberService;
        private readonly ILogger<UnsubscribeCommandHandler> _logger;

        public string Command => "/unsubscribe";

        public UnsubscribeCommandHandler(ITelegramBotClient botClient, ISubscriberService subscriberService, ILogger<UnsubscribeCommandHandler> logger)
        {
            _botClient = botClient;
            _subscriberService = subscriberService;
            _logger = logger;
        }

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatType = message.Chat.Type; // Group, Supergroup, Private и т.д.
            await _subscriberService.UnsubscribeUserAsync(message.Chat.Id);
            await _botClient.SendMessage(
            chatId: message.Chat.Id,
                 text: chatType != ChatType.Private ? "❌ Группа отписана от рассылки" : "❌ Вы отписались от рассылки.",
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Пользователь {User} подписался на отчёты", message.Chat.Id);
        }
    }
}
