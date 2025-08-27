using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис для управления Telegram ботом
    /// Обрабатывает входящие сообщения, команды пользователей и управляет подписками
    /// Обеспечивает запуск и работу бота в режиме polling
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр класса TelegramBotService
    /// </remarks>
    /// <param name="botClient">Клиент Telegram бота для отправки и получения сообщений</param>
    /// <param name="subscriberService">Сервис управления подписчиками для управления списком подписчиков</param>
    public class TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger, IEnumerable<ICommandHandler> commandHandler, IAuthorizationService authorizationService, IBotStateService botStateService, IOptions<BotSettings> options)
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ILogger<TelegramBotService> _logger = logger;
        private readonly IEnumerable<ICommandHandler> _commandHandler = commandHandler;
        private readonly IAuthorizationService _authorizationService = authorizationService;
        private readonly IBotStateService _botStateService = botStateService;
        private readonly BotSettings _options = options.Value;

        /// <summary>
        /// Запускает бота в режиме polling для приема входящих сообщений
        /// Настраивает обработчики сообщений и ошибок, выводит информацию о запуске бота
        /// </summary>
        /// <returns>Асинхронная задача завершения операции запуска</returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // все типы апдейтов
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            var me = await _botClient.GetMe(cancellationToken);
            _logger.LogInformation("Telegram Bot {Name} запущен", me.Username);
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
            if (update.Message is not { } message || message.Text is null)
                return;

            // Обработка команд с параметрами
            if (!message.Text.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Отсекаем сообщения старше 5 минут
            var messageAge = DateTime.UtcNow - message.Date;
            if (messageAge.TotalMinutes > 5)
            {
                return; // Просто игнорируем
            }

            var chatId = message.Chat.Id;
            var userId = message.From?.Id;
            var userName = message.From?.Username ?? message.From?.FirstName ?? "Unknown";
            var command = message.Text.Split(' ')[0]; // первая часть текста как команда
            var welcomeMessage = "";

            var handler = _commandHandler.FirstOrDefault(h => h.Command.Equals(command, StringComparison.OrdinalIgnoreCase));
            if (handler == null)
            {
                _logger.LogWarning("Неизвестная команда {Command} от пользователя {User}", command, message.Chat.Id);
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❓ Неизвестная команда. Попробуйте /start",
                    cancellationToken: cancellationToken
                );

                return;
            }

            // Проверяем команды управления для админов
            if (_options.AdminUserIds.Contains(userId ?? 0))
            {
                _botStateService.IsEnabledAsync();
                return;
            }

            var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(message.From.Id);
            var status = isAuthorized ? "✅ Вы авторизованы" : "❌ Вы не авторизованы";

            if (handler is IAuthorizedCommandHandler)
            {
                welcomeMessage = $@"👋 Привет, {userName}!
                        🤖 Это бот для внутреннего использования СберА.
                        🔒 {status}
                        📝 Для запроса доступа используйте команду: /requestaccess";
            }
            else
            {
                welcomeMessage = $@"👋 Привет, {userName}!
                        🤖 Это бот для внутреннего использования СберА.
                        🔒 {status}";
            }

            await _botClient.SendMessage(
              chatId: chatId,
              text: welcomeMessage,
              cancellationToken: cancellationToken);

            _logger.LogInformation("Команда {Command} от пользователя {User}", command, message.Chat.Id);
            await handler.HandleAsync(message, cancellationToken);
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Ошибка в TelegramBotService");
            return Task.CompletedTask;
        }
    }
}