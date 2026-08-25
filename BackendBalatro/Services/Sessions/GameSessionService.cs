using System.Collections.Concurrent;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;

namespace BackendBalatro.Services.Sessions;

public class GameSessionService : IGameSessionService
{
    private readonly ConcurrentDictionary<string, IGameEngine> _sessions = new();
    private readonly IScoringService _scoringService;
    private readonly IShopService _shopService;
    private readonly IConsumableEffectHandler _consumableHandler;

    public GameSessionService(
        IScoringService scoringService,
        IShopService shopService,
        IConsumableEffectHandler consumableHandler)
    {
        _scoringService = scoringService;
        _shopService = shopService;
        _consumableHandler = consumableHandler;
    }

    public IGameEngine GetOrCreateSession(string? sessionId, string? playerName = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = "default";
        }

        return _sessions.GetOrAdd(sessionId, id =>
        {
            var engine = new GameEngine(_scoringService, _shopService, _consumableHandler)
            {
                SessionId = id,
                Player = new Player(1, playerName ?? "Player 1")
            };
            engine.StartGame();
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
        return _sessions.TryRemove(sessionId, out _);
    }

    public string CreateNewSession(string? playerName = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var engine = new GameEngine(_scoringService, _shopService, _consumableHandler)
        {
            SessionId = id,
            Player = new Player(1, playerName ?? "Player 1")
        };
        engine.StartGame();
        _sessions[id] = engine;
        return id;
    }
}
