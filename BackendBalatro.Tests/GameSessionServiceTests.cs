/*
 * GameSessionServiceTests.cs - Unit Tests for Game Session Management
 *
 * This fixture documents the session-service contract: default and named
 * session resolution, session identity and lifecycle, thread-safe creation,
 * event subscription management, and structured logging.
 *
 * Key testing practices demonstrated:
 * - Arrange-Act-Assert (AAA)
 * - Dependency mocking with Moq
 * - Parameterized tests for equivalent input cases
 * - Assertions for object identity, lifecycle state, and logging behavior
 *
 */

using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Sessions;
using BackendBalatro.Services.Shop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackendBalatro.Tests;

/// <summary>
/// Test fixture for <see cref="GameSessionService"/>.
///
/// The service uses mocked scoring, shop, consumable, and logging dependencies
/// so these tests focus on session management and event-subscription behavior.
/// </summary>
[TestFixture]
public class GameSessionServiceTests
{
    // Mocked scoring dependency used when configuring a new game engine.
    private Mock<IScoringService> _mockScoringService;

    // Mocked shop dependency used to verify initial voucher generation.
    private Mock<IShopService> _mockShopService;

    // Mocked consumable dependency passed to each configured game engine.
    private Mock<IConsumableEffectHandler> _mockConsumableHandler;

    // System under test: creates, retrieves, and removes game sessions.
    private GameSessionService _service;

    // Mocked logger used to inspect structured log messages and scopes.
    private Mock<ILogger<GameSessionService>> _mockLogger;

    /// <summary>
    /// Creates fresh mocks and a new session service before every test to keep
    /// session state, dependency calls, and captured logs isolated.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _mockScoringService = new Mock<IScoringService>();
        _mockShopService = new Mock<IShopService>();
        _mockConsumableHandler = new Mock<IConsumableEffectHandler>();
        _mockLogger = new Mock<ILogger<GameSessionService>>();
        
