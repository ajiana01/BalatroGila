using BackendBalatro.Enums;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class GameStateMachineTests
{
    private readonly GameEngine _engine;

    public GameStateMachineTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _engine = new GameEngine(scoring, shopService, consumableHandler);
    }

    [Fact]
    public void StateMachine_FullCycle_SelectToPlayToShopToSelect()
    {
        // 1. Start Game
        _engine.StartGame();
        Assert.Equal(GameStatePhase.SelectingBlind, _engine.Phase);

        // 2. Select Blind
        bool selectResult = _engine.SelectBlind(1);
        Assert.True(selectResult);
        Assert.Equal(GameStatePhase.Playing, _engine.Phase);
        Assert.Equal(8, _engine.Hand.Count);

        // Cannot re-select blind while Playing
        Assert.False(_engine.SelectBlind(2));

        // Cannot buy cards while Playing
        var (buySuccess, _) = _engine.BuyCardFromShop("any-id");
        Assert.False(buySuccess);

        // Cannot leave shop while Playing
        var (leaveSuccess, _) = _engine.LeaveShop();
        Assert.False(leaveSuccess);

        // 3. Play hand to defeat blind
        // Add strong joker to guarantee 1-shot kill
        _engine.Deck.JokerCards.Add(new("Mega Joker", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.Chips, 1000, 10));
        var cardIds = _engine.Hand.Take(5).Select(c => c.Id).ToList();

        var (playSuccess, _, result) = _engine.PlayHand(cardIds);
        Assert.True(playSuccess);
        Assert.NotNull(result);
        Assert.Equal(GameStatePhase.InShop, _engine.Phase);

        // Cannot play hand while InShop
        var (playAgain, _, _) = _engine.PlayHand(_engine.Hand.Take(2).Select(c => c.Id).ToList());
        Assert.False(playAgain);

        // Cannot select blind while InShop
        Assert.False(_engine.SelectBlind(2));

        // 4. Leave Shop
        var (leaveShopSuccess, _) = _engine.LeaveShop();
        Assert.True(leaveShopSuccess);
        Assert.Equal(GameStatePhase.SelectingBlind, _engine.Phase);
    }

    [Fact]
    public void StateMachine_GameOver_WhenHandsExhausted()
    {
        _engine.StartGame();
        _engine.SelectBlind(1); // Target 300

        // Play 1 card at a time with lowest chips to exhaust all 4 hands without reaching 300
        while (_engine.HandsRemaining > 0 && _engine.Phase == GameStatePhase.Playing)
        {
            var singleCard = new List<string> { _engine.Hand[0].Id };
            _engine.PlayHand(singleCard);
        }

        Assert.Equal(GameStatePhase.GameOver, _engine.Phase);
    }
}
