using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды отписки от рассылки отчетов
    /// Позволяет авторизованным пользователям и группам отказаться от автоматической рассылки отчетов
    /// </summary>
    public class UnsubscribeCommandHandler : IAuthorizedCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISubscriberService _subscriberService;
        private readonly ILogger<UnsubscribeCommandHandler> _logger;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/unsubscribe" - команда для отписки от рассылки отчетов</value>
        public string Command => "/unsubscribe";

        /// <summary>
        /// Инициализирует новый экземпляр обработчика команды отписки от рассылки
        /// </summary>
        /// <param name="botClient">Клиент Telegram бота для отправки сообщений</param>
        /// <param name="subscriberService">Сервис управления подписчиками</param>
        /// <param name="logger">Логгер для записи информации о действиях пользователей</param>
        public UnsubscribeCommandHandler(ITelegramBotClient botClient, ISubscriberService subscriberService, ILogger<UnsubscribeCommandHandler> logger)
        {
            _botClient = botClient;
            _subscriberService = subscriberService;
            _logger = logger;
        }

        /// <summary>
        /// Обрабатывает команду отписки от рассылки отчетов
        /// Удаляет пользователя или группу из списка подписчиков и отправляет подтверждение
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки команды отписки</returns>
        /// <remarks>
        /// Формат команды: /unsubscribe
        /// Доступна только для авторизованных пользователей
        /// Поддерживает отписку как отдельных пользователей, так и групповых чатов
        /// Отправляет персонализированное подтверждение в зависимости от типа чата
        /// Логирует факт отписки для аналитики и мониторинга
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatType = message.Chat.Type; // Group, Supergroup, Private и т.д.

            // Удаляем пользователя/группу из списка подписчиков
            await _subscriberService.UnsubscribeUserAsync(message.Chat.Id);

            // Отправляем персонализированное подтверждение отписки
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: chatType != ChatType.Private ? "❌ Группа отписана от рассылки" : "❌ Вы отписались от рассылки.",
                cancellationToken: cancellationToken
            );

            // Логируем факт отписки для мониторинга активности пользователей
            _logger.LogInformation("Пользователь {User} отписался от отчётов", message.Chat.Id);
        }
    }
}