        _service = new GameSessionService(
            _mockScoringService.Object,
            _mockShopService.Object,
            _mockConsumableHandler.Object,
            _mockLogger.Object,
            NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Verifies that null, empty, and whitespace-only IDs resolve to the
    /// default session with the expected initial game state.
    /// </summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void GetOrCreateSession_NullOrBlankId_UsesDefaultSession(string? sessionId)
    {
        var session = _service.GetOrCreateSession(sessionId);

        Assert.Multiple(() =>
        {
            Assert.That(session.SessionId, Is.EqualTo("default"));
            Assert.That(session.Player.Name, Is.EqualTo("Player 1"));
            Assert.That(session.Phase, Is.EqualTo(GameStatePhase.SelectingBlind));
            Assert.That(session.CurrentAnte, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that a new named session uses the supplied player name,
    /// initializes the game engine, and generates the first ante voucher.
    /// </summary>
    [Test]
    public void GetOrCreateSession_NewId_CreatesConfiguredEngine()
    {
        var session = _service.GetOrCreateSession("alpha", "Aji");

        Assert.Multiple(() =>
        {
            Assert.That(session.SessionId, Is.EqualTo("alpha"));
            Assert.That(session.Player.Name, Is.EqualTo("Aji"));
            Assert.That(session.Phase, Is.EqualTo(GameStatePhase.SelectingBlind));
            Assert.That(session.DrawPile.Count, Is.EqualTo(52));
        });
        _mockShopService.Verify(service => service.GenerateVoucherForAnte(1, session.PurchasedVouchers), Times.Once);
    }

    /// <summary>
    /// Verifies that requesting an existing session returns the same engine
    /// instance and preserves the original player's name.
    /// </summary>
    [Test]
    public void GetOrCreateSession_ExistingId_ReturnsSameInstance()
    {
        var first = _service.GetOrCreateSession("same-id", "First Player");
        var second = _service.GetOrCreateSession("same-id", "Second Player");

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.Player.Name, Is.EqualTo("First Player"));
        });
    }

    /// <summary>
    /// Verifies that concurrent requests for the same ID create and store only
    /// one shared session instance.
    /// </summary>
    [Test]
    public async Task GetOrCreateSession_ConcurrentSameId_ReturnsSingleInstance()
    {
        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() => _service.GetOrCreateSession("concurrent-id", "Player")));

        var sessions = await Task.WhenAll(tasks);
        var stored = _service.GetSession("concurrent-id");

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.Not.Null);
            Assert.That(sessions.All(session => ReferenceEquals(session, stored)), Is.True);
        });
    }

    /// <summary>
    /// Verifies that GetSession returns the stored engine for an existing ID.
    /// </summary>
    [Test]
    public void GetSession_ExistingId_ReturnsSession()
    {
        var created = _service.GetOrCreateSession("existing");

        Assert.That(_service.GetSession("existing"), Is.SameAs(created));
    }

    /// <summary>
    /// Verifies that GetSession returns null when the requested ID is unknown.
    /// </summary>
    [Test]
    public void GetSession_UnknownId_ReturnsNull()
    {
        Assert.That(_service.GetSession("unknown"), Is.Null);
    }

    /// <summary>
    /// Verifies that CreateNewSession generates distinct 32-character
    /// identifier values and stores each session under its generated ID.
    /// </summary>
    [Test]
    public void CreateNewSession_CreatesUniqueThirtyTwoCharacterId()
    {
        var firstId = _service.CreateNewSession("First");
        var secondId = _service.CreateNewSession("Second");

        Assert.Multiple(() =>
        {
            Assert.That(firstId, Is.Not.EqualTo(secondId));
            Assert.That(firstId, Has.Length.EqualTo(32));
            Assert.That(secondId, Has.Length.EqualTo(32));
            Assert.That(Guid.TryParseExact(firstId, "N", out _), Is.True);
            Assert.That(Guid.TryParseExact(secondId, "N", out _), Is.True);
            Assert.That(_service.GetSession(firstId)!.Player.Name, Is.EqualTo("First"));
            Assert.That(_service.GetSession(secondId)!.Player.Name, Is.EqualTo("Second"));
        });
    }

    /// <summary>
    /// Verifies that removing an existing session returns true, deletes it,
    /// and records disposal of its event subscriptions.
    /// </summary>
    [Test]
    public void RemoveSession_ExistingId_RemovesSessionAndReturnsTrue()
    {
        const string sessionId = "remove-me";
        _service.GetOrCreateSession(sessionId);

        var removed = _service.RemoveSession(sessionId);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(_service.GetSession(sessionId), Is.Null);
            Assert.That(LoggerContains("Session removed and event subscriptions disposed for"), Is.True);
        });
    }

    /// <summary>
    /// Verifies that removing an unknown session returns false without writing
    /// a removal log entry.
    /// </summary>
    [Test]
    public void RemoveSession_UnknownId_ReturnsFalse()
    {
        var removed = _service.RemoveSession("unknown");

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.False);
            Assert.That(LogInvocationCount, Is.Zero);
        });
    }

    /// <summary>
    /// Verifies that configured engine events produce structured log messages
    /// containing the expected gameplay events and session scope.
    /// </summary>
    [Test]
    public void ConfiguredEngine_EventsWriteStructuredLogs()
    {
        const string sessionId = "logged-session";
        _mockScoringService
            .Setup(service => service.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { FinalScore = 1, HandType = PokerHandType.HighCard });
        var engine = _service.GetOrCreateSession(sessionId);

        Assert.That(engine.SelectBlind(1), Is.True);
        Assert.That(engine.PlayHand(new List<string> { engine.Hand[0].Id }).Success, Is.True);
        Assert.That(engine.DefeatBlind(), Is.True);

        engine.Money = 100;
        var addedCard = new PlayingCard(Suit.Hearts, Rank.Ace, price: 1);
        engine.Shop.PlayingCardOffers.Add(addedCard);
        Assert.That(engine.BuyCardFromShop(addedCard.Id).Success, Is.True);
        engine.NextRound();
        engine.AdvanceAnte();
        engine.Win();
        engine.GameOver();

        var templates = new[]
        {
            "selected with target", "Hand played with", "Score", "defeated",
            "Cashout collected", "Shop opened", "added to deck",
            "Round advanced", "Ante advanced", "Game won", "Game over"
        };
        Assert.Multiple(() =>
        {
            Assert.That(templates.All(LoggerContains), Is.True);
            Assert.That(SessionScopeInvocationCount, Is.GreaterThan(0));
        });
    }

    /// <summary>
    /// Verifies that removing a session unsubscribes all engine event handlers,
    /// preventing later engine events from producing additional logs.
    /// </summary>
    [Test]
    public void RemoveSession_UnsubscribesAllEngineEventHandlers()
    {
        const string sessionId = "unsubscribe";
        var engine = _service.GetOrCreateSession(sessionId);
        Assert.That(engine.GameOver(), Is.True);
        Assert.That(_service.RemoveSession(sessionId), Is.True);
        var logCountAfterRemoval = LogInvocationCount;

        engine.GameOver();
        engine.NextRound();
        engine.AdvanceAnte();
        engine.Win();

        Assert.That(LogInvocationCount, Is.EqualTo(logCountAfterRemoval));
    }

    /// <summary>
    /// Verifies that the session service remains usable with a null logger and
    /// still supports session creation, retrieval, and removal.
    /// </summary>
    [Test]
    public void SessionService_WithNullLogger_OperatesWithoutExceptions()
    {
        var service = new GameSessionService(
            _mockScoringService.Object,
            _mockShopService.Object,
            _mockConsumableHandler.Object,
            NullLogger<GameSessionService>.Instance,
            NullLoggerFactory.Instance);

        Assert.DoesNotThrow(() =>
        {
            var engine = service.GetOrCreateSession("no-logger");
            engine.SelectBlind(1);
            engine.GameOver();
            Assert.That(service.GetSession("no-logger"), Is.SameAs(engine));
            Assert.That(service.RemoveSession("no-logger"), Is.True);
        });
    }

    /// <summary>
    /// Gets the number of log calls captured by the mocked logger.
    /// </summary>
    private int LogInvocationCount =>
        _mockLogger.Invocations.Count(invocation => invocation.Method.Name == nameof(ILogger.Log));

    /// <summary>
    /// Gets the number of captured logging scopes that include a session ID.
    /// </summary>
    private int SessionScopeInvocationCount =>
        _mockLogger.Invocations.Count(invocation =>
            invocation.Method.Name == "BeginScope" &&
            invocation.Arguments[0] is IEnumerable<KeyValuePair<string, object?>> values &&
            values.Any(pair => pair.Key == "SessionId"));

    /// <summary>
    /// Determines whether any captured log message contains the specified text.
    /// </summary>
    private bool LoggerContains(string text)
    {
        return _mockLogger.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => invocation.Arguments[2].ToString() ?? string.Empty)
            .Any(message => message.Contains(text, StringComparison.Ordinal));
    }
}
