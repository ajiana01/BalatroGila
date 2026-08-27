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
    private readonly ConcurrentDictionary<string, IGameEngine> _sessions = new();
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

    public IGameEngine GetOrCreateSession(string? sessionId, string? playerName = null)
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

    public IGameEngine? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var engine);
        return engine;
    }

    public bool RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            // Unsubscribe all event listeners to prevent memory leaks
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

    private IGameEngine CreateAndConfigureEngine(string sessionId, string? playerName)
    {
        var engine = new GameEngine(_scoringService, _shopService, _consumableHandler)
        {
            SessionId = sessionId,
            Player = new Player(1, playerName ?? "Player 1")
        };

        // Subscribe to engine lifecycle events (Observer Pattern)
        SubscribeToEngineEvents(engine, sessionId);

        engine.StartGame();
        return engine;
    }

    private void SubscribeToEngineEvents(IGameEngine engine, string sessionId)
    {
        Action<Blind> onBlindSelected = blind =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 🎯 Blind Selected: {BlindName} (Target: {TargetScore}, Reward: ${Reward})", sessionId, blind.Name, blind.ScoreToDefeat, blind.RewardMoney);

        Action<List<PlayingCard>> onPlayHand = cards =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 🃏 Played Hand with {Count} cards: [{Cards}]", sessionId, cards.Count, string.Join(", ", cards.Select(c => c.Name)));

        Action<int> onScore = score =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 📈 Score Added: +{Score} pts | Total Score: {CurrentScore}/{TargetScore}", sessionId, score, engine.CurrentScore, engine.CurrentBlind?.ScoreToDefeat ?? 0);

        Action<Blind> onBlindDefeated = blind =>
            _logger?.LogInformation("[EVENT][Session: {BlindName}] 🏆 Blind Defeated: {BlindName}!", sessionId, blind.Name);

        Action onGetCashout = () =>
            _logger?.LogInformation("[EVENT][Session: {SessionId}] 💰 Cashout Collected! Current Money: ${Money}", sessionId, engine.Money);

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

        // Attach listeners (+=)
        engine.OnBlindSelected += onBlindSelected;
        engine.OnPlayHand += onPlayHand;
        engine.OnScore += onScore;
        engine.OnBlindDefeated += onBlindDefeated;
        engine.OnGetCashout += onGetCashout;
        engine.OnShopOpen += onShopOpen;
        engine.OnNextRound += onNextRound;
        engine.OnAnteAdvance += onAnteAdvance;
        engine.OnAddPlayingCard += onAddPlayingCard;
        engine.OnWinGame += onWinGame;
        engine.OnGameOver += onGameOver;

        // Register cleanup callback for Unsubscribe (-=)
        _sessionCleanups[sessionId] = () =>
        {
            engine.OnBlindSelected -= onBlindSelected;
            engine.OnPlayHand -= onPlayHand;
            engine.OnScore -= onScore;
            engine.OnBlindDefeated -= onBlindDefeated;
            engine.OnGetCashout -= onGetCashout;
            engine.OnShopOpen -= onShopOpen;
            engine.OnNextRound -= onNextRound;
            engine.OnAnteAdvance -= onAnteAdvance;
            engine.OnAddPlayingCard -= onAddPlayingCard;
            engine.OnWinGame -= onWinGame;
            engine.OnGameOver -= onGameOver;
        };
    }
}
