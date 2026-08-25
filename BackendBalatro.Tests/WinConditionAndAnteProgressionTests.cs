using BackendBalatro.Enums;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class WinConditionAndAnteProgressionTests
{
    private readonly GameEngine _engine;

    public WinConditionAndAnteProgressionTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _engine = new GameEngine(scoring, shopService, consumableHandler);
    }

    [Fact]
    public void AnteProgression_BossDefeatedOnAnte1_AdvancesToAnte2()
    {
        _engine.StartGame();
        Assert.Equal(1, _engine.CurrentAnte);

        // Select and defeat Boss Blind (Id = 3)
        _engine.SelectBlind(3);
        _engine.Deck.JokerCards.Add(new("God Joker", JokerEdition.Base, JokerRarity.Legendary, JokerModifierType.Chips, 100000, 10));

        var (playSuccess, _, _) = _engine.PlayHand(_engine.Hand.Take(5).Select(c => c.Id).ToList());
        Assert.True(playSuccess);
        Assert.Equal(GameStatePhase.InShop, _engine.Phase);

        // Leave Shop
        var (leaveSuccess, _) = _engine.LeaveShop();
        Assert.True(leaveSuccess);
        Assert.Equal(GameStatePhase.SelectingBlind, _engine.Phase);
        Assert.Equal(2, _engine.CurrentAnte);
    }

    [Fact]
    public void WinCondition_DefeatingBossOnAnte8_TriggersVictory()
    {
        _engine.StartGame();

        // Simulate reaching Ante 8
        while (_engine.CurrentAnte < 8)
        {
            _engine.AdvanceAnte();
        }
        Assert.Equal(8, _engine.CurrentAnte);

        // Select Ante 8 Boss Blind (Id = 3)
        bool selected = _engine.SelectBlind(3);
        Assert.True(selected);
        Assert.Equal(BlindType.Boss, _engine.CurrentBlind!.BlindType);

        // Defeat Boss Blind with massive score
        _engine.Deck.JokerCards.Add(new("God Joker", JokerEdition.Base, JokerRarity.Legendary, JokerModifierType.Chips, 500000, 10));
        var (playSuccess, _, _) = _engine.PlayHand(_engine.Hand.Take(5).Select(c => c.Id).ToList());
        Assert.True(playSuccess);
        Assert.Equal(GameStatePhase.InShop, _engine.Phase);

        // Leave Shop after Ante 8 Boss -> Victory!
        var (leaveSuccess, message) = _engine.LeaveShop();
        Assert.True(leaveSuccess);
        Assert.Equal(GameStatePhase.Victory, _engine.Phase);
        Assert.Contains("Victory", message);
    }
}
