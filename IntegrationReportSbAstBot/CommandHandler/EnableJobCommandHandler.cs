using IntegrationReportSbAstBot.Interfaces;
using IntegrationReportSbAstBot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.CommandHandler
{
    /// <summary>
    /// Обработчик команды включения конкретного Job
    /// Использование: /enablejob ReportJob или /enablejob ArchiveDocumentsJob
    /// </summary>
    public class EnableJobCommandHandler : IAdminCommandHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IJobManagementService _jobManagementService;
        private readonly ILogger<EnableJobCommandHandler> _logger;

        public string Command => "/enablejob";

        public EnableJobCommandHandler(
            ITelegramBotClient botClient,
            IJobManagementService jobManagementService,
            ILogger<EnableJobCommandHandler> logger)
        {
            _botClient = botClient;
            _jobManagementService = jobManagementService;
            _logger = logger;
        }

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
                        text: $"❌ Использование: /enablejob [название_job]\nДоступные Jobs: {jobs}",
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

                await _jobManagementService.EnableJobAsync(jobName);

                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"✅ Job '{jobName}' включен",
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Администратор {UserId} включил Job {JobName}", message.From?.Id, jobName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при включении Job");
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Ошибка при включении Job",
                    cancellationToken: cancellationToken);
            }
        }
    }
}