using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Реализация сервиса управления Quartz Jobs
    /// </summary>
    public class JobManagementService : IJobManagementService
    {
        private readonly ILogger<JobManagementService> _logger;
        private static readonly Dictionary<string, bool> _jobStatus = new()
        {
            { "ReportJob", true },
            { "ArchiveDocumentsJob", true },
            {"KtruMonitoringJob", true }
        };

        public JobManagementService(ILogger<JobManagementService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Включает выполнение всех Jobs
        /// </summary>
        public async Task EnableAllJobsAsync()
        {
            foreach (var jobName in _jobStatus.Keys.ToList())
            {
                _jobStatus[jobName] = true;
            }

            _logger.LogInformation("Все Jobs включены");
        }

        /// <summary>
        /// Отключает выполнение всех Jobs
        /// </summary>
        public async Task DisableAllJobsAsync()
        {
            foreach (var jobName in _jobStatus.Keys.ToList())
            {
                _jobStatus[jobName] = false;
            }

            _logger.LogInformation("Все Jobs отключены");
        }

        /// <summary>
        /// Включает конкретный Job
        /// </summary>
        public async Task EnableJobAsync(string jobName)
        {
            if (_jobStatus.ContainsKey(jobName))
            {
                _jobStatus[jobName] = true;
                _logger.LogInformation("Job {JobName} включен", jobName);
            }
            else
            {
                _logger.LogWarning("Попытка включить несуществующий Job: {JobName}", jobName);
            }
        }

        /// <summary>
        /// Отключает конкретный Job
        /// </summary>
        public async Task DisableJobAsync(string jobName)
        {
            if (_jobStatus.ContainsKey(jobName))
            {
                _jobStatus[jobName] = false;
                _logger.LogInformation("Job {JobName} отключен", jobName);
            }
            else
            {
                _logger.LogWarning("Попытка отключить несуществующий Job: {JobName}", jobName);
            }
        }

        /// <summary>
        /// Получает статус всех Jobs
        /// </summary>
        public async Task<Dictionary<string, bool>> GetJobsStatusAsync()
        {
            return new Dictionary<string, bool>(_jobStatus);
        }

        /// <summary>
        /// Получает статус конкретного Job
        /// </summary>
        public async Task<bool> IsJobEnabledAsync(string jobName)
        {
            return _jobStatus.TryGetValue(jobName, out var status) && status;
        }

        /// <summary>
        /// Проверяет, можно ли выполнять Job
        /// </summary>
        public static bool CanExecuteJob(string jobName)
        {
            return _jobStatus.TryGetValue(jobName, out var status) && status;
        }

        /// <summary>
        /// Получает список доступных Jobs
        /// </summary>
        public static List<string> GetAvailableJobs()
        {
            return new List<string>(_jobStatus.Keys);
        }
    }
}