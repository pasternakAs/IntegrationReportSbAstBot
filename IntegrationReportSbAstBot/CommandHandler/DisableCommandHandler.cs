using IntegrationReportSbAstBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды отключения бота
    /// Позволяет администраторам временно отключать функциональность бота для технических работ или обслуживания
    /// </summary>
    public class DisableCommandHandler : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IBotStateService _botStateService;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/disable" - команда для отключения бота</value>
        public string Command => "/disable";

        /// <summary>
        /// Инициализирует новый экземпляр обработчика команды отключения бота
        /// </summary>
        /// <param name="botClient">Клиент Telegram бота для отправки сообщений</param>
        /// <param name="botStateService">Сервис управления состоянием бота</param>
        public DisableCommandHandler(ITelegramBotClient botClient, IBotStateService botStateService)
        {
            _botClient = botClient;
            _botStateService = botStateService;
        }

        /// <summary>
        /// Обрабатывает команду отключения бота
        /// Устанавливает состояние бота в "отключено" и уведомляет администратора
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки команды</returns>
        /// <remarks>
        /// Формат команды: /disable
        /// Только пользователи из списка администраторов могут выполнять эту команду
        /// После выполнения команда бот становится недоступным для обычных пользователей
        /// Администраторы сохраняют возможность использовать административные команды
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            // Устанавливаем состояние бота в "отключено"
            await _botStateService.SetEnabledAsync(false);

            // Уведомляем администратора об успешном отключении
            await _botClient.SendMessage(
                message.Chat.Id,
                "⛔ Бот отключён. Доступны только команды админа.",
                cancellationToken: cancellationToken);
        }
    }
}