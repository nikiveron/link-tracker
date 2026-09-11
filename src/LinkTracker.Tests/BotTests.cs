using LinkTracker.Bot.Application.Commands;
using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.StateMachine;
using LinkTracker.Bot.Application.StateMachine.States;
using LinkTracker.Bot.Infrastructure.Client;
using Microsoft.Extensions.Logging;
using Moq;
using NSubstitute;
using Telegram.Bot;

namespace LinkTracker.Tests;

public class BotTests
{
    private readonly ILogger<CommandDispatcher> _dispatcherLoggerMock;
    private readonly ILogger<UserStateManager> _userStateLoggerMock;

    private readonly IEnumerable<ICommand> _commandsMock;
    private readonly ICommand _startCommandMock;
    private readonly ICommand _helpCommandMock;
    private readonly UnknownCommand _unknownCommandMock;
    private readonly Mock<IBotMetricsService> _botMericsServiceMock = new();

    private readonly CommandDispatcher _commandDispatcherReal;

    public BotTests()
    {
        _dispatcherLoggerMock = Substitute.For<ILogger<CommandDispatcher>>();
        _userStateLoggerMock = Substitute.For<ILogger<UserStateManager>>();

        var botClientMock = Substitute.For<ITelegramBotClient>();
        var scrapperClientMock = Substitute.For<IScrapperClient>();

        _startCommandMock = Substitute.For<ICommand>();
        _helpCommandMock = Substitute.For<ICommand>();

        var serviceProviderMock = Substitute.For<IServiceProvider>();
        var stateFactory = new StateFactory(serviceProviderMock);
        var userStateManager = new UserStateManager(scrapperClientMock, stateFactory, _userStateLoggerMock);
        _unknownCommandMock = Substitute.For<UnknownCommand>(botClientMock, userStateManager);

        _startCommandMock.Name.Returns("/start");
        _helpCommandMock.Name.Returns("/help");

        _commandsMock = [_startCommandMock, _helpCommandMock];

        _commandDispatcherReal = new CommandDispatcher(_dispatcherLoggerMock, _commandsMock, _unknownCommandMock, _botMericsServiceMock.Object);

        var defaultState = new DefaultState(botClientMock, userStateManager, _commandDispatcherReal, scrapperClientMock);

        serviceProviderMock.GetService(typeof(DefaultState)).Returns(defaultState);
    }

    [Fact]
    public async Task StartCommand_ShouldSendWelcomeMessage()
    {
        // Arrange
        var chatId = 12345L;
        var commandText = "/start";

        // Act
        await _commandDispatcherReal.DispatchAsync(commandText, chatId, CancellationToken.None);

        // Assert
        await _startCommandMock.Received(1).ExecuteAsync(chatId, Arg.Any<CancellationToken>());
        await _helpCommandMock.DidNotReceive().ExecuteAsync(chatId, Arg.Any<CancellationToken>());
        await _unknownCommandMock.DidNotReceive().ExecuteAsync(chatId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HelpCommand_ShouldSendHelpMessage()
    {
        // Arrange
        var chatId = 12345L;
        var commandText = "/help";

        // Act
        await _commandDispatcherReal.DispatchAsync(commandText, chatId, CancellationToken.None);

        // Assert
        await _startCommandMock.DidNotReceive().ExecuteAsync(chatId, Arg.Any<CancellationToken>());
        await _helpCommandMock.Received(1).ExecuteAsync(chatId, Arg.Any<CancellationToken>());
        await _unknownCommandMock.DidNotReceive().ExecuteAsync(chatId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownCommand_ShouldSendTipMessage()
    {
        // Arrange
        var chatId = 12345L;
        var commandText = "/unknown";

        // Act
        await _commandDispatcherReal.DispatchAsync(commandText, chatId, CancellationToken.None);

        // Assert
        await _startCommandMock.DidNotReceive().ExecuteAsync(chatId, Arg.Any<CancellationToken>());
        await _helpCommandMock.DidNotReceive().ExecuteAsync(chatId, Arg.Any<CancellationToken>());
        await _unknownCommandMock.Received(1).ExecuteAsync(chatId, Arg.Any<CancellationToken>());
    }
}