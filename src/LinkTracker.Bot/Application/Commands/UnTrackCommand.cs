using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.StateMachine;
using LinkTracker.Bot.Application.StateMachine.States.UnTrackStates;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.Commands;

public class UnTrackCommand : ICommand
{
    private readonly ITelegramBotClient _bot;
    private readonly UserStateManager _userStateManager;

    public string Name => "/untrack";
    public string Description => "Прекратить отслеживание ссылки";

    public UnTrackCommand(ITelegramBotClient client, UserStateManager userStateManager)
    {
        _bot = client;
        _userStateManager = userStateManager;
    }

    public async Task ExecuteAsync(long chatId, CancellationToken ct)
    {
        await _bot.SendMessage(chatId, "Пришли адрес ссылки", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        _userStateManager.SetState<WaitingForDeleteLinkState>(chatId, ct);
    }
}

