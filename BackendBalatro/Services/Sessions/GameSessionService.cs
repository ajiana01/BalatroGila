using System.Collections.Concurrent;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Microsoft.Extensions.Logging;

namespace BackendBalatro.Services.Sessions;

public class GameSessionService : IGameSessionService
{
    private readonly ConcurrentDictionary<string, IGameController> _sessions = new();
    private readonly ConcurrentDictionary<string, Action> _sessionCleanups = new();
    private readonly IScoringService _scoringService;
    private readonly IShopService _shopService;
    private readonly IConsumableEffectHandler _consumableHandler;
    private readonly ILogger<GameSessionService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public GameSessionService(
        IScoringService scoringService,
        IShopService shopService,
        IConsumableEffectHandler consumableHandler,
        ILogger<GameSessionService> logger,
        ILoggerFactory loggerFactory)
    {
        _scoringService = scoringService;
        _shopService = shopService;
        _consumableHandler = consumableHandler;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public IGameController GetOrCreateSession(string? sessionId, string? playerName = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = "default";
        }

        if (_sessions.TryGetValue(sessionId, out var existingSession))
        {
            _logger.LogDebug("Game session {SessionId} retrieved", sessionId);
            return existingSession;
        }

        return _sessions.GetOrAdd(sessionId, id =>
        {
            var engine = CreateAndConfigureEngine(id, playerName);
            _logger.LogInformation("Game session {SessionId} created", id);
            return engine;
        });
    }

    public IGameController? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var engine);
        _logger.LogDebug("Game session lookup for {SessionId} found {SessionFound}", sessionId, engine is not null);
        return engine;
    }

    public bool RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            if (_sessionCleanups.TryRemove(sessionId, out var cleanup))
            {
                cleanup.Invoke();
                _logger.LogInformation(
                    "Session removed and event subscriptions disposed for {SessionId}",
                    sessionId);
            }
            return true;
        }
        return false;
    }

    public string CreateNewSession(string? playerName = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var engine = CreateAndConfigureEngine(id, playerName);
        _sessions[id] = engine;
        _logger.LogInformation("Game session {SessionId} created", id);
        return id;
    }

    private IGameController CreateAndConfigureEngine(string sessionId, string? playerName)
    {
        var engine = new GameController(_scoringService, _shopService, _consumableHandler, _loggerFactory.CreateLogger<GameController>())
        {
            SessionId = sessionId,
            Player = new Player(1, playerName ?? "Player 1")
        };

        SubscribeToEngineEvents(engine, sessionId);

        engine.StartGame();
        return engine;
    }

    private void SubscribeToEngineEvents(IGameController controller, string sessionId)
    {
        Action<Blind> onBlindSelected = blind =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation(
                "Blind {BlindId} ({BlindName}) selected with target {TargetScore} and reward {RewardMoney}",
                blind.BlindId,
                blind.Name,
                blind.ScoreToDefeat,
                blind.RewardMoney);
        };

        Action<List<PlayingCard>> onPlayHand = cards =>
        {
            using var scope = BeginSessionScope(sessionId);
            var cardIds = cards.Select(card => card.Id).ToArray();

            _logger.LogInformation(
                "Hand played with {PlayedCardCount} cards {CardIds}",
                cards.Count,
                cardIds);
        };


        Action<int> onScore = score =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation(
                "Score {Score} added; current score {CurrentScore} of target {TargetScore}",
                score,
                controller.CurrentScore,
                controller.CurrentBlind?.ScoreToDefeat ?? 0);
        };

        Action<Blind> onBlindDefeated = blind =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation(
                "Blind {BlindId} ({BlindName}) defeated",
                blind.BlindId,
                blind.Name);
        };

        Action onGetCashout = () =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation("Cashout collected; money is now {Money}", controller.Money);
        };

        Action onShopOpen = () =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation("Shop opened");
        };

        Action<int> onNextRound = round =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation("Round advanced to {Round}", round);
        };

        Action<int> onAnteAdvance = ante =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation("Ante advanced to {Ante}", ante);
        };

        Action<PlayingCard> onAddPlayingCard = card =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation("Playing card {CardId} ({CardName}) added to deck", card.Id, card.Name);
        };

        Action onWinGame = () =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation("Game won");
        };

        Action onGameOver = () =>
        {
            using var scope = BeginSessionScope(sessionId);
            _logger.LogInformation("Game over");
        };

        controller.OnBlindSelected += onBlindSelected;
        controller.OnPlayHand += onPlayHand;
        controller.OnScore += onScore;
        controller.OnBlindDefeated += onBlindDefeated;
        controller.OnGetCashout += onGetCashout;
        controller.OnShopOpen += onShopOpen;
        controller.OnNextRound += onNextRound;
        controller.OnAnteAdvance += onAnteAdvance;
        controller.OnAddPlayingCard += onAddPlayingCard;
        controller.OnWinGame += onWinGame;
        controller.OnGameOver += onGameOver;

        _sessionCleanups[sessionId] = () =>
        {
            controller.OnBlindSelected -= onBlindSelected;
            controller.OnPlayHand -= onPlayHand;
            controller.OnScore -= onScore;
            controller.OnBlindDefeated -= onBlindDefeated;
            controller.OnGetCashout -= onGetCashout;
            controller.OnShopOpen -= onShopOpen;
            controller.OnNextRound -= onNextRound;
            controller.OnAnteAdvance -= onAnteAdvance;
            controller.OnAddPlayingCard -= onAddPlayingCard;
            controller.OnWinGame -= onWinGame;
            controller.OnGameOver -= onGameOver;
        };
    }

    private IDisposable? BeginSessionScope(string sessionId) =>
        _logger.BeginScope(new Dictionary<string, object?>
        {
            ["SessionId"] = sessionId
        });
}
