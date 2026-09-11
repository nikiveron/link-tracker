using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.StateMachine.States.ListStates;

public class WaitingForFilterTagsState : BaseListLinksState
{
    public WaitingForFilterTagsState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
    }

    public override async Task Handle(Update update, UserSession userSession, CancellationToken ct)
    {
        var chatId = update.Message!.Chat.Id;
        if (await IsProcessCanceled(update, ct)) { return; }

        var filterTags = update.Message!.Text!.Split(',').Select(t => t.Trim().ToLowerInvariant()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        userSession.FilterTags = filterTags;

        await _bot.SendMessage(chatId, $"Хорошо, показываю ссылки с фильтрами по тегам: {string.Join(", ", filterTags)}...", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        await ShowLinksAsync(chatId, userSession, ct);
    }
}

