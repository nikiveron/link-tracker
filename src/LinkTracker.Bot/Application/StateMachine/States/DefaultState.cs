using LinkTracker.Bot.Application.Commands;
using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Application.StateMachine.States;

public class DefaultState : BaseUserState
{
    private CommandDispatcher _commandDispatcher;

    public DefaultState(ITelegramBotClient bot, UserStateManager userStateManager, CommandDispatcher commandDispatcher, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
        _commandDispatcher = commandDispatcher;
    }

    public override async Task Handle(Update update, UserSession userSession, CancellationToken ct)
    {
        userSession.SetDefaultPagination();
        await _commandDispatcher.DispatchAsync(update.Message!.Text!, update.Message!.Chat.Id, ct);
    }
}
