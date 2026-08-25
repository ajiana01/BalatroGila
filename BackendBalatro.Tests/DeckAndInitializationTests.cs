using BackendBalatro.Enums;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class DeckAndInitializationTests
{
    private readonly GameEngine _engine;

    public DeckAndInitializationTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _engine = new GameEngine(scoring, shopService, consumableHandler);
    }

    [Fact]
    public void StartGame_InitializesStandard52CardDeck()
    {
        // Act
        _engine.StartGame();

        // Assert
        Assert.Equal(52, _engine.DrawPile.Count);
        Assert.Empty(_engine.DiscardPile.PlayingCards);
        Assert.Empty(_engine.Hand);

        // Verify exactly 13 of each suit
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            var count = _engine.DrawPile.PlayingCards.Count(c => c.Suit == suit);
            Assert.Equal(13, count);
        }

        // Verify exactly 4 of each rank
        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            var count = _engine.DrawPile.PlayingCards.Count(c => c.Rank == rank);
            Assert.Equal(4, count);
        }

        // Verify no duplicate cards
        var uniqueCards = _engine.DrawPile.PlayingCards.Select(c => $"{c.Rank}_{c.Suit}").Distinct().Count();
        Assert.Equal(52, uniqueCards);
    }

    [Fact]
    public void StartGame_SetsInitialStateCorrectly()
    {
        // Act
        _engine.StartGame();

        // Assert
        Assert.Equal(GameStatePhase.SelectingBlind, _engine.Phase);
        Assert.Equal(1, _engine.CurrentAnte);
        Assert.Equal(1, _engine.CurrentRound);
        Assert.Equal(4, _engine.Money);
        Assert.Equal(4, _engine.HandsRemaining);
        Assert.Equal(4, _engine.DiscardsRemaining);
        Assert.Equal(8, _engine.MaxHand);
        Assert.Equal(3, _engine.GetAvailableBlinds().Count);
    }
}
