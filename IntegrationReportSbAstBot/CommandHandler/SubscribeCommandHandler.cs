using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class SubscribeCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISubscriberService _subscriberService;
        private readonly ILogger<SubscribeCommandHandler> _logger;

        public string Command => "/subscribe";

        public SubscribeCommandHandler(ITelegramBotClient botClient, ISubscriberService subscriberService, ILogger<SubscribeCommandHandler> logger)
        {
            _botClient = botClient;
            _subscriberService = subscriberService;
            _logger = logger;
        }

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatType = message.Chat.Type; // Group, Supergroup, Private и т.д.
            await _subscriberService.SubscribeUserAsync(message.Chat.Id);
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: chatType != ChatType.Private ? "✅ Группа подписана на рассылки" : "✅ Вы успешно подписаны на отчёты",
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Пользователь {User} подписался на отчёты", message.Chat.Id);
        }
    }
}
