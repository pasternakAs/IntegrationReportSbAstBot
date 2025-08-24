using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// <remarks>
    /// Инициализирует новый экземпляр класса TelegramBotService
    /// </remarks>
    /// <param name="botClient">Клиент Telegram бота для отправки и получения сообщений</param>
    /// <param name="subscriberService">Сервис управления подписчиками для управления списком подписчиков</param>
    public class TelegramBotService(ITelegramBotClient botClient, ISubscriberService subscriberService, ILogger<TelegramBotService> logger, IProcedureInfoService procedureInfoService, IOptions<BotSettings> botSettings, IAuthorizationService authorizationService)
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ISubscriberService _subscriberService = subscriberService;
        private readonly IProcedureInfoService _procedureInfoService = procedureInfoService;
        private readonly ILogger<TelegramBotService> _logger = logger;
        private readonly BotSettings _botSettings = botSettings.Value;
        private readonly IAuthorizationService _authorizationService = authorizationService;

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
            var userId = message.From?.Id;
            var chatType = message.Chat.Type; // Group, Supergroup, Private и т.д.
            var userName = message.From?.Username ?? message.From?.FirstName ?? $"User_{userId}";

            // Обработка команд с параметрами
            if (!messageText.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Отсекаем сообщения старше 5 минут
            var messageAge = DateTime.UtcNow - message.Date;
            if (messageAge.TotalMinutes > 5)
            {
                return; // Просто игнорируем
            }

            // Проверяем авторизацию для всех команд, кроме /start и /requestaccess
            if (!messageText.StartsWith("/start") &&
                !messageText.StartsWith("/requestaccess") &&
                !await _authorizationService.IsUserAuthorizedAsync(userId ?? 0))
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: _botSettings.UnauthorizedMessage,
                    cancellationToken: cancellationToken);
                return;
            }

            // Если бот выключен, отправляем сообщение (кроме команд управления)
            if (!_botSettings.IsEnabled && !messageText.Equals("/enable", StringComparison.OrdinalIgnoreCase))
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Бот временно выключен администратором. Попробуйте позже.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Проверяем команды управления для админов
            if (_botSettings.AdminUserIds.Contains(userId ?? 0))
            {
                if (messageText.Equals("/enable", StringComparison.OrdinalIgnoreCase))
                {
                    _botSettings.IsEnabled = true;
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "✅ Бот включен",
                        cancellationToken: cancellationToken);
                    return;
                }

                if (messageText.Equals("/disable", StringComparison.OrdinalIgnoreCase))
                {
                    _botSettings.IsEnabled = false;
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Бот выключен",
                        cancellationToken: cancellationToken);
                    return;
                }
            }

            // Обработка команд с параметрами
            if (messageText.StartsWith("/procedure", StringComparison.OrdinalIgnoreCase))
            {
                await HandleProcedureCommand(message, messageText, cancellationToken);
                return;
            }

            switch (messageText.ToLower())
            {
                case "/start":
                    await HandleStartCommand(message, cancellationToken);
                    break;

                case "/subscribe" when chatType == Telegram.Bot.Types.Enums.ChatType.Private:
                    await _subscriberService.SubscribeUserAsync(chatId);
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "✅ Вы успешно подписались на рассылку!",
                        cancellationToken: cancellationToken);
                    break;

                case "/getInfProcedure":
                    await _subscriberService.SubscribeUserAsync(chatId);
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "",
                        cancellationToken: cancellationToken);
                    break;

                case "/groupid":
                    // Команда для получения ID группы (полезно для администратора)
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: $"🆔 Chat ID этой группы: <code>{chatId}</code>",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                        cancellationToken: cancellationToken);
                    break;

                case "/subscribe" when chatType != Telegram.Bot.Types.Enums.ChatType.Private:
                    // Для групп - добавляем группу в подписчики
                    await _subscriberService.SubscribeUserAsync(chatId);
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "✅ Группа подписана на рассылку отчетов!",
                        cancellationToken: cancellationToken);
                    break;

                case "/unsubscribe":
                    await _subscriberService.UnsubscribeUserAsync(chatId);
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Вы отписались от рассылки.",
                        cancellationToken: cancellationToken);
                    break;

                case "/requestaccess":
                    await HandleRequestAccess(message, cancellationToken);
                    break;

                case "/approve" when _botSettings.AdminUserIds.Contains(userId ?? 0):
                    await HandleApproveAccess(message, cancellationToken);
                    break;

                case "/listrequests" when _botSettings.AdminUserIds.Contains(userId ?? 0):
                    await HandleListRequests(message, cancellationToken);
                    break;

                case "/help":
                    var textHelp = chatType == Telegram.Bot.Types.Enums.ChatType.Private
                        ? "Доступные команды:\n/start - начать работу\n/subscribe - подписаться на рассылку\n/unsubscribe - отписаться от рассылки\n/procedure номер_процедуры - инфа по процедуре\n/help - помощь"
                        : "Доступные команды:\n/subscribe - подписаться на рассылку\n/unsubscribe - отписаться от рассылки\n/procedure номер_процедуры - инфа по процедуре\n/help - помощь";

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: textHelp,
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

        private async Task HandleStartCommand(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From?.Id;
            var userName = message.From?.Username ?? message.From?.FirstName ?? "Unknown";

            var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(userId ?? 0);
            var status = isAuthorized ? "✅ Вы авторизованы" : "❌ Вы не авторизованы";

            var welcomeMessage = $@"👋 Привет, {userName}!
                        🤖 Это бот для внутреннего использования СберА.
                        🔒 {status}
                        📝 Для запроса доступа используйте команду: /requestaccess
                                ";

            await _botClient.SendMessage(
                chatId: chatId,
                text: welcomeMessage,
                cancellationToken: cancellationToken);
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

        private async Task HandleProcedureCommand(Telegram.Bot.Types.Message message, string messageText, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;

            // Разбираем команду и параметры
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Использование: /procedure [номер_процедуры]\nПример: /procedure номерпроцедуры",
                    cancellationToken: cancellationToken);
                return;
            }

            var procedureNumber = parts[1].Trim();

            // Валидация параметра (проверяем, что это не пустая строка)
            if (string.IsNullOrWhiteSpace(procedureNumber))
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Номер процедуры не может быть пустым.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Проверка формата (опционально)
            if (!IsValidProcedureNumber(procedureNumber))
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Неверный формат номера процедуры. Ожидается числовой идентификатор.",
                    cancellationToken: cancellationToken);
                return;
            }

            try
            {
                // Получаем информацию о процедуре (ваш метод)
                var procedureInfo = await _procedureInfoService.GetProcedureInfoAsync(procedureNumber);

                if (procedureInfo == null)
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: $"❌ Процедура с номером {procedureNumber} не найдена.",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Формируем и отправляем ответ
                var response = _procedureInfoService.FormatProcedureDocuments(procedureNumber, procedureInfo);
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: response,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации по процедуре {ProcedureId}", procedureNumber);
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Произошла ошибка при получении информации о процедуре.",
                    cancellationToken: cancellationToken);
            }
        }

        private static bool IsValidProcedureNumber(string procedureNumber)
        {
            // Проверяем, что строка содержит только цифры
            return procedureNumber.All(char.IsDigit) && procedureNumber.Length >= 10;
        }

        private async Task HandleRequestAccess(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From?.Id;
            var userName = message.From?.Username ?? message.From?.FirstName ?? $"User_{userId}";

            // Проверяем, не авторизован ли уже пользователь
            if (await _authorizationService.IsUserAuthorizedAsync(userId ?? 0))
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "✅ Вы уже авторизованы!",
                    cancellationToken: cancellationToken);
                return;
            }

            // Создаем запрос на авторизацию
            await _authorizationService.CreateAuthorizationRequestAsync(
                userId ?? 0,
                userName,
                chatId,
                $"Запрос доступа от пользователя {userName}");

            await _botClient.SendMessage(
                chatId: chatId,
                text: "📥 Ваш запрос на доступ отправлен администратору. Ожидайте подтверждения.",
                cancellationToken: cancellationToken);

            // Уведомляем администраторов
            var adminNotification = $@"📥 Новый запрос доступа к боту!
                            Пользователь: {userName}
                            ID: {userId}
                            Chat ID: {chatId}

                            Для одобрения используйте команду: /approve [request_id]
                            Для просмотра всех запросов: /listrequests";

            foreach (var adminId in _botSettings.AdminUserIds)
            {
                try
                {
                    await _botClient.SendMessage(
                        chatId: adminId,
                        text: adminNotification,
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // Игнорируем ошибки отправки админам
                }
            }
        }

        private async Task HandleApproveAccess(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var adminChatId = message.Chat.Id;
            var adminId = message.From?.Id ?? 0;

            // Извлекаем request_id из команды
            var parts = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts?.Length < 2 || !long.TryParse(parts[1], out var requestId))
            {
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: "❌ Использование: /approve [request_id]\nИспользуйте /listrequests для просмотра ожидающих запросов.",
                    cancellationToken: cancellationToken);
                return;
            }

            try
            {
                await _authorizationService.ApproveAuthorizationRequestAsync(requestId, adminId);

                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"✅ Запрос #{requestId} одобрен!",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"❌ Ошибка одобрения запроса: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleListRequests(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var adminChatId = message.Chat.Id;

            try
            {
                var requests = await _authorizationService.GetPendingRequestsAsync();

                if (requests.Count == 0)
                {
                    await _botClient.SendMessage(
                        chatId: adminChatId,
                        text: "📭 Нет ожидающих запросов на авторизацию.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var response = "📥 Ожидающие запросы на авторизацию:\n\n";
                foreach (var request in requests)
                {
                    response += $"<b>Запрос #{request.Id}</b>\n";
                    response += $"Пользователь: {request.UserName} ({request.UserId})\n";
                    response += $"Дата: {request.RequestedAt:dd.MM.yyyy HH:mm}\n";
                    response += $"Команда: /approve {request.Id}\n\n";
                }

                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: response,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"❌ Ошибка получения списка запросов: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }
    }
}