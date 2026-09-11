using LinkTracker.Bot.Application.StateMachine.States;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Application.StateMachine;

public class UserSession
{
    private readonly int _defaultPage = 1;
    private readonly int _defaultPageSize = 5;
    private readonly List<string> _defaultFilterTags = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<string> FilterTags { get; set; }

    public BaseUserState UserState { get; set; }
    public string PendingLink { get; set; }

    public UserSession(BaseUserState initialState)
    {
        UserState = initialState;
        PendingLink = string.Empty;
        Page = _defaultPage;
        PageSize = _defaultPageSize;
        FilterTags = _defaultFilterTags;
    }

    public async Task HandleMessage(Update update, CancellationToken ct)
    {
        await UserState.Handle(update, this, ct);
    }

    public async Task HandleCallback(CallbackQuery callback, CancellationToken ct)
    {
        var data = callback.Data;

        if (data == "noop" || data == null)
        {
            return;
        }

        var parts = data.Split(':');

        if (parts[0] == "list" && parts[1] == "page")
        {
            int newPage = int.Parse(parts[2]);
            Page = Math.Max(1, newPage);
            await UserState.ShowPage(callback, this, ct);
        }
    }

    public void SetState(BaseUserState userState)
    {
        UserState = userState;
    }

    public void SetDefaultPagination()
    {
        Page = _defaultPage;
        PageSize = _defaultPageSize;
        FilterTags = _defaultFilterTags;
    }
}
