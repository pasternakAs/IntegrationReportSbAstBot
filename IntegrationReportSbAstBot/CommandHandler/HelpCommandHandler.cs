using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды помощи
    /// Предоставляет пользователям информацию о доступных командах бота в зависимости от контекста общения
    /// </summary>
    public class HelpCommandHandler(ITelegramBotClient telegramBotClient) : ICommandHandler
    {
        private readonly ITelegramBotClient _botClient = telegramBotClient;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/help" - команда для получения справочной информации</value>
        public string Command => "/help";

        /// <summary>
        /// Обрабатывает команду помощи и отправляет пользователю соответствующую справочную информацию
        /// Формирует персонализированный список команд в зависимости от типа чата
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения отправки справочной информации</returns>
        /// <remarks>
        /// Формат команды: /help
        /// Для приватных чатов отображается полный список команд
        /// Для групповых чатов отображается ограниченный список команд
        /// Это позволяет избежать спама в группах и предоставить релевантную информацию
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var chatType = message.Chat.Type; // Group, Supergroup, Private и т.д.

            // Формируем персонализированный текст помощи в зависимости от типа чата
            var textHelp = chatType == ChatType.Private
                ? "Доступные команды:\n" +
                    "/start - начать работу\n" +
                    "/subscribe - подписаться на рассылку\n" +
                    "/unsubscribe - отписаться от рассылки\n" +
                    "/procedure номер_процедуры - инфа по процедуре\n" +
                    "/geterrorintegration - получить все ошибки интеграции\n" +
                    "/help - помощь"
                : "Доступные команды:\n/subscribe - подписаться на рассылку\n" +
                    "/unsubscribe - отписаться от рассылки\n" +
                    "/procedure номер_процедуры - инфа по процедуре\n" +
                    "/geterrorintegration - получить все ошибки интеграции\n" +
                    "/help - помощь";

            // Отправляем справочную информацию пользователю
            await _botClient.SendMessage(
                chatId: chatId,
                text: textHelp,
                cancellationToken: cancellationToken);
        }
    }
}