using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Class.Options;
using IntegrationReportSbAstBot.Data;
using IntegrationReportSbAstBot.Interfaces;
using IntegrationReportSbAstBot.Jobs;
using IntegrationReportSbAstBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Явно устанавливаем окружение для разработки (только для отладки!)
#if DEBUG
builder.Configuration.AddEnvironmentVariables();
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
#endif

// Загружаем конфигурацию с учетом окружения
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
Console.WriteLine($"Environment: {environment}");

// Опции с поддержкой окружения
var config = builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Опции
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<QuartzJobOptions>(builder.Configuration.GetSection("Quartz:Jobs:ReportJob"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("BotSettings"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Sqlite")); // Для SQLite

// Telegram Bot
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TelegramOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BotToken))
    {
        throw new InvalidOperationException("Telegram BotToken is not configured");
    }

    return new TelegramBotClient(options.BotToken);
});

// Сервисы
builder.Services.AddSingleton<ISubscriberService, SubscriberService>();
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddSingleton<IReportHtmlService, ReportHtmlService>();
builder.Services.AddSingleton<IProcedureInfoService, GenerateReportForProcedure>();
builder.Services.AddSingleton<ISqliteConnectionFactory, SqlLiteConnectionFactory>(); // Для авторизации
builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();

// Quartz
builder.Services.AddQuartz(q =>
{
    var options = builder.Configuration.GetSection("Quartz:Jobs:ReportJob").Get<QuartzJobOptions>();
    if (options == null || string.IsNullOrWhiteSpace(options.CronSchedule))
        throw new InvalidOperationException("Quartz cron schedule not configured");

    Console.WriteLine($"[Quartz] Cron: {options?.CronSchedule}");

    q.ScheduleJob<ReportJob>(trigger => trigger
        .WithIdentity("ReportJob-trigger")
        .WithCronSchedule(options.CronSchedule));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();

//Запуск Telegram Bot 
var telegramBotService = host.Services.GetRequiredService<TelegramBotService>();
await telegramBotService.StartAsync();

await host.RunAsync();