using Dapper;
using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;

namespace IntegrationReportSbAstBot.Services
{
    /// <summary>
    /// Сервис для архивирования документов с ошибками валидации
    /// </summary>
    public class DocumentArchiveService(IDbConnectionFactory connectionFactory, ILogger<DocumentArchiveService> logger)
    {
        private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
        private readonly ILogger<DocumentArchiveService> _logger = logger;

        /// <summary>
        /// Архивирует документы с ошибками валидации Kind/NULL
        /// </summary>
        /// <returns>Количество обработанных документов</returns>
        public async Task<int> ArchiveDocumentsWithKindErrorsAsync()
        {
            try
            {
                await using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Начинаем транзакцию
                await using var transaction = connection.BeginTransaction();

                try
                {
                    // Находим документы для архивирования
                    const string findDocumentsSql = @"
                        SELECT OOSDocId, ObjectId, InOut, IndexNum
                        FROM docoosdoc WITH (NOLOCK)
                        WHERE violations LIKE 'Cannot insert the value NULL into column ''Kind'', table ''CDB.dbo.prmPersonAll''; column does not allow nulls. INSERT fails.'
                        AND state = -2";

                    var documentsToArchive = (await connection.QueryAsync<DocumentToArchive>(findDocumentsSql, transaction: transaction)).ToList();

                    if (documentsToArchive.Count == 0)
                    {
                        _logger.LogInformation("Нет документов для архивирования");
                        return 0;
                    }

                    _logger.LogInformation("Найдено {Count} документов для архивирования", documentsToArchive.Count);

                    // Обрабатываем каждый документ
                    foreach (var doc in documentsToArchive)
                    {
                        // Обновляем LastNum если нужно
                        await UpdateLastNumIfNeededAsync(connection, transaction, doc);

                        // Архивируем документ
                        const string updateDocumentSql = @"
                            UPDATE docoosdoc 
                            SET State = -3 
                            WHERE OOSDocId = @OOSDocId";

                        await connection.ExecuteAsync(updateDocumentSql, new { OOSDocId = doc.OOSDocId }, transaction);

                        _logger.LogInformation("Документ {OOSDocId} архивирован", doc.OOSDocId);
                    }

                    // Фиксируем транзакцию
                    transaction.Commit();

                    _logger.LogInformation("Успешно архивировано {Count} документов", documentsToArchive.Count);
                    return documentsToArchive.Count;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при архивировании документов");
                throw;
            }
        }

        /// <summary>
        /// Обновляет LastNum в oosObject если это необходимо
        /// </summary>
        private async Task UpdateLastNumIfNeededAsync(IDbConnection connection, IDbTransaction transaction, DocumentToArchive doc)
        {
            try
            {
                // Получаем текущее значение LastNum
                const string getLastNumSql = @"
                    SELECT lastnum
                    FROM oosObject WITH (NOLOCK)
                    WHERE ObjectId = @ObjectId AND InOut = @InOut";

                var lastNum = await connection.QueryFirstOrDefaultAsync<int?>(getLastNumSql,
                    new { doc.ObjectId, doc.InOut }, transaction);

                // Проверяем и обновляем LastNum если нужно
                if (lastNum.HasValue && lastNum.Value == doc.IndexNum && lastNum.Value != 1)
                {
                    const string updateLastNumSql = @"
                        UPDATE oosObject
                        SET lastnum = lastnum - 1
                        WHERE ObjectId = @ObjectId AND InOut = @InOut";

                    await connection.ExecuteAsync(updateLastNumSql,
                        new { doc.ObjectId, doc.InOut }, transaction);

                    _logger.LogInformation("Обновлен LastNum для ObjectId: {ObjectId}, InOut: {InOut}",
                        doc.ObjectId, doc.InOut);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении LastNum для документа {OOSDocId}", doc.OOSDocId);
                // Не прерываем основной процесс из-за ошибки обновления LastNum
            }
        }
    }
}