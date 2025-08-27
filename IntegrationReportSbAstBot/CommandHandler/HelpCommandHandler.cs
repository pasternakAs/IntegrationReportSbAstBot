using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class HelpCommandHandler(ITelegramBotClient telegramBotClient) : ICommandHandler
    {
        private readonly ITelegramBotClient _botClient = telegramBotClient;
        public string Command => "/help";

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var chatType = message.Chat.Type; // Group, Supergroup, Private и т.д.

            var textHelp = chatType == ChatType.Private
            ? "Доступные команды:\n/start - начать работу\n/subscribe - подписаться на рассылку\n/unsubscribe - отписаться от рассылки" +
                "\n/procedure номер_процедуры - инфа по процедуре\n/help - помощь"
            : "Доступные команды:\n/subscribe - подписаться на рассылку\n/unsubscribe - отписаться от рассылки" +
                "\n/procedure номер_процедуры - инфа по процедуре\n/help - помощь";

            await _botClient.SendMessage(
              chatId: chatId,
              text: textHelp,
              cancellationToken: cancellationToken);
        }
    }
}