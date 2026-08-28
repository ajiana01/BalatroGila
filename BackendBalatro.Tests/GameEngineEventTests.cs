using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Sessions;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class GameEngineEventTests
{
    [Fact]
    public void GameEngine_Events_TriggerCorrectlyWhenActionsPerformed()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();

        var engine = new GameController(scoring, shopService, consumableHandler);
        engine.StartGame();

        bool blindSelectedFired = false;
        bool playHandFired = false;
        bool scoreFired = false;
        int recordedScore = 0;
        bool nextRoundFired = false;
        bool anteAdvanceFired = false;
        bool winGameFired = false;
        bool gameOverFired = false;

        engine.OnBlindSelected += blind => { blindSelectedFired = true; };
        engine.OnPlayHand += cards => { playHandFired = true; };
        engine.OnScore += score => { scoreFired = true; recordedScore = score; };
        engine.OnNextRound += round => { nextRoundFired = true; };
        engine.OnAnteAdvance += ante => { anteAdvanceFired = true; };
        engine.OnWinGame += () => { winGameFired = true; };
        engine.OnGameOver += () => { gameOverFired = true; };

        // 1. Select Blind
        engine.SelectBlind(1);
        Assert.True(blindSelectedFired);

        // 2. Play Hand
        var cardsToPlay = engine.Hand.Take(5).Select(c => c.Id).ToList();
        var playResult = engine.PlayHand(cardsToPlay);
        Assert.True(playHandFired);
        Assert.True(scoreFired);
        Assert.True(recordedScore > 0);

        // 3. Next Round & Ante Advance
        engine.NextRound();
        Assert.True(nextRoundFired);

        engine.AdvanceAnte();
        Assert.True(anteAdvanceFired);

        // 4. Win & Game Over
        engine.Win();
        Assert.True(winGameFired);

        engine.GameOver();
        Assert.True(gameOverFired);
    }

    [Fact]
    public void GameSessionService_RemoveSession_CleansUpAndUnsubscribesEvents()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();

        var sessionService = new GameSessionService(scoring, shopService, consumableHandler);
        string sessionId = "test-session-cleanup";

        var engine = sessionService.GetOrCreateSession(sessionId);
        Assert.NotNull(engine);

        // Remove the session which triggers cleanup (Unsubscribe -=)
        bool removed = sessionService.RemoveSession(sessionId);
        Assert.True(removed);

        // Verify session is no longer accessible
        var retrieved = sessionService.GetSession(sessionId);
        Assert.Null(retrieved);
    }
}
