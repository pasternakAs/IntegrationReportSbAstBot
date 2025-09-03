using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды просмотра статуса Jobs
    /// </summary>
    public class JobsStatusCommandHandler : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IJobManagementService _jobManagementService;
        private readonly ILogger<JobsStatusCommandHandler> _logger;

        public string Command => "/jobsstatus";

        public JobsStatusCommandHandler(
            ITelegramBotClient botClient,
            IJobManagementService jobManagementService,
            ILogger<JobsStatusCommandHandler> logger)
        {
            _botClient = botClient;
            _jobManagementService = jobManagementService;
            _logger = logger;
        }

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var jobsStatus = await _jobManagementService.GetJobsStatusAsync();
                var statusMessage = "📊 Статус Jobs:\n\n";

                foreach (var job in jobsStatus)
                {
                    var status = job.Value ? "✅ Включен" : "❌ Отключен";
                    statusMessage += $"<b>{job.Key}:</b> {status}\n";
                }

                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: statusMessage,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Администратор {UserId} запросил статус Jobs", message.From?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статуса Jobs");
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Ошибка при получении статуса Jobs",
                    cancellationToken: cancellationToken);
            }
        }
    }
}