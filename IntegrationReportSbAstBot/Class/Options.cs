namespace IntegrationReportSbAstBot.Class.Options
{
    /// <summary>
    /// Настройки для конфигурации Telegram бота
    /// Содержит параметры, необходимые для подключения и работы Telegram бота
    /// </summary>
    public class TelegramOptions
    {
        /// <summary>
        /// Токен доступа к Telegram Bot API
        /// Получается у @BotFather при создании бота
        /// </summary>
        /// <example>"123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ"</example>
        public string BotToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Настройки для конфигурации задач Quartz
    /// Содержит параметры планирования для выполнения фоновых задач
    /// </summary>
    public class QuartzJobOptions
    {
        /// <summary>
        /// Расписание выполнения задачи в формате CRON
        /// Определяет, когда и как часто будет выполняться задача
        /// </summary>
        /// <example>"0 0/5 * * * ?" - каждые 5 минут</example>
        /// <example>"0 0 9 * * ?" - каждый день в 9:00</example>
        public string CronSchedule { get; set; } = string.Empty;
    }

    /// <summary>
    /// Настройки для конфигурации подключения к базе данных
    /// Содержит параметры подключения и выполнения команд базы данных
    /// </summary>
    public class DatabaseOptions
    {
        /// <summary>
        /// Строка подключения к базе данных
        /// Содержит информацию о сервере, базе данных и параметрах аутентификации
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Таймаут выполнения команды базы данных в секундах
        /// Определяет максимальное время ожидания выполнения команды
        /// </summary>
        /// <value>По умолчанию 30 секунд</value>
        public int CommandTimeout { get; set; } = 30;
    }


    /// <summary>
    /// Настройки для конфигурации подключения к базе данных
    /// Содержит параметры подключения и выполнения команд базы данных
    /// </summary>
    public class SqliteOptions
    {
        /// <summary>
        /// Строка подключения к базе данных
        /// Содержит информацию о сервере, базе данных и параметрах аутентификации
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Таймаут выполнения команды базы данных в секундах
        /// Определяет максимальное время ожидания выполнения команды
        /// </summary>
        /// <value>По умолчанию 30 секунд</value>
        public int CommandTimeout { get; set; } = 30;
    }
}