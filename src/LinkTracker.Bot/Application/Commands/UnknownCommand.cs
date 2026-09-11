using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.StateMachine;
using LinkTracker.Bot.Application.StateMachine.States;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.Commands;

public class UnknownCommand : ICommand
{
    private readonly ITelegramBotClient _bot;
    private readonly UserStateManager _userStateManager;

    public string Name => string.Empty;
    public string Description => "Неизвестная команда, вызванная пользователем";

    public UnknownCommand(ITelegramBotClient client, UserStateManager userStateManager)
    {
        _bot = client;
        _userStateManager = userStateManager;
    }

    public async Task ExecuteAsync(long chatId, CancellationToken ct)
    {
        await _bot.SendMessage(chatId, "Неизвестная команда! Попробуйте /help чтобы узнать доступные команды.", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        _userStateManager.SetState<DefaultState>(chatId, ct);
    }
}
