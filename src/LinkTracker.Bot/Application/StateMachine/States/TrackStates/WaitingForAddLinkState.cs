using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.StateMachine.States.TrackStates;

public class WaitingForAddLinkState : BaseUserState
{
    private readonly ReplyKeyboardMarkup _keyboard =
        new(
        [
            [
                new KeyboardButton("Да"),
                new KeyboardButton("Нет")
            ]
        ])
        {
            ResizeKeyboard = true
        };

    public WaitingForAddLinkState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
    }

    public override async Task Handle(Update update, UserSession userSession, CancellationToken ct)
    {
        var chatId = update.Message!.Chat.Id;
        if (await IsProcessCanceled(update, ct)) { return; }

        string link = update.Message!.Text!.Trim();
        userSession.PendingLink = link;
        await _bot.SendMessage(chatId, $"Хочешь присвоить теги для ссылки?", replyMarkup: _keyboard, cancellationToken: ct);
        _userStateManager.SetState<WaitingForTagsConfirmationState>(chatId, ct);
    }
}
