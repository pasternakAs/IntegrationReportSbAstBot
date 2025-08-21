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
    public class TelegramBotService(ITelegramBotClient botClient, ISubscriberService subscriberService, ILogger<TelegramBotService> logger, IProcedureInfoService procedureInfoService, IOptions<BotSettings> botSettings)
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ISubscriberService _subscriberService = subscriberService;
        private readonly IProcedureInfoService _procedureInfoService = procedureInfoService;
        private readonly ILogger<TelegramBotService> _logger = logger;
        private readonly BotSettings _botSettings = botSettings.Value;

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

            // Обработка команд с параметрами
            if (messageText.StartsWith("/procedure", StringComparison.OrdinalIgnoreCase))
            {
                await HandleProcedureCommand(message, messageText, cancellationToken);
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

            switch (messageText.ToLower())
            {
                case "/start":
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Привет! Я бот для рассылок. Используйте /subscribe для подписки на уведомления. И команда /help для вопросов.",
                        cancellationToken: cancellationToken);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureNumber"></param>
        /// <returns></returns>
        private static bool IsValidProcedureNumber(string procedureNumber)
        {
            // Проверяем, что строка содержит только цифры
            return procedureNumber.All(char.IsDigit) && procedureNumber.Length >= 10;
        }
    }
}