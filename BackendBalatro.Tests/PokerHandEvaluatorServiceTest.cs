using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Evaluators;
using Microsoft.Extensions.Logging.Abstractions;

namespace BackendBalatro.Tests;

[TestFixture]
public class PokerHandEvaluatorServiceTest
{
    private PokerHandEvaluator _evaluator;

    [SetUp]
    public void SetUp() => _evaluator = new PokerHandEvaluator(NullLogger<PokerHandEvaluator>.Instance);

    [TestCase(true)]
    [TestCase(false)]
    public void Evaluate_NullOrEmptyCards_ReturnsEmptyHighCardResult(bool useNull)
    {
        var result = _evaluator.Evaluate(useNull ? null! : new List<PlayingCard>());

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.HighCard));
            Assert.That(result.ScoringCards, Is.Empty);
            Assert.That(result.UnscoredCards, Is.Empty);
        });
    }

    [Test]
    public void Evaluate_AllStoneCards_ReturnsAllCardsAsScoringHighCard()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Ace, EnhancePokerCard.StoneCards),
            (Suit.Spades, Rank.Two, EnhancePokerCard.StoneCards));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.HighCard));
            Assert.That(result.ScoringCards, Is.EqualTo(cards));
            Assert.That(result.UnscoredCards, Is.Empty);
        });
    }

    [Test]
    public void Evaluate_HighCard_ReturnsHighestStandardCardOnly()
    {
        var cards = Cards((Suit.Hearts, Rank.Four), (Suit.Spades, Rank.Ace), (Suit.Clubs, Rank.Nine));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.HighCard));
            Assert.That(result.ScoringCards, Is.EqualTo(new[] { cards[1] }));
            Assert.That(result.UnscoredCards, Is.EqualTo(new[] { cards[0], cards[2] }));
        });
    }

    [Test]
    public void Evaluate_Pair_ReturnsPairAndKickersUnscored()
    {
        var cards = Cards((Suit.Hearts, Rank.King), (Suit.Spades, Rank.King), (Suit.Clubs, Rank.Two));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.Pair));
            Assert.That(result.ScoringCards, Is.EqualTo(cards.Take(2)));
            Assert.That(result.UnscoredCards, Is.EqualTo(new[] { cards[2] }));
        });
    }

    [Test]
    public void Evaluate_TwoPair_ReturnsTwoHighestPairs()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Ace), (Suit.Spades, Rank.Ace),
            (Suit.Hearts, Rank.King), (Suit.Spades, Rank.King),
            (Suit.Clubs, Rank.Two));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.TwoPair));
            Assert.That(result.ScoringCards, Is.EqualTo(cards.Take(4)));
            Assert.That(result.UnscoredCards, Is.EqualTo(new[] { cards[4] }));
        });
    }

    [Test]
    public void Evaluate_ThreeOfAKind_ReturnsThreeMatchingCards()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Queen), (Suit.Spades, Rank.Queen), (Suit.Clubs, Rank.Queen),
            (Suit.Diamonds, Rank.Two));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.ThreeOfAKind));
            Assert.That(result.ScoringCards, Is.EqualTo(cards.Take(3)));
            Assert.That(result.UnscoredCards, Is.EqualTo(new[] { cards[3] }));
        });
    }

    [Test]
    public void Evaluate_Straight_ReturnsFiveSequentialDistinctRanks()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Nine), (Suit.Spades, Rank.Eight), (Suit.Clubs, Rank.Seven),
            (Suit.Diamonds, Rank.Six), (Suit.Hearts, Rank.Five));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.Straight));
            Assert.That(result.ScoringCards.Select(card => card.Rank),
                Is.EqualTo(new[] { Rank.Nine, Rank.Eight, Rank.Seven, Rank.Six, Rank.Five }));
            Assert.That(result.UnscoredCards, Is.Empty);
        });
    }

    [Test]
    public void Evaluate_AceLowStraight_RecognizesWheel()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Ace), (Suit.Spades, Rank.Two), (Suit.Clubs, Rank.Three),
            (Suit.Diamonds, Rank.Four), (Suit.Hearts, Rank.Five));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.Straight));
            Assert.That(result.ScoringCards.Select(card => card.Rank),
                Is.EqualTo(new[] { Rank.Five, Rank.Four, Rank.Three, Rank.Two, Rank.Ace }));
        });
    }

    [Test]
    public void Evaluate_DuplicateRanksDoNotFormStraight()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Ace), (Suit.Spades, Rank.Ace), (Suit.Clubs, Rank.King),
            (Suit.Diamonds, Rank.King), (Suit.Hearts, Rank.Queen));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.TwoPair));
            Assert.That(result.HandType, Is.Not.EqualTo(PokerHandType.Straight));
            Assert.That(result.HandType, Is.Not.EqualTo(PokerHandType.StraightFlush));
        });
    }

    [Test]
    public void Evaluate_Flush_ReturnsFiveSameSuitCards()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Ace), (Suit.Hearts, Rank.King), (Suit.Hearts, Rank.Nine),
            (Suit.Hearts, Rank.Seven), (Suit.Hearts, Rank.Two));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.Flush));
            Assert.That(result.ScoringCards, Has.Count.EqualTo(5));
            Assert.That(result.ScoringCards.All(card => card.Suit == Suit.Hearts), Is.True);
        });
    }

    [Test]
    public void Evaluate_WildCardsCompleteFlush()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Ace, EnhancePokerCard.None), (Suit.Hearts, Rank.King, EnhancePokerCard.None),
            (Suit.Hearts, Rank.Nine, EnhancePokerCard.None), (Suit.Hearts, Rank.Seven, EnhancePokerCard.None),
            (Suit.Spades, Rank.Two, EnhancePokerCard.WildCards));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.Flush));
            Assert.That(result.ScoringCards, Has.Count.EqualTo(5));
            Assert.That(result.ScoringCards, Does.Contain(cards[4]));
        });
    }

    [Test]
    public void Evaluate_FullHouse_ReturnsTripsAndPair()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Ace), (Suit.Spades, Rank.Ace), (Suit.Clubs, Rank.Ace),
            (Suit.Hearts, Rank.King), (Suit.Spades, Rank.King), (Suit.Clubs, Rank.Two));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.FullHouse));
            Assert.That(result.ScoringCards, Is.EqualTo(cards.Take(5)));
            Assert.That(result.UnscoredCards, Is.EqualTo(new[] { cards[5] }));
        });
    }

    [Test]
    public void Evaluate_FourOfAKind_ReturnsFourMatchingCards()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.Ace), (Suit.Spades, Rank.Ace), (Suit.Clubs, Rank.Ace),
            (Suit.Diamonds, Rank.Ace), (Suit.Hearts, Rank.King));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.FourOfAKind));
            Assert.That(result.ScoringCards, Is.EqualTo(cards.Take(4)));
            Assert.That(result.UnscoredCards, Is.EqualTo(new[] { cards[4] }));
        });
    }

    [Test]
    public void Evaluate_StraightFlush_TakesPrecedenceOverStraightAndFlush()
    {
        var cards = Cards(
            (Suit.Spades, Rank.Nine), (Suit.Spades, Rank.Eight), (Suit.Spades, Rank.Seven),
            (Suit.Spades, Rank.Six), (Suit.Spades, Rank.Five));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.StraightFlush));
            Assert.That(result.ScoringCards, Is.EqualTo(cards));
        });
    }

    [TestCase("straight-flush", PokerHandType.StraightFlush)]
    [TestCase("four-over-full-house", PokerHandType.FourOfAKind)]
    [TestCase("full-house-over-trips", PokerHandType.FullHouse)]
    [TestCase("two-pair-over-pair", PokerHandType.TwoPair)]
    public void Evaluate_CompetingHands_UsesDocumentedPrecedence(string scenario, PokerHandType expectedHandType)
    {
        var cards = scenario switch
        {
            "straight-flush" => Cards(
                (Suit.Hearts, Rank.Nine), (Suit.Hearts, Rank.Eight), (Suit.Hearts, Rank.Seven),
                (Suit.Hearts, Rank.Six), (Suit.Hearts, Rank.Five)),
            "four-over-full-house" => Cards(
                (Suit.Hearts, Rank.Ace), (Suit.Spades, Rank.Ace), (Suit.Clubs, Rank.Ace),
                (Suit.Diamonds, Rank.Ace), (Suit.Hearts, Rank.King), (Suit.Spades, Rank.King)),
            "full-house-over-trips" => Cards(
                (Suit.Hearts, Rank.Ace), (Suit.Spades, Rank.Ace), (Suit.Clubs, Rank.Ace),
                (Suit.Hearts, Rank.King), (Suit.Spades, Rank.King)),
            _ => Cards(
                (Suit.Hearts, Rank.Ace), (Suit.Spades, Rank.Ace), (Suit.Hearts, Rank.King),
                (Suit.Spades, Rank.King), (Suit.Clubs, Rank.Two))
        };

        var result = _evaluator.Evaluate(cards);

        Assert.That(result.HandType, Is.EqualTo(expectedHandType));
    }

    [Test]
    public void Evaluate_StoneCardsAreExcludedFromStandardPatternAndReturnedUnscored()
    {
        var cards = Cards(
            (Suit.Hearts, Rank.King, EnhancePokerCard.None), (Suit.Spades, Rank.King, EnhancePokerCard.None),
            (Suit.Clubs, Rank.King, EnhancePokerCard.StoneCards));

        var result = _evaluator.Evaluate(cards);

        Assert.Multiple(() =>
        {
            Assert.That(result.HandType, Is.EqualTo(PokerHandType.Pair));
            Assert.That(result.ScoringCards, Is.EqualTo(cards.Take(2)));
            Assert.That(result.UnscoredCards, Is.EqualTo(new[] { cards[2] }));
        });
    }

    private static List<PlayingCard> Cards(params (Suit Suit, Rank Rank, EnhancePokerCard Enhancement)[] definitions)
    {
        return definitions.Select(definition =>
            new PlayingCard(definition.Suit, definition.Rank, definition.Enhancement)).ToList();
    }

    private static List<PlayingCard> Cards(params (Suit Suit, Rank Rank)[] definitions)
    {
        return definitions.Select(definition => new PlayingCard(definition.Suit, definition.Rank)).ToList();
    }
}
