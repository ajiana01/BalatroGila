using BackendBalatro.Enums;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class WinConditionAndAnteProgressionTests
{
    private readonly GameController _controller;

    public WinConditionAndAnteProgressionTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _controller = new GameController(scoring, shopService, consumableHandler);
    }

    [Fact]
    public void AnteProgression_BossDefeatedOnAnte1_AdvancesToAnte2()
    {
        _controller.StartGame();
        Assert.Equal(1, _controller.CurrentAnte);

        // Select and defeat Boss Blind (Id = 3)
        _controller.SelectBlind(3);
        _controller.Deck.JokerCards.Add(new("God Joker", JokerEdition.Base, JokerRarity.Legendary, JokerModifierType.Chips, 100000, 10));

        var (playSuccess, _, _) = _controller.PlayHand(_controller.Hand.Take(5).Select(c => c.Id).ToList());
        Assert.True(playSuccess);
        Assert.Equal(GameStatePhase.InShop, _controller.Phase);

        // Leave Shop
        var (leaveSuccess, _) = _controller.LeaveShop();
        Assert.True(leaveSuccess);
        Assert.Equal(GameStatePhase.SelectingBlind, _controller.Phase);
        Assert.Equal(2, _controller.CurrentAnte);
    }

    [Fact]
    public void WinCondition_DefeatingBossOnAnte8_TriggersVictory()
    {
        _controller.StartGame();

        // Simulate reaching Ante 8
        while (_controller.CurrentAnte < 8)
        {
            _controller.AdvanceAnte();
        }
        Assert.Equal(8, _controller.CurrentAnte);

        // Select Ante 8 Boss Blind (Id = 3)
        bool selected = _controller.SelectBlind(3);
        Assert.True(selected);
        Assert.Equal(BlindType.Boss, _controller.CurrentBlind!.BlindType);

        // Defeat Boss Blind with massive score
        _controller.Deck.JokerCards.Add(new("God Joker", JokerEdition.Base, JokerRarity.Legendary, JokerModifierType.Chips, 500000, 10));
        var (playSuccess, _, _) = _controller.PlayHand(_controller.Hand.Take(5).Select(c => c.Id).ToList());
        Assert.True(playSuccess);
        Assert.Equal(GameStatePhase.InShop, _controller.Phase);

        // Leave Shop after Ante 8 Boss -> Victory!
        var (leaveSuccess, message) = _controller.LeaveShop();
        Assert.True(leaveSuccess);
        Assert.Equal(GameStatePhase.Victory, _controller.Phase);
        Assert.Contains("Victory", message);
    }
}
