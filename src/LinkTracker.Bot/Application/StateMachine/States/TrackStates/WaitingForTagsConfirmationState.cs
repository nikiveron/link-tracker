using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.StateMachine.States.TrackStates;

public class WaitingForTagsConfirmationState : BaseAddLinkState
{
    public WaitingForTagsConfirmationState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
    }

    public override async Task Handle(Update update, UserSession userSession, CancellationToken ct)
    {
        var chatId = update.Message!.Chat.Id;
        if (await IsProcessCanceled(update, ct)) { return; }

        var messageText = update.Message!.Text;

        if (messageText == "Да")
        {
            await _bot.SendMessage(chatId, "Пришли теги для ссылки, перечисли их через запятую.\n\nНапример:\nтег1, тег2, тег3", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            _userStateManager.SetState<WaitingForTagsState>(chatId, ct);
        }
        else
        {
            await AddLinkAsync(chatId, tags: [], userSession, ct);
        }
    }
}
