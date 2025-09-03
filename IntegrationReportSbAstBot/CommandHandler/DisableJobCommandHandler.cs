using IntegrationReportSbAstBot.Interfaces;
using IntegrationReportSbAstBot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды отключения конкретного Job
    /// Использование: /disablejob ReportJob или /disablejob ArchiveDocumentsJob
    /// </summary>
    public class DisableJobCommandHandler(
        ITelegramBotClient botClient,
        IJobManagementService jobManagementService,
        ILogger<DisableJobCommandHandler> logger) : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly IJobManagementService _jobManagementService = jobManagementService;
        private readonly ILogger<DisableJobCommandHandler> _logger = logger;

        public string Command => "/disablejob";

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var parts = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts?.Length < 2)
                {
                    var jobs = string.Join(", ", JobManagementService.GetAvailableJobs());
                    await _botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"❌ Использование: /disablejob [название_job]\nДоступные Jobs: {jobs}",
                        cancellationToken: cancellationToken);
                    return;
                }

                var jobName = parts[1];

                // Проверяем, существует ли такой Job
                var availableJobs = JobManagementService.GetAvailableJobs();
                if (!availableJobs.Contains(jobName))
                {
                    await _botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"❌ Job '{jobName}' не найден.\nДоступные Jobs: {string.Join(", ", availableJobs)}",
                        cancellationToken: cancellationToken);
                    return;
                }

                await _jobManagementService.DisableJobAsync(jobName);

                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"❌ Job '{jobName}' отключен",
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Администратор {UserId} отключил Job {JobName}", message.From?.Id, jobName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отключении Job");
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Ошибка при отключении Job",
                    cancellationToken: cancellationToken);
            }
        }
    }
}