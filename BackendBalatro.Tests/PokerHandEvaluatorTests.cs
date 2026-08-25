using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Evaluators;
using Xunit;

namespace BackendBalatro.Tests;

public class PokerHandEvaluatorTests
{
    private readonly PokerHandEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_IdentifiesStraightFlush()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Hearts, Rank.Ten),
            new(Suit.Hearts, Rank.Jack),
            new(Suit.Hearts, Rank.Queen),
            new(Suit.Hearts, Rank.King)
        };

        var (handType, scoringCards, _) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.StraightFlush, handType);
        Assert.Equal(5, scoringCards.Count);
    }

    [Fact]
    public void Evaluate_IdentifiesFourOfAKind()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Eight),
            new(Suit.Diamonds, Rank.Eight),
            new(Suit.Clubs, Rank.Eight),
            new(Suit.Spades, Rank.Eight),
            new(Suit.Hearts, Rank.Two)
        };

        var (handType, scoringCards, unscoredCards) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.FourOfAKind, handType);
        Assert.Equal(4, scoringCards.Count);
        Assert.Single(unscoredCards);
    }

    [Fact]
    public void Evaluate_IdentifiesFullHouse()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.King),
            new(Suit.Diamonds, Rank.King),
            new(Suit.Clubs, Rank.King),
            new(Suit.Hearts, Rank.Four),
            new(Suit.Spades, Rank.Four)
        };

        var (handType, scoringCards, _) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.FullHouse, handType);
        Assert.Equal(5, scoringCards.Count);
    }

    [Fact]
    public void Evaluate_IdentifiesFlush()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Spades, Rank.Two),
            new(Suit.Spades, Rank.Five),
            new(Suit.Spades, Rank.Eight),
            new(Suit.Spades, Rank.Jack),
            new(Suit.Spades, Rank.Ace)
        };

        var (handType, scoringCards, _) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.Flush, handType);
        Assert.Equal(5, scoringCards.Count);
    }

    [Fact]
    public void Evaluate_IdentifiesAceLowStraight()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.Two),
            new(Suit.Clubs, Rank.Three),
            new(Suit.Spades, Rank.Four),
            new(Suit.Hearts, Rank.Five)
        };

        var (handType, scoringCards, _) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.Straight, handType);
        Assert.Equal(5, scoringCards.Count);
    }

    [Fact]
    public void Evaluate_IdentifiesThreeOfAKind()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Seven),
            new(Suit.Diamonds, Rank.Seven),
            new(Suit.Clubs, Rank.Seven),
            new(Suit.Hearts, Rank.Two),
            new(Suit.Spades, Rank.Three)
        };

        var (handType, scoringCards, unscoredCards) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.ThreeOfAKind, handType);
        Assert.Equal(3, scoringCards.Count);
        Assert.Equal(2, unscoredCards.Count);
    }

    [Fact]
    public void Evaluate_IdentifiesTwoPair()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Jack),
            new(Suit.Diamonds, Rank.Jack),
            new(Suit.Clubs, Rank.Five),
            new(Suit.Spades, Rank.Five),
            new(Suit.Hearts, Rank.Two)
        };

        var (handType, scoringCards, unscoredCards) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.TwoPair, handType);
        Assert.Equal(4, scoringCards.Count);
        Assert.Single(unscoredCards);
    }

    [Fact]
    public void Evaluate_IdentifiesPair()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ten),
            new(Suit.Diamonds, Rank.Ten),
            new(Suit.Clubs, Rank.Three)
        };

        var (handType, scoringCards, unscoredCards) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.Pair, handType);
        Assert.Equal(2, scoringCards.Count);
        Assert.Single(unscoredCards);
    }

    [Fact]
    public void Evaluate_IdentifiesHighCard()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.King),
            new(Suit.Diamonds, Rank.Eight),
            new(Suit.Clubs, Rank.Three)
        };

        var (handType, scoringCards, unscoredCards) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.HighCard, handType);
        Assert.Single(scoringCards);
        Assert.Equal(Rank.King, scoringCards[0].Rank);
        Assert.Equal(2, unscoredCards.Count);
    }

    [Fact]
    public void Evaluate_RecognizesWildCardsForFlush()
    {
        var cards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Hearts, Rank.Five),
            new(Suit.Hearts, Rank.Eight),
            new(Suit.Hearts, Rank.Jack),
            new(Suit.Clubs, Rank.Ace, EnhancePokerCard.WildCards) // Wild Card counts as Heart
        };

        var (handType, scoringCards, _) = _evaluator.Evaluate(cards);

        Assert.Equal(PokerHandType.Flush, handType);
        Assert.Equal(5, scoringCards.Count);
    }
}
