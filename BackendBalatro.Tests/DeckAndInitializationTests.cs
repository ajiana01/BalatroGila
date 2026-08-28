using BackendBalatro.Enums;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class DeckAndInitializationTests
{
    private readonly GameController _controller;

    public DeckAndInitializationTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _controller = new GameController(scoring, shopService, consumableHandler);
    }

    [Fact]
    public void StartGame_InitializesStandard52CardDeck()
    {
        // Act
        _controller.StartGame();

        // Assert
        Assert.Equal(52, _controller.DrawPile.Count);
        Assert.Empty(_controller.DiscardPile.PlayingCards);
        Assert.Empty(_controller.Hand);

        // Verify exactly 13 of each suit
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            var count = _controller.DrawPile.PlayingCards.Count(c => c.Suit == suit);
            Assert.Equal(13, count);
        }

        // Verify exactly 4 of each rank
        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            var count = _controller.DrawPile.PlayingCards.Count(c => c.Rank == rank);
            Assert.Equal(4, count);
        }

        // Verify no duplicate cards
        var uniqueCards = _controller.DrawPile.PlayingCards.Select(c => $"{c.Rank}_{c.Suit}").Distinct().Count();
        Assert.Equal(52, uniqueCards);
    }

    [Fact]
    public void StartGame_SetsInitialStateCorrectly()
    {
        // Act
        _controller.StartGame();

        // Assert
        Assert.Equal(GameStatePhase.SelectingBlind, _controller.Phase);
        Assert.Equal(1, _controller.CurrentAnte);
        Assert.Equal(1, _controller.CurrentRound);
        Assert.Equal(4, _controller.Money);
        Assert.Equal(4, _controller.HandsRemaining);
        Assert.Equal(4, _controller.DiscardsRemaining);
        Assert.Equal(8, _controller.MaxHand);
        Assert.Equal(3, _controller.GetAvailableBlinds().Count);
    }
}
