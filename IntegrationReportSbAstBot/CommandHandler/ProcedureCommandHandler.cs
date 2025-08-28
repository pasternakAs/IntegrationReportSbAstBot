using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды получения информации по процедуре
    /// Позволяет авторизованным пользователям запрашивать детальную информацию о конкретных процедурах
    /// </summary>
    public class ProcedureCommandHandler(
        ITelegramBotClient botClient,
        IProcedureInfoService procedureInfoService,
        ILogger<ProcedureCommandHandler> logger) : IAuthorizedCommandHandler
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly IProcedureInfoService _procedureInfoService = procedureInfoService;
        private readonly ILogger<ProcedureCommandHandler> _logger = logger;

        /// <summary>
        /// Команда, которую обрабатывает данный обработчик
        /// </summary>
        /// <value>"/procedure" - команда для получения информации по номеру процедуры</value>
        public string Command => "/procedure";

        /// <summary>
        /// Обрабатывает команду получения информации по процедуре
        /// Выполняет валидацию параметров, запрашивает данные из сервиса и формирует ответ для пользователя
        /// </summary>
        /// <param name="message">Сообщение Telegram, содержащее команду и номер процедуры</param>
        /// <param name="cancellationToken">Токен отмены для асинхронных операций</param>
        /// <returns>Асинхронная задача завершения обработки команды</returns>
        /// <remarks>
        /// Формат команды: /procedure [номер_процедуры]
        /// Пример: /procedure 0560300000624000078
        /// Доступно только для авторизованных пользователей
        /// Включает валидацию формата номера процедуры и обработку ошибок
        /// </remarks>
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var chatId = message.Chat.Id;
                // Разбираем команду и параметры для извлечения номера процедуры
                var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Проверяем наличие обязательного параметра (номера процедуры)
                if (parts.Length < 2)
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Использование: /procedure [номер_процедуры]\nПример: /procedure номерпроцедуры",
                        cancellationToken: cancellationToken);
                    return;
                }

                var procedureNumber = parts[1].Trim();

                // Валидация параметра - проверяем, что номер процедуры не пустой
                if (string.IsNullOrWhiteSpace(procedureNumber))
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Номер процедуры не может быть пустым.",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Проверка формата номера процедуры для предотвращения некорректных запросов
                if (!IsValidProcedureNumber(procedureNumber))
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Неверный формат номера процедуры. Ожидается числовой идентификатор.",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Уведомляем пользователя о начале формирования отчета
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⏳ Формирую отчёт по процедурам...",
                    cancellationToken: cancellationToken);

                // Получаем информацию о процедуре через сервисный слой
                var procedureInfo = await _procedureInfoService.GetProcedureInfoAsync(procedureNumber);

                // Проверяем наличие данных по запрошенной процедуре
                if (procedureInfo == null || procedureInfo.Count == 0)
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: $"❌ Процедура с номером {procedureNumber} не найдена.",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Формируем и отправляем отформатированный ответ пользователю
                var response = _procedureInfoService.FormatProcedureDocuments(procedureNumber, procedureInfo);

                // Разбиваем длинное сообщение на части если необходимо
                var messageParts = _procedureInfoService.SplitMessage(response);

                foreach (var part in messageParts)
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: part,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                        cancellationToken: cancellationToken);
                }

                // Логируем успешное выполнение запроса для мониторинга использования
                _logger.LogInformation("Пользователь {User} получил отчёт по процедурам {ProcedureNumber}", message.Chat.Id, procedureNumber);
            }
            catch (Exception ex)
            {
                // Логируем ошибку для последующего анализа и уведомляем пользователя
                _logger.LogError(ex, "Ошибка при обработке команды /procedure для пользователя {User}", message.Chat.Id);
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ Ошибка при формировании отчёта",
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Проверяет корректность формата номера процедуры
        /// </summary>
        /// <param name="procedureNumber">Номер процедуры для валидации</param>
        /// <returns>True если формат корректен, иначе False</returns>
        /// <remarks>
        /// Валидация включает проверку:
        /// - Содержит только цифровые символы
        /// - Имеет минимальную длину 10 символов (типичная длина идентификаторов процедур)
        /// </remarks>
        private static bool IsValidProcedureNumber(string procedureNumber)
        {
            // Проверяем, что строка содержит только цифры и имеет достаточную длину
            return procedureNumber.All(char.IsDigit) && procedureNumber.Length >= 10;
        }
    }
}