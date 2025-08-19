using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.Services
{
    public class TelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISubscriberService _subscriberService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="subscriberService"></param>
        public TelegramBotService(ITelegramBotClient botClient, ISubscriberService subscriberService)
        {
            _botClient = botClient;
            _subscriberService = subscriberService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var receiverOptions = new Telegram.Bot.Polling.ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );

            var me = await _botClient.GetMe(cancellationToken);
            Console.WriteLine($"Бот @{me.Username} запущен в {DateTime.Now} и готов принимать сообщения...");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            // Обработка команд
            await HandleCommandAsync(message, messageText, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageText"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task HandleCommandAsync(Telegram.Bot.Types.Message message, string messageText, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;

            switch (messageText.ToLower())
            {
                case "/start":
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Привет! Я бот для рассылок. Используйте /subscribe для подписки на уведомления. И команда /help для вопросов.",
                        cancellationToken: cancellationToken);
                    break;

                case "/subscribe":
                    await _subscriberService.SubscribeUserAsync(chatId);
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "✅ Вы успешно подписались на рассылку!",
                        cancellationToken: cancellationToken);
                    break;

                case "/unsubscribe":
                    await _subscriberService.UnsubscribeUserAsync(chatId);
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Вы отписались от рассылки.",
                        cancellationToken: cancellationToken);
                    break;

                case "/help":
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Доступные команды:\n/start - начать работу\n/subscribe - подписаться на рассылку\n/unsubscribe - отписаться от рассылки\n/help - помощь",
                        cancellationToken: cancellationToken);
                    break;

                default:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Неизвестная команда. Введите /help для списка команд.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        /// <summary>
        ///  Обработчик ошибок
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка бота: {exception.Message}");

            // Если это ошибка API Telegram
            if (exception is ApiRequestException apiException)
            {
                Console.WriteLine($"Telegram API Error [{apiException.ErrorCode}]: {apiException.Message}");
            }

            return Task.CompletedTask;
        }
    }
}