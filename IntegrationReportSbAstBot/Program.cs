using IntegrationReportSbAstBot.Class;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Simpl;
using Quartz.Spi;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Telegram Bot
builder.Services.AddSingleton(new TelegramBotClient("7634606068:AAG3uDPuMeCzfGz5UA5fARixigt43isFt7c"));

// Quartz scheduler
builder.Services.AddQuartz(q =>
{
    q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();

    var jobKey = new JobKey("ReportJob");
    q.AddJob<ReportJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ReportJob-trigger")
        .WithCronSchedule("0 0/5 * * * ?")); // каждый день в 9:00
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddSingleton<IJobFactory, MicrosoftDependencyInjectionJobFactory>();

var host = builder.Build();
await host.RunAsync();