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
    private readonly ILogger<GameSessionService>? _logger;

    public GameSessionService(
        IScoringService scoringService,
        IShopService shopService,
        IConsumableEffectHandler consumableHandler,
        ILogger<GameSessionService>? logger = null)
    {
        _scoringService = scoringService;
        _shopService = shopService;
        _consumableHandler = consumableHandler;
        _logger = logger;
    }

    public IGameController GetOrCreateSession(string? sessionId, string? playerName = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = "default";
        }

        return _sessions.GetOrAdd(sessionId, id =>
        {
            var engine = CreateAndConfigureEngine(id, playerName);
            return engine;
        });
    }

    public IGameController? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var engine);
        return engine;
    }

    public bool RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            if (_sessionCleanups.TryRemove(sessionId, out var cleanup))
            {
                cleanup.Invoke();
                _logger?.LogInformation("[EVENT][Session: {SessionId}] 🧹 Session removed and all event listeners unsubscribed.", sessionId);
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
        return id;
    }

    private IGameController CreateAndConfigureEngine(string sessionId, string? playerName)
    {
        var engine = new GameController(_scoringService, _shopService, _consumableHandler)
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
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 🎯 Blind Selected: {BlindName} (Target: {TargetScore}, Reward: ${Reward})", sessionId, blind.Name, blind.ScoreToDefeat, blind.RewardMoney);

        Action<List<PlayingCard>> onPlayHand = cards =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 🃏 Played Hand with {Count} cards: [{Cards}]", sessionId, cards.Count, string.Join(", ", cards.Select(c => c.Name)));

        Action<int> onScore = score =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 📈 Score Added: +{Score} pts | Total Score: {CurrentScore}/{TargetScore}", sessionId, score, controller.CurrentScore, controller.CurrentBlind?.ScoreToDefeat ?? 0);

        Action<Blind> onBlindDefeated = blind =>
            _logger?.LogInformation("[EVENT][Session: {BlindName}] 🏆 Blind Defeated: {BlindName}!", sessionId, blind.Name);

        Action onGetCashout = () =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 💰 Cashout Collected! Current Money: ${Money}", sessionId, controller.Money);

        Action onShopOpen = () =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 🛒 Shop Opened! Items generated.", sessionId);

        Action<int> onNextRound = round =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 🔄 Round Advanced to Round {Round}", sessionId, round);

        Action<int> onAnteAdvance = ante =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 🚩 Ante Advanced to Ante {Ante}", sessionId, ante);

        Action<PlayingCard> onAddPlayingCard = card =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] ➕ Card Added to Deck: {CardName}", sessionId, card.Name);

        Action onWinGame = () =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 🎉 GAME WON! All 8 Antes completed successfully!", sessionId);

        Action onGameOver = () =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 💀 GAME OVER! Failed to defeat the blind.", sessionId);

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
}
