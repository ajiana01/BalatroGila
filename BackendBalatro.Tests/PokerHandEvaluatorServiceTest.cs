/*
 * PokerHandEvaluatorServiceTest.cs - Unit Tests for Poker-Hand Evaluation
 *
 * This fixture documents the evaluator contract: hand classification,
 * scoring-card selection, unscored-card handling, special enhancements, and
 * precedence when multiple poker hands can be formed from the same cards.
 *
 * Key testing practices demonstrated:
 * - Arrange-Act-Assert (AAA)
 * - Parameterized tests for related hand scenarios
 * - Assertions for both hand type and participating cards
 * - Small test-data helpers for readable card definitions
 *
 */

using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Evaluators;
using Microsoft.Extensions.Logging.Abstractions;

namespace BackendBalatro.Tests;

/// <summary>
/// Test fixture for <see cref="PokerHandEvaluator"/>.
///
/// Each test uses a fresh evaluator with a null logger because the behavior
/// under test is limited to poker-hand detection and card classification.
/// </summary>
[TestFixture]
public class PokerHandEvaluatorServiceTest
{
    // System under test: classifies cards into a poker hand and scoring groups.
    private PokerHandEvaluator _evaluator;

    /// <summary>
    /// Creates a fresh evaluator before each test to keep evaluator state
    /// isolated between scenarios.
    /// </summary>
    [SetUp]
    public void SetUp() => _evaluator = new PokerHandEvaluator(NullLogger<PokerHandEvaluator>.Instance);

    /// <summary>
    /// Verifies that null or empty input is treated as an empty High Card hand
    /// with no scoring or unscored cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that Stone cards are returned as scoring cards when the hand
    /// contains only Stone enhancements.
    /// </summary>
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

    /// <summary>
    /// Verifies that a standard High Card hand scores only its highest card and
    /// marks the remaining cards as unscored.
    /// </summary>
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

    /// <summary>
    /// Verifies that a Pair is classified correctly and its kicker remains
    /// outside the scoring cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that the two highest matching pairs are selected as a Two Pair
    /// hand while the remaining card is unscored.
    /// </summary>
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

    /// <summary>
    /// Verifies that three cards of the same rank form a Three of a Kind and
    /// that the unrelated card is unscored.
    /// </summary>
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

    /// <summary>
    /// Verifies that five sequential cards with distinct ranks form a Straight
    /// and all participate in scoring.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Ace-low sequence A-2-3-4-5 is recognized as a
    /// Straight with the expected scoring-card order.
    /// </summary>
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

    /// <summary>
    /// Verifies that duplicate ranks prevent a Straight and that the evaluator
    /// correctly classifies the cards as Two Pair instead.
    /// </summary>
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

    /// <summary>
    /// Verifies that five cards of the same suit form a Flush and all five are
    /// included in the scoring cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that a Wild card can complete a Flush when the other four cards
    /// share a suit.
    /// </summary>
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

    /// <summary>
    /// Verifies that three matching cards and a pair form a Full House while a
    /// sixth unrelated card remains unscored.
    /// </summary>
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

    /// <summary>
    /// Verifies that four cards of the same rank form a Four of a Kind and the
    /// kicker is excluded from scoring.
    /// </summary>
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

    /// <summary>
    /// Verifies that a Straight Flush is detected and takes precedence over
    /// the separate Straight and Flush patterns.
    /// </summary>
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

    /// <summary>
    /// Verifies that competing poker-hand patterns are resolved according to
    /// the evaluator's documented hand precedence.
    /// </summary>
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

    /// <summary>
    /// Verifies that Stone cards are excluded from standard pattern detection
    /// and returned as unscored cards.
    /// </summary>
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

    /// <summary>
    /// Creates playing cards from suit, rank, and enhancement definitions.
    /// </summary>
    private static List<PlayingCard> Cards(params (Suit Suit, Rank Rank, EnhancePokerCard Enhancement)[] definitions)
    {
        return definitions.Select(definition =>
            new PlayingCard(definition.Suit, definition.Rank, definition.Enhancement)).ToList();
    }

    /// <summary>
    /// Creates standard playing cards from suit and rank definitions.
    /// </summary>
    private static List<PlayingCard> Cards(params (Suit Suit, Rank Rank)[] definitions)
    {
        return definitions.Select(definition => new PlayingCard(definition.Suit, definition.Rank)).ToList();
    }
}
