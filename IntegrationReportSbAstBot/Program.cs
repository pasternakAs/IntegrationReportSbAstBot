using IntegrationReportSbAstBot.Class;
using IntegrationReportSbAstBot.Data;
using IntegrationReportSbAstBot.Interfaces;
using IntegrationReportSbAstBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Simpl;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Telegram Bot - регистрируем ITelegramBotClient 
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
    new TelegramBotClient("7634606068:AAG3uDPuMeCzfGz5UA5fARixigt43isFt7c"));
// Сервис для управления подписчиками
builder.Services.AddSingleton<ISubscriberService, SubscriberService>();
builder.Services.AddSingleton<TelegramBotService>();// Регистрируем сервис
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddSingleton<IReportHtmlService, ReportHtmlService>();

// Quartz scheduler
builder.Services.AddQuartz(q =>
{
    q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();

    var jobKey = new JobKey("ReportJob");
    q.AddJob<ReportJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ReportJob-trigger")
        .WithCronSchedule("0 0/2 * * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();

// Запуск бота
var telegramBotService = host.Services.GetRequiredService<TelegramBotService>();
await telegramBotService.StartAsync();

await host.RunAsync();