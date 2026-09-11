namespace LinkTracker.Bot.Application.StateMachine.States;

public class StateFactory
{
    private readonly IServiceProvider _provider;

    public StateFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public BaseUserState Create(Type stateType)
    {
        return (BaseUserState)_provider.GetRequiredService(stateType);
    }
}