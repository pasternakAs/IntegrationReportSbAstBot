using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик стартовой команды бота
    /// Предоставляет пользователям приветственное сообщение и информацию о текущем статусе авторизации
    /// </summary>
    public class StartCommandHandler(ITelegramBotClient botClient, ILogger<StartCommandHandler> logger, IAuthorizationService authorizationService) : ICommandHandler
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ILogger<StartCommandHandler> _logger = logger;
        private readonly IAuthorizationService _authorizationService = authorizationService;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/start" - стартовая команда бота</value>
        public string Command => "/start";

        /// <summary>
        /// Обрабатывает стартовую команду и отправляет пользователю приветственное сообщение
        /// Отображает текущий статус авторизации пользователя и предоставляет инструкции для дальнейших действий
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения отправки приветственного сообщения</returns>
        /// <remarks>
        /// Формат команды: /start
        /// Доступна для всех пользователей, включая неавторизованных
        /// Персонализирует сообщение в зависимости от статуса авторизации пользователя
        /// Логирует факт взаимодействия пользователя с ботом для аналитики
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From?.Id;
            var userName = message.From?.Username ?? message.From?.FirstName ?? "Unknown";

            // Проверяем статус авторизации пользователя для персонализации сообщения
            var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(userId ?? 0);
            var status = isAuthorized ? "✅ Вы авторизованы" : "❌ Вы не авторизованы";

            // Формируем персонализированное приветственное сообщение
            var welcomeMessage = $@"👋 Привет, {userName}!
                            🤖 Это бот для внутреннего использования СберА.
                            🔒 {status}"
                       + (isAuthorized ? "" : "📝 Для запроса доступа используйте команду: /requestaccess");

            // Отправляем приветственное сообщение пользователю
            await _botClient.SendMessage(
                chatId: chatId,
                text: welcomeMessage,
                cancellationToken: cancellationToken);

            // Логируем факт отправки приветственного сообщения для мониторинга активности
            _logger.LogInformation("Отправлено приветственное сообщение пользователю {User}", message.Chat.Id);
        }
    }
}