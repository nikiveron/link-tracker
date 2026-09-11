using LinkTracker.Bot.Application.StateMachine.States;
using LinkTracker.Bot.Domain.Exceptions;
using LinkTracker.Bot.Infrastructure.Client;

namespace LinkTracker.Bot.Application.StateMachine;

public class UserStateManager
{
    private readonly Dictionary<long, UserSession> _userSessions = [];
    private readonly IScrapperClient _scrapperClient;
    private readonly StateFactory _stateFactory;
    private readonly ILogger<UserStateManager> _logger;

    public UserStateManager(IScrapperClient scrapperClient, StateFactory stateFactory, ILogger<UserStateManager> logger)
    {
        _scrapperClient = scrapperClient;
        _stateFactory = stateFactory;
        _logger = logger;
    }

    public UserSession GetUser(long chatId, CancellationToken ct = default)
    {
        CheckAndCreateNewUser(chatId, out UserSession value, ct);
        return value;
    }

    public BaseUserState GetState(long chatId, CancellationToken ct = default)
    {
        CheckAndCreateNewUser(chatId, out UserSession value, ct);
        return value.UserState;
    }

    public void SetState<T>(long chatId, CancellationToken ct = default) where T : BaseUserState
    {
        CheckAndCreateNewUser(chatId, out UserSession value, ct);
        value.UserState = _stateFactory.Create(typeof(T));
    }

    private bool CheckAndCreateNewUser(long chatId, out UserSession value, CancellationToken ct = default)
    {
        if (_userSessions.TryGetValue(chatId, out value!))
        {
            return true;
        }
        else
        {
            var initialState = _stateFactory.Create(typeof(DefaultState));
            value = new UserSession(initialState);
            _userSessions[chatId] = value;
            _ = Task.Run(async () => await RegisterChatAsync(chatId, ct), ct);
            return false;
        }
    }

    private async Task RegisterChatAsync(long chatId, CancellationToken ct = default)
    {
        try
        {
            await _scrapperClient.RegisterChatAsync(chatId, ct);
            _logger.LogInformation("Чат {ChatId} успешно зарегистрирован в Scrapper", chatId);
        }
        catch (ScrapperConflictException)
        {
            _logger.LogInformation("Чат {ChatId} уже зарегистрирован в Scrapper", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при регистрации чата {ChatId} в Scrapper", chatId);
        }
    }
}
