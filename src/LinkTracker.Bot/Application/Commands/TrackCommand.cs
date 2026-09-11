using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.StateMachine;
using LinkTracker.Bot.Application.StateMachine.States.TrackStates;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.Commands;

public class TrackCommand : ICommand
{
    private readonly ITelegramBotClient _bot;
    private readonly UserStateManager _userStateManager;

    public string Name => "/track";
    public string Description => "Начать отслеживание ссылки";

    public TrackCommand(ITelegramBotClient bot, UserStateManager userStateManager)
    {
        _bot = bot;
        _userStateManager = userStateManager;
    }

    public async Task ExecuteAsync(long chatId, CancellationToken ct)
    {
        await _bot.SendMessage(chatId, "Пришли адрес ссылки", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        _userStateManager.SetState<WaitingForAddLinkState>(chatId, ct);
    }
}

