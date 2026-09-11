using LinkTracker.Bot.Application.Interfaces;

namespace LinkTracker.Bot.Application.Commands;

public class CommandDispatcher
{
    private ILogger _logger;
    private readonly IEnumerable<ICommand> _commands;
    private readonly UnknownCommand _unknownCommand;
    private readonly IBotMetricsService _metricsService;

    public CommandDispatcher(ILogger<CommandDispatcher> logger, IEnumerable<ICommand> commands, UnknownCommand unknownCommand, IBotMetricsService metricsService)
    {
        _logger = logger;
        _commands = commands;
        _unknownCommand = unknownCommand;
        _metricsService = metricsService;
    }

    public async Task DispatchAsync(string text, long chatId, CancellationToken ct)
    {
        _logger.LogDebug("Обработка сообщения для чата {ChatId}: {Message}", chatId, text);
        var command = _commands.FirstOrDefault(c => c.Name == text);

        if (command is null)
        {
            _logger.LogWarning("Неизвестная команда от чата {ChatId}: {Message}", chatId, text);
            await _unknownCommand.ExecuteAsync(chatId, ct);
            _metricsService.TrackCommandRequest("unknown");
        }
        else
        {
            _logger.LogInformation("Выполнение команды {CommandType} для чата {ChatId}", command.GetType().Name, chatId);
            _metricsService.TrackCommandRequest(command.Name);

            try
            {
                await command.ExecuteAsync(chatId, ct);
                _logger.LogDebug("Команда {CommandType} успешно выполнена для чата {ChatId}", command.GetType().Name, chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении команды {CommandType} для чата {ChatId}", command.GetType().Name, chatId);
            }
        }
    }
}
