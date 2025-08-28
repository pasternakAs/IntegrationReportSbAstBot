namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Представляет запрос на авторизацию пользователя в системе Telegram бота
    /// Используется для управления доступом к функциональности бота и отслеживания запросов
    /// </summary>
    public class AuthorizationRequest
    {
        /// <summary>
        /// Уникальный идентификатор запроса на авторизацию
        /// Автоинкрементное значение, присваивается при создании запроса
        /// </summary>
        /// <example>1, 2, 3, ...</example>
        public long Id { get; set; }

        /// <summary>
        /// Уникальный идентификатор пользователя в Telegram
        /// Используется для идентификации пользователя в системе Telegram
        /// </summary>
        /// <example>6426583094</example>
        public long UserId { get; set; }

        /// <summary>
        /// Имя пользователя в Telegram
        /// Может содержать username (@username) или имя/фамилию пользователя
        /// </summary>
        /// <example>"@john_doe", "John Smith"</example>
        public string UserName { get; set; }

        /// <summary>
        /// Идентификатор чата пользователя в Telegram
        /// Используется для отправки уведомлений и сообщений пользователю
        /// </summary>
        /// <example>6426583094</example>
        public long ChatId { get; set; }

        /// <summary>
        /// Дата и время создания запроса на авторизацию
        /// Временная метка UTC когда пользователь отправил запрос
        /// </summary>
        /// <example>2024-01-15 14:30:00</example>
        public DateTime RequestedAt { get; set; }

        /// <summary>
        /// Сообщение сопровождающее запрос на авторизацию
        /// Дополнительная информация от пользователя или система генерации запроса
        /// </summary>
        /// <example>"Запрос доступа от сотрудника отдела закупок"</example>
        public string RequestMessage { get; set; }

        /// <summary>
        /// Статус одобрения запроса
        /// True если запрос одобрен администратором, False если отклонен или ожидает рассмотрения
        /// </summary>
        /// <remarks>
        /// False - по умолчанию (ожидает рассмотрения)
        /// True - одобрен администратором
        /// </remarks>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Статус обработки запроса
        /// True если запрос был рассмотрен администратором (одобрен или отклонен)
        /// </summary>
        /// <remarks>
        /// False - по умолчанию (ожидает рассмотрения)
        /// True - запрос обработан (IsApproved определяет результат)
        /// </remarks>
        public bool IsProcessed { get; set; }

        /// <summary>
        /// Идентификатор администратора, обработавшего запрос
        /// Содержит UserId администратора, который одобрил или отклонил запрос
        /// </summary>
        /// <example>123456789 (если обработан)</example>
        /// <value>null если запрос еще не обработан</value>
        public long? ProcessedBy { get; set; }

        /// <summary>
        /// Дата и время обработки запроса
        /// Временная метка UTC когда администратор обработал запрос
        /// </summary>
        /// <example>2024-01-15 15:45:00 (если обработан)</example>
        /// <value>null если запрос еще не обработан</value>
        public DateTime? ProcessedAt { get; set; }
    }
}