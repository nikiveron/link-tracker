using LinkTracker.Bot.Domain.Exceptions;
using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.StateMachine.States.UnTrackStates;

public class WaitingForDeleteLinkState : BaseUserState
{
    public WaitingForDeleteLinkState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
    }

    public override async Task Handle(Update update, UserSession userSession, CancellationToken ct)
    {
        var chatId = update.Message!.Chat.Id;
        if (await IsProcessCanceled(update, ct)) { return; }

        string link = update.Message!.Text!;
        try
        {
            await _scrapperClient.RemoveLinkAsync(chatId, link, ct);
            await _bot.SendMessage(chatId, $"Ссылка успешно удалена!", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (ScrapperBadRequestException ex)
        {
            await _bot.SendMessage(chatId, $"Некорректные параметры запроса: {ex.Message}", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (ScrapperNotFoundException)
        {
            await _bot.SendMessage(chatId, "Ссылка для удаления не найдена. Для просмотра всех ссылок попробуйте /list", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (ScrapperApiException)
        {
            await _bot.SendMessage(chatId, "Произошла ошибка при удалении ссылки. Попробуйте позже.", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (Exception)
        {
            await _bot.SendMessage(chatId, "Произошла ошибка при удалении ссылки. Попробуйте позже.", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        finally
        {
            _userStateManager.SetState<DefaultState>(chatId, ct);
        }
    }
}
