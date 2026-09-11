using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.StateMachine;
using LinkTracker.Bot.Application.StateMachine.States;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.Commands;

public class CancelCommand : ICommand
{
    private readonly ITelegramBotClient _bot;
    private readonly UserStateManager _userStateManager;

    public string Name => "/cancel";
    public string Description => "Прервать процесс";

    public CancelCommand(ITelegramBotClient bot, UserStateManager userStateManager)
    {
        _bot = bot;
        _userStateManager = userStateManager;
    }

    public async Task ExecuteAsync(long chatId, CancellationToken ct)
    {
        await _bot.SendMessage(chatId, "Процесс прерван!", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        _userStateManager.SetState<DefaultState>(chatId, ct);
    }
}
