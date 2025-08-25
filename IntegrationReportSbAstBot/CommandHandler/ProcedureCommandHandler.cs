using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class ProcedureCommandHandler(
        ITelegramBotClient botClient,
        IProcedureInfoService procedureInfoService,
        ILogger<ProcedureCommandHandler> logger)
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly IProcedureInfoService _procedureInfoService = procedureInfoService;
        private readonly ILogger<ProcedureCommandHandler> _logger = logger;

        public string Command => "/procedure";

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var chatId = message.Chat.Id;
                // Разбираем команду и параметры
                var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

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

                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⏳ Формирую отчёт по процедурам...",
                    cancellationToken: cancellationToken);

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

                _logger.LogInformation("Пользователь {User} получил отчёт по процедурам", message.Chat.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /procedure для пользователя {User}", message.Chat.Id);
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ Ошибка при формировании отчёта",
                    cancellationToken: cancellationToken);
            }
        }

        private static bool IsValidProcedureNumber(string procedureNumber)
        {
            // Проверяем, что строка содержит только цифры
            return procedureNumber.All(char.IsDigit) && procedureNumber.Length >= 10;
        }
    }
}
