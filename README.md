# IntegrationReportSbAstBot

Telegram-бот для отправки отчётов по интеграции (замена рассылки через SQL Server).  
Реализован на **.NET 8**, с использованием **Quartz**, **Dapper**, **SQLite**.

## Возможности
- Ежедневная отправка отчётов по интеграции в Telegram.
- Авторизация пользователей с подтверждением администратором.
- Управление доступом (enable/disable бота).
- Сводка и детализация ошибок процедур.
- Хранение состояния бота и авторизационных запросов в SQLite.

## Технологии
- [.NET 8](https://dotnet.microsoft.com/)
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)
- [Quartz.NET](https://www.quartz-scheduler.net/)
- [Dapper](https://github.com/DapperLib/Dapper)
- SQLite для локального хранения

## Конфигурация
Файл `appsettings.json`:
```json
{
  "Database": {
    "ConnectionString": "Data Source=bot.db"
  },
  "Telegram": {
    "BotToken": "YOUR_TOKEN"
  },
  "Quartz": {
    "Jobs": {
      "ReportJob": {
        "CronSchedule": "0 0 9 * * ?"
      }
    }
  }
}
