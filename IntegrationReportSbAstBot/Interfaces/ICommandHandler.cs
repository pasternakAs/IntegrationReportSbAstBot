using Telegram.Bot.Types;

namespace IntegrationReportSbAstBot.Interfaces
{
    public interface ICommandHandler
    {
        string Command { get; }   // напр. "/start" или "/subscribe"
        Task HandleAsync(Message message, CancellationToken cancellationToken);
    }
}
