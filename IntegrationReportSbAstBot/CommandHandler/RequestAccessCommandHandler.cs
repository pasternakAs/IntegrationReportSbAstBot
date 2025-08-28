using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды запроса доступа к боту
    /// Позволяет неавторизованным пользователям подать заявку на получение доступа к функциональности бота
    /// </summary>
    public class RequestAccessCommandHandler(IAuthorizationService authorizationService, ITelegramBotClient telegramBotClient, IOptions<BotSettings> botSettings) : ICommandHandler
    {
        private readonly ITelegramBotClient _botClient = telegramBotClient;
        private readonly IAuthorizationService _authorizationService = authorizationService;
        private readonly BotSettings _botSettings = botSettings.Value;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/requestaccess" - команда для подачи запроса на авторизацию</value>
        public string Command => "/requestaccess";

        /// <summary>
        /// Обрабатывает команду запроса доступа и инициирует процесс авторизации пользователя
        /// Создает запрос на авторизацию и уведомляет администраторов для рассмотрения
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки команды</returns>
        /// <remarks>
        /// Формат команды: /requestaccess
        /// Доступна для всех пользователей, включая неавторизованных
        /// Предотвращает повторную подачу запроса от уже авторизованных пользователей
        /// Автоматически уведомляет всех администраторов о новом запросе
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var userId = message.From?.Id;
            var userName = message.From?.Username ?? message.From?.FirstName ?? $"User_{userId}";

            // Проверяем, не авторизован ли уже пользователь для предотвращения дубликатов
            if (await _authorizationService.IsUserAuthorizedAsync(userId ?? 0))
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "✅ Вы уже авторизованы!",
                    cancellationToken: cancellationToken);
                return;
            }

            // Создаем запрос на авторизацию в системе с метаинформацией о пользователе
            await _authorizationService.CreateAuthorizationRequestAsync(
                userId ?? 0,
                userName,
                chatId,
                $"Запрос доступа от пользователя {userName}");

            // Уведомляем пользователя об успешной подаче запроса
            await _botClient.SendMessage(
                chatId: chatId,
                text: "📥 Ваш запрос на доступ отправлен администратору. Ожидайте подтверждения.",
                cancellationToken: cancellationToken);

            // Формируем уведомление для администраторов о новом запросе
            var adminNotification = $@"📥 Новый запрос доступа к боту!
                                    Пользователь: {userName}
                                    ID: {userId}
                                    Chat ID: {chatId}

                                    Для одобрения используйте команду: /approve [request_id]
                                    Для просмотра всех запросов: /listrequests";

            // Уведомляем всех администраторов из списка настроек
            foreach (var adminId in _botSettings.AdminUserIds)
            {
                try
                {
                    // Отправляем уведомление каждому администратору индивидуально
                    await _botClient.SendMessage(
                        chatId: adminId,
                        text: adminNotification,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    // Игнорируем ошибки отправки админам для предотвращения сбоев основного процесса
                    // Это может происходить если админ заблокировал бота или изменил ID
                }
            }
        }
    }
}