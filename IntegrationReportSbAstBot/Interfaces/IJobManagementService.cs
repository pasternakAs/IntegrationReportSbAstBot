// Interfaces/IJobManagementService.cs
using Quartz;

namespace IntegrationReportSbAstBot.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса управления Quartz Jobs
    /// </summary>
    public interface IJobManagementService
    {
        /// <summary>
        /// Включает выполнение всех Jobs
        /// </summary>
        Task EnableAllJobsAsync();

        /// <summary>
        /// Отключает выполнение всех Jobs
        /// </summary>
        Task DisableAllJobsAsync();

        /// <summary>
        /// Включает конкретный Job
        /// </summary>
        Task EnableJobAsync(string jobName);

        /// <summary>
        /// Отключает конкретный Job
        /// </summary>
        Task DisableJobAsync(string jobName);

        /// <summary>
        /// Получает статус всех Jobs
        /// </summary>
        Task<Dictionary<string, bool>> GetJobsStatusAsync();

        /// <summary>
        /// Получает статус конкретного Job
        /// </summary>
        Task<bool> IsJobEnabledAsync(string jobName);
    }
}