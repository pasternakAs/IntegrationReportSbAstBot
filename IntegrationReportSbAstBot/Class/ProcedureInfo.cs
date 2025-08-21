namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Представляет информацию о документе процедуры для формирования отчетов
    /// Содержит детальную информацию о документах, их статусах и метаданных из системы госзакупок
    /// </summary>
    public class ProcedureInfo
    {
        /// <summary>
        /// Направление движения документа
        /// Указывает направление обмена между системами (AST --> EIS или AST <-- EIS)
        /// </summary>
        /// <example>"AST --> EIS" - документ отправлен из AST в ЕИС</example>
        /// <example>"AST <-- EIS" - документ получен из ЕИС в AST</example>
        public string Act { get; set; }

        /// <summary>
        /// XML-представление нарушений документа
        /// Содержит структурированную информацию о выявленных ошибках и предупреждениях
        /// </summary>
        /// <example>"<violations><error code="123">Текст ошибки</error></violations>"</example>
        public string ViolationsXML { get; set; }

        /// <summary>
        /// Уникальный идентификатор документа OOS
        /// Внутренний числовой идентификатор документа в системе OOS
        /// </summary>
        public int OOSDocId { get; set; }

        /// <summary>
        /// Номер протокола документа
        /// Уникальный номер протокола, извлеченный из XML-данных документа
        /// </summary>
        public string ProtocolNumber { get; set; }

        /// <summary>
        /// Индексный номер документа
        /// Дополнительный идентификатор документа для индексации и поиска
        /// </summary>
        public string IndexNum { get; set; }

        /// <summary>
        /// Статус документа
        /// Числовой код состояния обработки документа в системе
        /// </summary>
        /// <remarks>
        /// -1 - Ошибка
        /// -2 - Предупреждение  
        /// 0 - Обработан
        /// 1 - В обработке
        /// </remarks>
        public int State { get; set; }

        /// <summary>
        /// Тип документа
        /// Классификация документа в системе государственных закупок
        /// </summary>
        /// <example>"epProtocolEZK2020FinalPart", "epNotificationEOK"</example>
        public string DocType { get; set; }

        /// <summary>
        /// Уникальный GUID документа OOS
        /// Глобальный уникальный идентификатор документа в нижнем регистре
        /// </summary>
        /// <example>"550e8400-e29b-41d4-a716-446655440000"</example>
        public string OOSDocGuid { get; set; }

        /// <summary>
        /// Дата создания документа
        /// Временная метка создания документа в системе
        /// </summary>
        /// <example>2024-01-15 10:30:00</example>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Дата последней отправки документа
        /// Временная метка последней попытки отправки документа
        /// </summary>
        /// <example>2024-01-15 14:45:00</example>
        public DateTime LastSendDate { get; set; }

        /// <summary>
        /// Идентификатор документа
        /// Внешний идентификатор документа в системе-отправителе
        /// </summary>
        public string DocID { get; set; }

        /// <summary>
        /// Описание ожидания
        /// Текстовое описание причины ожидания обработки документа
        /// </summary>
        /// <example>"Ожидание подтверждения от ЕИС"</example>
        public string WaitingDescription { get; set; }
    }
}