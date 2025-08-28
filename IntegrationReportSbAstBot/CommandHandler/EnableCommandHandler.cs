using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды включения бота
    /// Позволяет администраторам восстанавливать нормальную функциональность бота после отключения
    /// </summary>
    public class EnableCommandHandler : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IBotStateService _botStateService;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/enabled" - команда для включения бота</value>
        public string Command => "/enabled";

        /// <summary>
        /// Инициализирует новый экземпляр обработчика команды включения бота
        /// </summary>
        /// <param name="botClient">Клиент Telegram бота для отправки сообщений</param>
        /// <param name="botStateService">Сервис управления состоянием бота</param>
        public EnableCommandHandler(ITelegramBotClient botClient, IBotStateService botStateService)
        {
            _botClient = botClient;
            _botStateService = botStateService;
        }

        /// <summary>
        /// Обрабатывает команду включения бота
        /// Устанавливает состояние бота в "включено" и уведомляет администратора
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки команды</returns>
        /// <remarks>
        /// Формат команды: /enabled
        /// Только пользователи из списка администраторов могут выполнять эту команду
        /// После выполнения команда все функции бота становятся доступными для авторизованных пользователей
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            // Устанавливаем состояние бота в "включено"
            await _botStateService.SetEnabledAsync(true);

            // Уведомляем администратора об успешном включении
            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Бот включён.",
                cancellationToken: cancellationToken);
        }
    }
}