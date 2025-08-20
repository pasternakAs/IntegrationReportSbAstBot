using System.Net;
using System.Text;
using IntegrationReportSbAstBot.Interfaces;

namespace IntegrationReportSbAstBot.Class
{
    /// <summary>
    /// Сервис для генерации HTML-отчета по пакетам.
    /// В отчете формируются:
    ///  - сводка по важным пакетам за последние сутки,
    ///  - общая сводка по всем пакетам,
    ///  - детализированная таблица по каждому пакету.
    /// </summary>
    public class ReportHtmlService : IReportHtmlService
    {
        public ReportHtmlService() { }

        /// <summary>
        /// Формирует полный HTML-отчет на основе данных <see cref="ReportDataClass"/>.
        /// </summary>
        /// <param name="reportData">Данные по пакетам для отчета.</param>
        /// <returns>Готовая HTML-страница в виде строки.</returns>
        public string GenerateHtmlReport(ReportDataClass reportData)
        {
            var sb = new StringBuilder();

            sb.Append($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Отчет по пакетам</title>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        table {{ border-collapse: collapse; width: 100%; margin-bottom: 20px; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        tr:nth-child(even) {{ background-color: #f9f9f9; }}
    </style>
</head>
<body>
    <h1>Отчет по важным пакетам</h1>
    <p>Сформирован: {reportData.GeneratedAt:dd.MM.yyyy HH:mm}</p>
    <p>Всего пакетов: {reportData.TotalCount}</p>
");

            // Добавляем блоки отчета
            sb.Append(GenerateDailySummaryTable(reportData)); // сводка за сутки
            sb.Append(GenerateSummaryTable(reportData));      // сводка по всем
            sb.Append(GenerateDetailTable(reportData));       // детализация

            sb.Append("</body></html>");

            return sb.ToString();
        }

        /// <summary>
        /// Генерирует таблицу "Сводка по важным пакетам за последние сутки"
        /// Берутся только пакеты, у которых LastSendDate >= (текущая дата - 1 день)
        /// Группировка по типу пакета (DocumentType)
        /// </summary>
        /// <param name="reportData">Данные по пакетам</param>
        /// <returns>HTML-код таблицы или пустая строка, если данных нет</returns>
        private string GenerateDailySummaryTable(ReportDataClass reportData)
        {
            var sb = new StringBuilder();

            var lastDay = DateTime.Now.AddDays(-1);
            var dailySummary = reportData.Packages
                .Where(p => p.LastSendDate >= lastDay)
                .GroupBy(p => p.DocumentType)
                .OrderByDescending(g => g.Count());

            if (dailySummary.Any())
            {
                sb.Append(@"
    <h2>Сводка по важным пакетам за последние сутки</h2>
    <table>
        <thead>
            <tr>
                <th>Тип пакета</th>
                <th>Количество</th>
            </tr>
        </thead>
        <tbody>");

                foreach (var group in dailySummary)
                {
                    sb.Append($@"
            <tr>
                <td>{WebUtility.HtmlEncode(group.Key)}</td>
                <td>{group.Count()}</td>
            </tr>");
                }

                sb.Append("</tbody></table>");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Генерирует таблицу "Сводка по всем пакетам"
        /// Группировка по типу пакета (DocumentType), сортировка по количеству
        /// </summary>
        /// <param name="reportData">Данные по пакетам</param>
        /// <returns>HTML-код таблицы</returns>
        private string GenerateSummaryTable(ReportDataClass reportData)
        {
            var sb = new StringBuilder();

            var summary = reportData.Packages
                .GroupBy(p => p.DocumentType)
                .OrderByDescending(g => g.Count());

            sb.Append(@"
    <h2>Сводка по всем пакетам</h2>
    <table>
        <thead>
            <tr>
                <th>Тип пакета</th>
                <th>Количество</th>
            </tr>
        </thead>
        <tbody>");

            foreach (var group in summary)
            {
                sb.Append($@"
            <tr>
                <td>{WebUtility.HtmlEncode(group.Key)}</td>
                <td>{group.Count()}</td>
            </tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        /// <summary>
        /// Генерирует детализированную таблицу по каждому пакету.
        /// Для каждого пакета выводятся:
        ///  - идентификатор процедуры (ObjectId),
        ///  - тип пакета (DocumentType),
        ///  - ошибка (Violations),
        ///  - направление (InOut),
        ///  - последняя дата отправки (LastSendDate).
        /// </summary>
        /// <param name="reportData">Данные по пакетам.</param>
        /// <returns>HTML-код таблицы.</returns>
        private string GenerateDetailTable(ReportDataClass reportData)
        {
            var sb = new StringBuilder();

            sb.Append(@"
    <h2>Детализация</h2>
    <table>
        <thead>
            <tr>
                <th>Процедура</th>
                <th>Тип пакета</th>
                <th>Ошибка</th>
                <th>Направление</th>
                <th>Последняя дата отправки</th>
            </tr>
        </thead>
        <tbody>");

            foreach (var package in reportData.Packages.OrderByDescending(p => p.LastSendDate))
            {
                sb.Append($@"
            <tr>
                <td>{WebUtility.HtmlEncode(package.ObjectId.ToString())}</td>
                <td>{WebUtility.HtmlEncode(package.DocumentType)}</td>
                <td>{WebUtility.HtmlEncode(package.Violations)}</td>
                <td>{WebUtility.HtmlEncode(package.InOut)}</td>
                <td>{package.LastSendDate:dd.MM.yyyy HH:mm}</td>
            </tr>");
            }

            sb.Append("</tbody></table>");
            return sb.ToString();
        }
    }
}
