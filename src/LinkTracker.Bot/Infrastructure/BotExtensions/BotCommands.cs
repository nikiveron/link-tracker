using LinkTracker.Bot.Application.Commands;
using LinkTracker.Bot.Application.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Infrastructure.BotExtensions;

public class BotCommands
{
    private readonly ILogger _logger;
    private IEnumerable<ICommand> _commands;

    public BotCommands(ILogger<BotCommands> logger, IEnumerable<ICommand> commands)
    {
        _logger = logger;
        _commands = commands;
    }

    public async Task SetCommands(ITelegramBotClient botClient)
    {
        var formattedCommands = FormatCommandsAsync();

        _logger.LogInformation("Устанавливаем команды бота...");
        await botClient.SetMyCommands(formattedCommands, scope: new BotCommandScopeAllPrivateChats());
        _logger.LogInformation("Команды установлены");

        _logger.LogInformation("Установлено команд: {amount}", formattedCommands.Count());
        foreach (var cmd in formattedCommands)
        {
            _logger.LogInformation("Команда: /{Command} - {Description}", cmd.Command, cmd.Description);
        }
    }

    public IEnumerable<BotCommand> FormatCommandsAsync()
    {
        return _commands
            .Where(c => c is not UnknownCommand)
            .Select(c => new BotCommand
            {
                Command = c.Name.TrimStart('/'),
                Description = c.Description
            });
    }
}
