namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Представляет авторизованного пользователя системы Telegram бота
    /// Содержит информацию о пользователях, которым предоставлен доступ к функциональности бота
    /// </summary>
    public class AuthorizedUser
    {
        /// <summary>
        /// Уникальный идентификатор записи об авторизованном пользователе
        /// Автоинкрементное значение, присваивается при добавлении пользователя в систему авторизации
        /// </summary>
        /// <example>1, 2, 3, ...</example>
        public long Id { get; set; }

        /// <summary>
        /// Уникальный идентификатор пользователя в Telegram
        /// Используется для идентификации пользователя в системе Telegram и проверки авторизации
        /// </summary>
        /// <example>6426583094</example>
        public long UserId { get; set; }

        /// <summary>
        /// Имя пользователя в Telegram
        /// Может содержать username (@username) или имя/фамилию пользователя для идентификации
        /// </summary>
        /// <example>"@john_doe", "John Smith"</example>
        public string UserName { get; set; }

        /// <summary>
        /// Идентификатор чата пользователя в Telegram
        /// Используется для отправки уведомлений, отчетов и персональных сообщений пользователю
        /// </summary>
        /// <example>6426583094</example>
        public long ChatId { get; set; }

        /// <summary>
        /// Дата и время авторизации пользователя
        /// Временная метка UTC когда пользователь был добавлен в список авторизованных
        /// </summary>
        /// <example>2024-01-15 15:45:00</example>
        public DateTime AuthorizedAt { get; set; }

        /// <summary>
        /// Идентификатор администратора, предоставившего доступ
        /// Содержит UserId администратора, который одобрил запрос на авторизацию
        /// </summary>
        /// <example>123456789</example>
        public long AuthorizedBy { get; set; }

        /// <summary>
        /// Статус активности пользователя в системе
        /// True если пользователь имеет активный доступ, False если доступ был отозван
        /// </summary>
        /// <value>True по умолчанию</value>
        /// <remarks>
        /// True - пользователь активен и имеет доступ к боту
        /// False - доступ пользователя был отозван администратором
        /// </remarks>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Дополнительные заметки или комментарии об авторизации пользователя
        /// Может содержать причину авторизации, должность пользователя или другую метаинформацию
        /// </summary>
        /// <example>"Сотрудник отдела закупок", "Временный доступ до 31.12.2024"</example>
        public string Notes { get; set; }

        public bool IsSubscribe { get; set; }
    }
}