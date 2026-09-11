using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Application.StateMachine.States.TrackStates;

public class WaitingForTagsState : BaseAddLinkState
{
    public WaitingForTagsState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
    }

    public override async Task Handle(Update update, UserSession userSession, CancellationToken ct)
    {
        var chatId = update.Message!.Chat.Id;
        if (await IsProcessCanceled(update, ct)) { return; }

        var tags = update.Message!.Text!.Split(',').Select(t => t.Trim().ToLowerInvariant()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        await AddLinkAsync(chatId, tags, userSession, ct);
    }
}
