using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.StateMachine;
using LinkTracker.Bot.Application.StateMachine.States;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.Commands;

public class HelpCommand : ICommand
{
    private readonly ITelegramBotClient _bot;
    private readonly UserStateManager _userStateManager;

    public string Name => "/help";
    public string Description => "Помощь";

    public HelpCommand(ITelegramBotClient bot, UserStateManager userStateManager)
    {
        _bot = bot;
        _userStateManager = userStateManager;
    }

    public async Task ExecuteAsync(long chatId, CancellationToken ct)
    {
        await _bot.SendMessage(chatId, "Доступные команды:\n\n/start - начать работу\n\n/help - список команд\n\n/track - начать отслеживание ссылки. Опционально вы можете указать один или несколько тегов, привязанных к ссылке.\n\n/untrack — прекратить отслеживание ссылки\n\n/list — вывести список всех ссылок, отслеживаемых пользователем. Опционально вторым параметром можно указать тег — в этом случае список ссылок будет отфильтрован по указанному тегу.\n\n/cancel - отменить запустивщийся процесс, вернуться в главное меню для использования других команд", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        _userStateManager.SetState<DefaultState>(chatId, ct);
    }
}
