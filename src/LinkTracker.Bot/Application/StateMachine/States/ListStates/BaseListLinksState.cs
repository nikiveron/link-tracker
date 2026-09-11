using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.StateMachine.States.ListStates;

public abstract class BaseListLinksState : BaseUserState
{
    protected BaseListLinksState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
    }

    protected async Task ShowLinksAsync(long chatId, UserSession userSession, CancellationToken ct)
    {
        int page = userSession.Page;
        int pageSize = userSession.PageSize;
        var filterTags = userSession.FilterTags;

        var response = await _scrapperClient.GetLinksAsync(chatId, page, pageSize, filterTags, ct);

        if (response == null || response.Links.Count == 0)
        {
            var emptyMessage = filterTags.Count != 0 == true ? "Нет ссылок с указанными тегами. Попробуйте добавить ссылку с помощью /track" : "У вас нет ссылок. Попробуйте добавить ссылку с помощью /track";

            await _bot.SendMessage(chatId, emptyMessage, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            _userStateManager.SetState<DefaultState>(chatId, ct);
            return;
        }

        int totalPages = (int)Math.Ceiling((double)response.TotalCount / pageSize);
        string message = string.Join("\n\n", response.Links.Select(l => l.ToString()));
        var keyboard = BuildPaginationKeyboard(page, totalPages);

        var header = $"📋 Ссылки: {response.TotalCount}шт. (страница {page}/{totalPages}):";

        await _bot.SendMessage(chatId, $"{header}\n\n{message}", replyMarkup: keyboard, cancellationToken: ct);
        _userStateManager.SetState<DefaultState>(chatId, ct);
    }
}