using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды подписки на рассылку отчетов
    /// Позволяет авторизованным пользователям и группам подписаться на автоматическую рассылку отчетов
    /// </summary>
    public class SubscribeCommandHandler(ITelegramBotClient botClient, ISubscriberService subscriberService, ILogger<SubscribeCommandHandler> logger) : IAuthorizedCommandHandler
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ISubscriberService _subscriberService = subscriberService;
        private readonly ILogger<SubscribeCommandHandler> _logger = logger;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/subscribe" - команда для подписки на рассылку отчетов</value>
        public string Command => "/subscribe";

        /// <summary>
        /// Обрабатывает команду подписки на рассылку отчетов
        /// Добавляет пользователя или группу в список подписчиков и отправляет подтверждение
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки команды подписки</returns>
        /// <remarks>
        /// Формат команды: /subscribe
        /// Доступна только для авторизованных пользователей
        /// Поддерживает подписку как отдельных пользователей, так и групповых чатов
        /// Отправляет персонализированное подтверждение в зависимости от типа чата
        /// Логирует факт подписки для аналитики и мониторинга
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatType = message.Chat.Type; // Group, Supergroup, Private и т.д.
            var chatId = message.Chat.Id; // Group, Supergroup, Private и т.д.

            if (_subscriberService.GetSubscribersAsync().Result.Contains(chatId))
            {
                return;
            }

            // Добавляем пользователя/группу в список подписчиков
            await _subscriberService.SubscribeUserAsync(message.Chat.Id, chatType != ChatType.Private, message.Chat.FirstName ?? "");

            // Отправляем персонализированное подтверждение подписки
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: chatType != ChatType.Private ? "✅ Группа подписана на рассылки" : "✅ Вы успешно подписаны на отчёты",
                cancellationToken: cancellationToken
            );

            // Логируем факт подписки для мониторинга активности пользователей
            _logger.LogInformation("Пользователь {User} подписался на отчёты", message.Chat.Id);
        }
    }
}