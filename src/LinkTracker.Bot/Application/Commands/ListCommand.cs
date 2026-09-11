using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.StateMachine;
using LinkTracker.Bot.Application.StateMachine.States.ListStates;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.Commands;

public class ListCommand : ICommand
{
    private readonly ITelegramBotClient _bot;
    private readonly UserStateManager _userStateManager;
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

    public string Name => "/list";
    public string Description => "Вывести список ссылок";

    public ListCommand(ITelegramBotClient bot, UserStateManager userStateManager)
    {
        _bot = bot;
        _userStateManager = userStateManager;
    }

    public async Task ExecuteAsync(long chatId, CancellationToken ct)
    {
        await _bot.SendMessage(chatId, $"Хотите просмотреть ссылки по определенному тегу?", replyMarkup: _keyboard, cancellationToken: ct);
        _userStateManager.SetState<WaitingForFilterTagsConfirmationState>(chatId, ct);
    }
}

