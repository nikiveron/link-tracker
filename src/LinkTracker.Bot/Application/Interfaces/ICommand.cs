namespace LinkTracker.Bot.Application.Interfaces;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    Task ExecuteAsync(long chatId, CancellationToken ct);
}
