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
        /// <param name="cancellationToken">Токен отмены для корректной остановки бота</param>
        /// <returns>Асинхронная задача завершения операции запуска</returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<Telegram.Bot.Types.Enums.UpdateType>() // все типы апдейтов
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
        /// <param name="botClient">Клиент бота для взаимодействия с Telegram API</param>
        /// <param name="update">Объект обновления от Telegram API</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача обработки обновления</returns>
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            // Проверяем, что обновление содержит сообщение и текст
            if (update.Message is not { } message || message.Text is null)
                return;

            // Обрабатываем только команды (сообщения, начинающиеся с /)
            if (!message.Text.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Отсекаем старые сообщения (старше 5 минут) для предотвращения обработки устаревших команд
            var messageAge = DateTime.UtcNow - message.Date;
            if (messageAge.TotalMinutes > 5)
            {
                _logger.LogDebug("Игнорируем старое сообщение от пользователя {User} (возраст: {Age} минут)",
                    message.Chat.Id, messageAge.TotalMinutes);
                return; // Просто игнорируем
            }

            var chatId = message.Chat.Id;
            var userId = message.From?.Id;
            var userName = message.From?.Username ?? message.From?.FirstName ?? "Unknown";
            var command = message.Text.Split(' ')[0]; // первая часть текста как команда

            // Проверяем, включен ли бот
            if (!_botStateService.IsEnabledAsync().Result)
            {
                // Если бот выключен, разрешаем только администраторам использовать команды
                if (!_options.AdminUserIds.Contains(userId ?? 0))
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: _options.MaintenanceMessage,
                        cancellationToken: cancellationToken);
                    return;
                }
            }

            // Ищем обработчик для команды
            var handler = _commandHandler.FirstOrDefault(h => h.Command.Equals(command, StringComparison.OrdinalIgnoreCase));
            if (handler == null)
            {
                _logger.LogWarning("Неизвестная команда {Command} от пользователя {User}", command, message.Chat.Id);
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❓ Неизвестная команда. Попробуйте /start",
                    cancellationToken: cancellationToken);
                return;
            }

            // Проверяем авторизацию для команд, требующих авторизации
            if (handler is IAuthorizedCommandHandler)
            {
                var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(userId ?? 0);
                if (!isAuthorized)
                {
                    // Если пользователь не авторизован, отправляем сообщение с предложением авторизоваться
                    var welcomeMessage = $@"👋 Привет, {userName}!
                                            🤖 Это бот для внутреннего использования СберА.
                                            🔒 ❌ Вы не авторизованы
                                            📝 Для запроса доступа используйте команду: /requestaccess";

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: welcomeMessage,
                        cancellationToken: cancellationToken);
                    return;
                }
            }

            // Проверяем права администратора для административных команд
            if (handler is IAdminCommandHandler)
            {
                if (!_options.AdminUserIds.Contains(userId ?? 0))
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Только администраторы могут выполнять эту команду.",
                        cancellationToken: cancellationToken);
                    return;
                }
            }

            _logger.LogInformation("Команда {Command} от пользователя {User}", command, message.Chat.Id);

            // Обрабатываем команду
            try
            {
                await handler.HandleAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды {Command} пользователем {User}", command, message.Chat.Id);
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Произошла ошибка при обработке команды.",
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Обработчик ошибок polling режима работы бота
        /// Логирует ошибки и обеспечивает стабильную работу бота
        /// </summary>
        /// <param name="bot">Клиент бота, в котором произошла ошибка</param>
        /// <param name="exception">Исключение, возникшее во время работы бота</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки ошибки</returns>
        private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Ошибка в TelegramBotService");
            return Task.CompletedTask;
        }
    }
}