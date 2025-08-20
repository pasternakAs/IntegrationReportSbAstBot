using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис для управления Telegram ботом
    /// Обрабатывает входящие сообщения, команды пользователей и управляет подписками
    /// Обеспечивает запуск и работу бота в режиме polling
    /// </summary>
    public class TelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISubscriberService _subscriberService;

        /// <summary>
        /// Инициализирует новый экземпляр класса TelegramBotService
        /// </summary>
        /// <param name="botClient">Клиент Telegram бота для отправки и получения сообщений</param>
        /// <param name="subscriberService">Сервис управления подписчиками для управления списком подписчиков</param>
        public TelegramBotService(ITelegramBotClient botClient, ISubscriberService subscriberService)
        {
            _botClient = botClient;
            _subscriberService = subscriberService;
        }

        /// <summary>
        /// Запускает бота в режиме polling для приема входящих сообщений
        /// Настраивает обработчики сообщений и ошибок, выводит информацию о запуске бота
        /// </summary>
        /// <param name="cancellationToken">Токен отмены для корректной остановки бота</param>
        /// <returns>Асинхронная задача завершения операции запуска</returns>
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

        /// <summary>
        /// Обработчик входящих обновлений от Telegram API
        /// Фильтрует сообщения и передает их на обработку команд
        /// </summary>
        /// <param name="botClient">Клиент бота (не используется, так как есть поле класса)</param>
        /// <param name="update">Объект обновления от Telegram API</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача обработки обновления</returns>
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
        /// Обрабатывает текстовые команды от пользователей Telegram
        /// Поддерживает команды: /start, /subscribe, /unsubscribe, /help
        /// </summary>
        /// <param name="message">Объект сообщения от пользователя</param>
        /// <param name="messageText">Текст команды для обработки</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача обработки команды</returns>
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
        /// Обработчик ошибок polling режима работы бота
        /// Логирует ошибки и специфичную информацию об ошибках Telegram API
        /// </summary>
        /// <param name="botClient">Клиент бота (не используется в статическом контексте)</param>
        /// <param name="exception">Исключение, возникшее во время работы бота</param>
        /// <param name="cancellationToken">Токен отмены (не используется)</param>
        /// <returns>Асинхронная задача завершения обработки ошибки</returns>
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