using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Class.Options;
using IntegrationReportSbAstBot.Data;
using IntegrationReportSbAstBot.Interfaces;
using IntegrationReportSbAstBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Опции
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<QuartzJobOptions>(builder.Configuration.GetSection("Quartz:Jobs:ReportJob"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

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
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddSingleton<IReportHtmlService, ReportHtmlService>();

// Quartz
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    var jobKey = new JobKey("ReportJob");
    q.AddJob<ReportJob>(opts => opts.WithIdentity(jobKey));


    var options = builder.Configuration.GetSection("Quartz:Jobs:ReportJob").Get<QuartzJobOptions>();
    if (options == null || string.IsNullOrWhiteSpace(options.CronSchedule))
        throw new InvalidOperationException("Quartz cron schedule not configured");

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ReportJob-trigger")
        .WithCronSchedule(options.CronSchedule));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();

//Запуск Telegram Bot 
var telegramBotService = host.Services.GetRequiredService<TelegramBotService>();
await telegramBotService.StartAsync();

await host.RunAsync();