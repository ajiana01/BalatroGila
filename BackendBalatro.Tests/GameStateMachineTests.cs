using BackendBalatro.Enums;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class GameStateMachineTests
{
    private readonly GameController _controller;

    public GameStateMachineTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _controller = new GameController(scoring, shopService, consumableHandler);
    }

    [Fact]
    public void StateMachine_FullCycle_SelectToPlayToShopToSelect()
    {
        // 1. Start Game
        _controller.StartGame();
        Assert.Equal(GameStatePhase.SelectingBlind, _controller.Phase);

        // 2. Select Blind
        bool selectResult = _controller.SelectBlind(1);
        Assert.True(selectResult);
        Assert.Equal(GameStatePhase.Playing, _controller.Phase);
        Assert.Equal(8, _controller.Hand.Count);

        // Cannot re-select blind while Playing
        Assert.False(_controller.SelectBlind(2));

        // Cannot buy cards while Playing
        var (buySuccess, _) = _controller.BuyCardFromShop("any-id");
        Assert.False(buySuccess);

        // Cannot leave shop while Playing
        var (leaveSuccess, _) = _controller.LeaveShop();
        Assert.False(leaveSuccess);

        // 3. Play hand to defeat blind
        // Add strong joker to guarantee 1-shot kill
        _controller.Deck.JokerCards.Add(new("Mega Joker", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.Chips, 1000, 10));
        var cardIds = _controller.Hand.Take(5).Select(c => c.Id).ToList();

        var (playSuccess, _, result) = _controller.PlayHand(cardIds);
        Assert.True(playSuccess);
        Assert.NotNull(result);
        Assert.Equal(GameStatePhase.InShop, _controller.Phase);

        // Cannot play hand while InShop
        var (playAgain, _, _) = _controller.PlayHand(_controller.Hand.Take(2).Select(c => c.Id).ToList());
        Assert.False(playAgain);

        // Cannot select blind while InShop
        Assert.False(_controller.SelectBlind(2));

        // 4. Leave Shop
        var (leaveShopSuccess, _) = _controller.LeaveShop();
        Assert.True(leaveShopSuccess);
        Assert.Equal(GameStatePhase.SelectingBlind, _controller.Phase);
    }

    [Fact]
    public void StateMachine_GameOver_WhenHandsExhausted()
    {
        _controller.StartGame();
        _controller.SelectBlind(1); // Target 300

        // Play 1 card at a time with lowest chips to exhaust all 4 hands without reaching 300
        while (_controller.HandsRemaining > 0 && _controller.Phase == GameStatePhase.Playing)
        {
            var singleCard = new List<string> { _controller.Hand[0].Id };
            _controller.PlayHand(singleCard);
        }

        Assert.Equal(GameStatePhase.GameOver, _controller.Phase);
    }
}
