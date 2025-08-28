using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды просмотра списка ожидающих запросов на авторизацию
    /// Позволяет администраторам просматривать и управлять запросами пользователей на доступ к боту
    /// </summary>
    public class ListRequestsCommandHandler : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/listrequests" - команда для просмотра списка ожидающих запросов</value>
        public string Command => "/listrequests";

        /// <summary>
        /// Инициализирует новый экземпляр обработчика команды просмотра запросов на авторизацию
        /// </summary>
        /// <param name="botClient">Клиент Telegram бота для отправки сообщений</param>
        /// <param name="authorizationService">Сервис управления авторизацией пользователей</param>
        public ListRequestsCommandHandler(ITelegramBotClient botClient, IAuthorizationService authorizationService)
        {
            _botClient = botClient;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Обрабатывает команду просмотра списка ожидающих запросов на авторизацию
        /// Получает список нерассмотренных запросов и форматирует их для отображения администратору
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки команды</returns>
        /// <remarks>
        /// Формат команды: /listrequests
        /// Только пользователи из списка администраторов могут выполнять эту команду
        /// Отображает список всех нерассмотренных запросов с возможностью быстрого одобрения
        /// Для каждого запроса показывается команда /approve с соответствующим ID
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var adminChatId = message.Chat.Id;

            try
            {
                // Получаем список всех ожидающих запросов на авторизацию
                var requests = await _authorizationService.GetPendingRequestsAsync();

                // Если нет ожидающих запросов, уведомляем администратора
                if (requests.Count == 0)
                {
                    await _botClient.SendMessage(
                        chatId: adminChatId,
                        text: "📭 Нет ожидающих запросов на авторизацию.",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Формируем подробный отчет по ожидающим запросам
                var response = "📥 Ожидающие запросы на авторизацию:\n\n";
                foreach (var request in requests)
                {
                    response += $"<b>Запрос #{request.Id}</b>\n";
                    response += $"Пользователь: {request.UserName} ({request.UserId})\n";
                    response += $"Дата: {request.RequestedAt:dd.MM.yyyy HH:mm}\n";
                    response += $"Команда: /approve {request.Id}\n\n";
                }

                // Отправляем отформатированный список запросов администратору
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: response,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // Логируем ошибку и уведомляем администратора о проблеме
                // Это предотвращает сбой всего приложения при проблемах с базой данных
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"❌ Ошибка получения списка запросов: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }
    }
}