using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationReportSbAstBot.Interfaces;
using IntegrationReportSbAstBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IntegrationReportSbAstBot.CommandHandler
{
    public class ListRequestsCommandHandler : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IAuthorizationService _authorizationService;

        public string Command => "/listrequests";

        public ListRequestsCommandHandler(ITelegramBotClient botClient, IAuthorizationService authorizationService)
        {
            _botClient = botClient;
            _authorizationService = authorizationService;
        }

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var adminChatId = message.Chat.Id;

            try
            {
                var requests = await _authorizationService.GetPendingRequestsAsync();

                if (requests.Count == 0)
                {
                    await _botClient.SendMessage(
                        chatId: adminChatId,
                        text: "📭 Нет ожидающих запросов на авторизацию.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var response = "📥 Ожидающие запросы на авторизацию:\n\n";
                foreach (var request in requests)
                {
                    response += $"<b>Запрос #{request.Id}</b>\n";
                    response += $"Пользователь: {request.UserName} ({request.UserId})\n";
                    response += $"Дата: {request.RequestedAt:dd.MM.yyyy HH:mm}\n";
                    response += $"Команда: /approve {request.Id}\n\n";
                }

                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: response,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendMessage(
                    chatId: adminChatId,
                    text: $"❌ Ошибка получения списка запросов: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }
    }
}