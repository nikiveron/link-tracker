using LinkTracker.Bot.Domain.Exceptions;
using LinkTracker.Bot.Infrastructure.Client;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.Application.StateMachine.States.TrackStates;

public abstract class BaseAddLinkState : BaseUserState
{
    protected BaseAddLinkState(ITelegramBotClient bot, UserStateManager userStateManager, IScrapperClient scrapperClient) : base(bot, userStateManager, scrapperClient)
    {
    }

    protected async Task AddLinkAsync(long chatId, List<string> tags, UserSession userSession, CancellationToken ct)
    {
        try
        {
            await _scrapperClient.AddLinkAsync(chatId, userSession.PendingLink, tags, ct);

            string successMessage = SuccessMessageConstrctror(userSession.PendingLink, tags);
            await _bot.SendMessage(chatId, successMessage, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (ScrapperConflictException)
        {
            await _bot.SendMessage(chatId, "Эта ссылка уже отслеживается в системе!", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (ScrapperBadRequestException ex)
        {
            await _bot.SendMessage(chatId, $"Некорректный запрос: {ex.Message}", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (ScrapperNotFoundException)
        {
            await _bot.SendMessage(chatId, "Чат не найден или не зарегистрирован. Попробуйте начать диалог сначала.", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (ScrapperApiException)
        {
            await _bot.SendMessage(chatId, "Произошла ошибка при добавлении ссылки. Попробуйте позже.", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        catch (Exception)
        {
            await _bot.SendMessage(chatId, "Произошла ошибка. Попробуйте позже.", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        }
        finally
        {
            userSession.PendingLink = string.Empty;
            _userStateManager.SetState<DefaultState>(chatId, ct);
        }
    }

    private static string SuccessMessageConstrctror(string link, List<string> tags)
    {
        var tagsText = tags.Count > 0 ? $"🏷️ Теги: {string.Join(", ", tags)}" : "🏷️ Теги: не указаны";
        string linkInfo = $"🔗 Ссылка: {link}\n{tagsText}";
        return tags.Count != 0 ? $"Ссылка с тегами добавлена! \n\n{linkInfo}" : $"Ссылка без тегов добавлена! \n\n{linkInfo}";
    }
}