using LinkTracker.Bot.Application.Commands;
using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.StateMachine.States;

public abstract class BaseUserState
{
    protected ITelegramBotClient _bot;
    protected readonly IScrapperClient _scrapperClient;
    protected readonly UserStateManager _userStateManager;
    private readonly CancelCommand _cancelCommand;

    public BaseUserState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient)
    {
        _bot = bot;
        _scrapperClient = scrapperClient;
        _userStateManager = userStateManager;
        _cancelCommand = new CancelCommand(_bot, _userStateManager);
    }

    public abstract Task Handle(Update update, UserSession userSession, CancellationToken cancellationToken);

    public async Task<bool> IsProcessCanceled(Update update, CancellationToken ct)
    {
        var message = update.Message!.Text!;
        var chatId = update.Message.Chat.Id;

        if (message.Trim() == _cancelCommand.Name)
        {
            await _cancelCommand.ExecuteAsync(chatId, ct);
            return true;
        }
        return false;
    }

    protected InlineKeyboardMarkup BuildPaginationKeyboard(int page, int totalPages)
    {
        var navigationButtons = new List<InlineKeyboardButton>();

        if (page > 1)
        {
            navigationButtons.Add(new InlineKeyboardButton("◀️")
            {
                CallbackData = $"list:page:{page - 1}"
            });
        }

        navigationButtons.Add(new InlineKeyboardButton($"{page}/{totalPages}")
        {
            CallbackData = "ignore"
        });

        if (page < totalPages)
        {
            navigationButtons.Add(new InlineKeyboardButton("▶️")
            {
                CallbackData = $"list:page:{page + 1}"
            });
        }

        if (navigationButtons.Count > 1)
        {
            return new InlineKeyboardMarkup([[.. navigationButtons]]);
        }

        return null!;
    }

    public async Task ShowPage(CallbackQuery callback, UserSession userSession, CancellationToken ct)
    {
        var chatId = callback.Message!.Chat.Id;
        if (callback.Data?.StartsWith("list:page:") == true)
        {
            userSession.Page = int.Parse(callback.Data.Split(':')[2]);
        }

        var response = await _scrapperClient.GetLinksAsync(callback.Message!.Chat.Id, userSession.Page, userSession.PageSize, userSession.FilterTags, ct);
        if (response == null || response.Links.Count == 0)
        {
            await _bot.SendMessage(chatId, "У вас нет ссылок", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            return;
        }

        int totalPages = (int)Math.Ceiling(response.TotalCount / (double)userSession.PageSize);
        userSession.Page = Math.Clamp(userSession.Page, 1, totalPages);

        string message = string.Join("\n\n", response.Links.Select(l => l.ToString()));
        var keyboard = BuildPaginationKeyboard(userSession.Page, totalPages);

        await _bot.EditMessageText(chatId: chatId, messageId: callback.Message.MessageId, text: $"📋 Ссылки: {response.TotalCount}шт. (страница {userSession.Page}/{totalPages}):\n\n{message}", replyMarkup: keyboard, cancellationToken: ct);
    }
}