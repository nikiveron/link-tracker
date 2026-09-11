using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.StateMachine.States.ListStates;

public class WaitingForFilterTagsConfirmationState : BaseListLinksState
{
    public WaitingForFilterTagsConfirmationState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
    }

    public override async Task Handle(Update update, UserSession userSession, CancellationToken ct)
    {
        var chatId = update.Message!.Chat.Id;
        if (await IsProcessCanceled(update, ct)) { return; }

        var messageText = update.Message!.Text;

        if (messageText == "Да")
        {
            await _bot.SendMessage(chatId, "Пришли теги, по которым нужно отфильтровать ссылки, перечисли их через запятую.\n\nНапример:\nтег1, тег2, тег3", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            _userStateManager.SetState<WaitingForFilterTagsState>(chatId, ct);
        }
        else
        {
            await _bot.SendMessage(chatId, "Хорошо, показываю все ссылки...", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            await ShowLinksAsync(chatId, userSession, ct);
        }
    }
}