/*
 * ScoringServiceTests.cs - Unit Tests for Score Calculation
 *
 * This file documents the scoring-service contract: hand-level base values,
 * card and joker effects, blind modifiers, and the final score calculation.
 * The evaluator is mocked so these tests focus on score aggregation rather
 * than poker-hand detection.
 *
 * Key testing practices demonstrated:
 * - Arrange-Act-Assert (AAA)
 * - Dependency mocking with Moq
 * - Parameterized tests with [TestCase]
 * - Test names following [Method]_[Scenario]_[ExpectedResult]
 *
 */

using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Evaluators;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackendBalatro.Tests;

/// <summary>
/// Test fixture for <see cref="ScoringService"/>.
///
/// Each test uses a mocked poker-hand evaluator to isolate score calculation
/// from hand detection and keep the scoring scenarios deterministic.
/// </summary>
[TestFixture]
public class ScoringServiceTests
{
    // Mocked dependency that supplies the detected poker hand and its cards.
    private Mock<IPokerHandEvaluator> _evaluator;

    // System under test responsible for combining all score components.
    private ScoringService _service;

    /// <summary>
    /// Runs before every test to create a fresh evaluator mock and scoring service.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _evaluator = new Mock<IPokerHandEvaluator>();
        _service = new ScoringService(_evaluator.Object, NullLogger<ScoringService>.Instance);
    }

    /// <summary>
    /// Verifies that level-one poker hands return their configured base chips
    /// and multiplier values.
    /// </summary>
    [TestCase(PokerHandType.HighCard, 5, 1f)]
    [TestCase(PokerHandType.Pair, 10, 2f)]
    [TestCase(PokerHandType.TwoPair, 20, 2f)]
    [TestCase(PokerHandType.ThreeOfAKind, 30, 3f)]
    [TestCase(PokerHandType.Straight, 30, 4f)]
    [TestCase(PokerHandType.Flush, 35, 4f)]
    [TestCase(PokerHandType.FullHouse, 40, 4f)]
    [TestCase(PokerHandType.FourOfAKind, 60, 7f)]
    [TestCase(PokerHandType.StraightFlush, 100, 8f)]
    public void GetBaseChipsAndMult_LevelOne_ReturnsDefaultMatrix(PokerHandType type, int chips, float mult)
    {
        var result = _service.GetBaseChipsAndMult(type, 1);
        Assert.Multiple(() => { Assert.That(result.BaseChips, Is.EqualTo(chips)); Assert.That(result.BaseMult, Is.EqualTo(mult)); });
    }

    /// <summary>
    /// Verifies that each poker hand returns its configured chips and multiplier
    /// bonus when its level increases.
    /// </summary>
    [TestCase(PokerHandType.HighCard, 10, 1f)]
    [TestCase(PokerHandType.Pair, 15, 1f)]
    [TestCase(PokerHandType.TwoPair, 20, 1f)]
    [TestCase(PokerHandType.ThreeOfAKind, 20, 2f)]
    [TestCase(PokerHandType.Straight, 30, 3f)]
    [TestCase(PokerHandType.Flush, 15, 2f)]
    [TestCase(PokerHandType.FullHouse, 25, 2f)]
    [TestCase(PokerHandType.FourOfAKind, 30, 3f)]
    [TestCase(PokerHandType.StraightFlush, 40, 4f)]
    public void GetLevelUpBonus_ReturnsConfiguredMatrix(PokerHandType type, int chips, float mult)
    {
        var result = _service.GetLevelUpBonus(type);
        Assert.Multiple(() => { Assert.That(result.LevelUpChips, Is.EqualTo(chips)); Assert.That(result.LevelUpMult, Is.EqualTo(mult)); });
    }

    /// <summary>
    /// Verifies that increasing a hand from level one to level three applies
    /// the level-up bonus once for each additional level.
    /// </summary>
    [TestCase(PokerHandType.Pair)]
    [TestCase(PokerHandType.StraightFlush)]
    public void GetBaseChipsAndMult_HigherLevel_AppliesLinearLevelBonus(PokerHandType type)
    {
        var levelOne = _service.GetBaseChipsAndMult(type, 1);
        var bonus = _service.GetLevelUpBonus(type);
        var result = _service.GetBaseChipsAndMult(type, 3);
        Assert.Multiple(() => { Assert.That(result.BaseChips, Is.EqualTo(levelOne.BaseChips + 2 * bonus.LevelUpChips)); Assert.That(result.BaseMult, Is.EqualTo(levelOne.BaseMult + 2 * bonus.LevelUpMult)); });
    }

    /// <summary>
    /// Verifies that hand levels below one are clamped to the level-one values.
    /// </summary>
    [TestCase(0)]
    [TestCase(-3)]
    public void GetBaseChipsAndMult_LevelBelowOne_ClampsToLevelOne(int level)
    {
        Assert.That(_service.GetBaseChipsAndMult(PokerHandType.Flush, level), Is.EqualTo(_service.GetBaseChipsAndMult(PokerHandType.Flush, 1)));
    }

    /// <summary>
    /// Verifies that a basic hand combines base hand values with card chips
    /// to produce the expected totals and final score.
    /// </summary>
    [Test]
    public void CalculateScore_BasicHand_ComposesBaseCardsAndFinalScore()
    {
        var card = Card(Rank.Ace); SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var result = Calculate(new() { card }, levels: new() { [PokerHandType.HighCard] = 2 });
        Assert.Multiple(() =>
        {
            Assert.That(result.HandLevel, Is.EqualTo(2)); Assert.That(result.BaseChips, Is.EqualTo(15)); Assert.That(result.BaseMult, Is.EqualTo(2));
            Assert.That(result.CardChips, Is.EqualTo(11)); Assert.That(result.TotalChips, Is.EqualTo(26)); Assert.That(result.TotalMult, Is.EqualTo(2)); Assert.That(result.FinalScore, Is.EqualTo(52));
            Assert.That(result.ScoringCards, Is.EqualTo(new[] { card })); _evaluator.Verify(e => e.Evaluate(It.IsAny<List<PlayingCard>>()), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that a hand without an entry in the level map defaults to level
    /// one and uses the corresponding base values.
    /// </summary>
    [Test]
    public void CalculateScore_MissingHandLevel_DefaultsToOne()
    {
        var card = Card(Rank.Two); SetEvaluation(PokerHandType.Pair, new() { card }, new());
        var result = Calculate(new() { card }, levels: new());
        Assert.Multiple(() => { Assert.That(result.HandLevel, Is.EqualTo(1)); Assert.That(result.BaseChips, Is.EqualTo(10)); Assert.That(result.BaseMult, Is.EqualTo(2)); });
    }

    /// <summary>
    /// Verifies that a stone card excluded by the evaluator is restored to the
    /// scoring cards and contributes its effective chips.
    /// </summary>
    [Test]
    public void CalculateScore_StoneCardOutsideEvaluatorScoring_IsAddedToScoring()
    {
        var normal = Card(Rank.Two); var stone = Card(Rank.King, EnhancePokerCard.StoneCards);
        SetEvaluation(PokerHandType.HighCard, new() { normal }, new() { stone });
        var result = Calculate(new() { normal, stone });
        Assert.Multiple(() => { Assert.That(result.ScoringCards, Does.Contain(stone)); Assert.That(result.UnscoredCards, Does.Not.Contain(stone)); Assert.That(result.CardChips, Is.EqualTo(normal.GetEffectiveChips() + stone.GetEffectiveChips())); });
    }

    /// <summary>
    /// Verifies that The Flint halves the base chips and multiplier while
    /// enforcing a minimum value of one for each base component.
    /// </summary>
    [Test]
    public void CalculateScore_TheFlint_HalvesBaseValuesWithMinimumOne()
    {
        var card = Card(Rank.Two); SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var result = Calculate(new() { card }, blind: BlindId.TheFlint);
        Assert.Multiple(() => { Assert.That(result.BaseChips, Is.EqualTo(2)); Assert.That(result.BaseMult, Is.EqualTo(1)); Assert.That(result.CardChips, Is.EqualTo(2)); });
    }

    /// <summary>
    /// Verifies that card enhancements and editions contribute their effective
    /// chips, multiplier, and multiplicative multiplier values.
    /// </summary>
    [TestCase(EnhancePokerCard.BonusCards, JokerEdition.Base)]
    [TestCase(EnhancePokerCard.MultCards, JokerEdition.Base)]
    [TestCase(EnhancePokerCard.StoneCards, JokerEdition.Base)]
    [TestCase(EnhancePokerCard.GlassCards, JokerEdition.Base)]
    [TestCase(EnhancePokerCard.None, JokerEdition.Foil)]
    [TestCase(EnhancePokerCard.None, JokerEdition.Holographic)]
    [TestCase(EnhancePokerCard.None, JokerEdition.Polychrome)]
    public void CalculateScore_CardEnhancementsAndEditions_ApplyEffectiveValues(EnhancePokerCard enhancement, JokerEdition edition)
    {
        var card = Card(Rank.Five, enhancement, edition); SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var result = Calculate(new() { card });
        Assert.Multiple(() => { Assert.That(result.CardChips, Is.EqualTo(card.GetEffectiveChips())); Assert.That(result.CardMult, Is.EqualTo(card.GetEffectiveMult())); Assert.That(result.CardXMult, Is.EqualTo(card.GetEffectiveXMult())); Assert.That(result.FinalScore, Is.EqualTo((int)Math.Floor(result.TotalChips * result.TotalMult))); });
    }

    /// <summary>
    /// Verifies that a debuffed scoring card contributes no chips or multiplier
    /// while preserving the default multiplicative multiplier.
    /// </summary>
    [Test]
    public void CalculateScore_DebuffedScoringCardContributesNoChipsOrMult()
    {
        var card = Card(Rank.King); card.IsDebuffed = true; SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var result = Calculate(new() { card }, jokers: new() { Joker(JokerId.ScaryFace) });
        Assert.Multiple(() => { Assert.That(result.CardChips, Is.EqualTo(0)); Assert.That(result.CardMult, Is.EqualTo(0)); Assert.That(result.CardXMult, Is.EqualTo(1)); Assert.That(result.JokerChips, Is.EqualTo(0)); });
    }

    /// <summary>
    /// Verifies that non-debuffed steel cards held in hand multiply the card
    /// multiplier and that debuffed steel cards are ignored.
    /// </summary>
    [Test]
    public void CalculateScore_SteelCardsHeldInHand_MultiplyCardXMult()
    {
        var played = Card(Rank.Two); SetEvaluation(PokerHandType.HighCard, new() { played }, new());
        var debuffedSteel = Card(Rank.Three, EnhancePokerCard.SteelCards); debuffedSteel.IsDebuffed = true;
        var result = Calculate(new() { played }, hand: new() { Card(Rank.Four, EnhancePokerCard.SteelCards), Card(Rank.Five, EnhancePokerCard.SteelCards), debuffedSteel });
        Assert.That(result.CardXMult, Is.EqualTo(2.25f).Within(.0001f));
    }

    /// <summary>
    /// Verifies that foil, holographic, and polychrome joker editions apply
    /// their respective chips, multiplier, or multiplicative multiplier bonus.
    /// </summary>
    [TestCase(JokerEdition.Foil, 50, 0f, 1f)]
    [TestCase(JokerEdition.Holographic, 0, 10f, 1f)]
    [TestCase(JokerEdition.Polychrome, 0, 0f, 1.5f)]
    public void CalculateScore_JokerEdition_AppliesEditionBonus(JokerEdition edition, int chips, float mult, float xmult)
    {
        var card = Card(Rank.Two); SetEvaluation(PokerHandType.HighCard, new() { card }, new()); var joker = Joker(JokerId.Joker, edition);
        var result = Calculate(new() { card }, jokers: new() { joker });
        Assert.Multiple(() => { Assert.That(result.JokerChips, Is.EqualTo(chips)); Assert.That(result.JokerMult, Is.EqualTo(mult)); Assert.That(result.JokerXMult, Is.EqualTo(xmult)); Assert.That(result.JokerTriggers.Single().JokerId, Is.EqualTo(joker.Id)); });
    }

    /// <summary>
    /// Verifies that base joker chips, multiplier, and multiplicative multiplier
    /// are aggregated in joker order and recorded in trigger metadata.
    /// </summary>
    [Test]
    public void CalculateScore_JokerBaseValuesAndOrder_AggregateCorrectly()
    {
        var card = Card(Rank.Two); SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var jokers = new List<JokerCard> { Joker(JokerId.Joker, chips: 10), Joker(JokerId.JollyJoker, mult: 4), Joker(JokerId.Photograph, xmult: 2) };
        var result = Calculate(new() { card }, jokers: jokers);
        Assert.Multiple(() => { Assert.That(result.JokerChips, Is.EqualTo(10)); Assert.That(result.JokerMult, Is.EqualTo(4)); Assert.That(result.JokerXMult, Is.EqualTo(2)); Assert.That(result.JokerTriggers.Select(t => t.JokerIndex), Is.EqualTo(new[] { 0, 1, 2 })); });
    }

    /// <summary>
    /// Verifies that a debuffed lucky card produces neither its multiplier nor
    /// its money proc or trigger message.
    /// </summary>
    [Test]
    public void CalculateScore_LuckyCard_HandlesMultAndMoneyProc()
    {
        var lucky = Card(Rank.Two, EnhancePokerCard.LuckyCards); lucky.IsDebuffed = true; SetEvaluation(PokerHandType.HighCard, new() { lucky }, new());
        var result = Calculate(new() { lucky });
        Assert.Multiple(() => { Assert.That(result.CardMult, Is.EqualTo(0)); Assert.That(result.LuckyMoneyWon, Is.EqualTo(0)); Assert.That(result.JokerTriggerMessages, Is.Empty); });
    }

    /// <summary>
    /// Verifies that face-card jokers apply their configured bonuses when a
    /// matching face card is present and debuffed cards do not qualify.
    /// </summary>
    [TestCase(JokerId.ScaryFace, 30, 0f, 1f)]
    [TestCase(JokerId.SmileyFace, 0, 5f, 1f)]
    [TestCase(JokerId.Photograph, 0, 0f, 2f)]
    public void CalculateScore_FaceCardJokers_ApplyExpectedBonus(JokerId id, int chips, float mult, float xmult)
    {
        var face = Card(Rank.King); var debuffed = Card(Rank.Queen); debuffed.IsDebuffed = true; SetEvaluation(PokerHandType.HighCard, new() { face, debuffed }, new());
        var result = Calculate(new() { face, debuffed }, jokers: new() { Joker(id) });
        Assert.Multiple(() => { Assert.That(result.JokerChips, Is.EqualTo(chips)); Assert.That(result.JokerMult, Is.EqualTo(mult)); Assert.That(result.JokerXMult, Is.EqualTo(xmult)); });
    }

    /// <summary>
    /// Verifies that Half Joker triggers only when three or fewer cards are
    /// played.
    /// </summary>
    [TestCase(3, 20f)]
    [TestCase(4, 0f)]
    public void CalculateScore_HalfJoker_TriggersOnlyForAtMostThreePlayedCards(int count, float expected)
    {
        var cards = Enumerable.Range(0, count).Select(_ => Card(Rank.Two)).ToList(); SetEvaluation(PokerHandType.HighCard, cards, new());
        Assert.That(Calculate(cards, jokers: new() { Joker(JokerId.HalfJoker) }).JokerMult, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that jokers based on held cards use the remaining hand and
    /// apply their expected chips, multiplier, or multiplicative multiplier.
    /// </summary>
    [TestCase(JokerId.RaisedFist, 0f, 6f, 1f)]
    [TestCase(JokerId.Baron, 0f, 0f, 2.25f)]
    [TestCase(JokerId.Blackboard, 0f, 0f, 3f)]
    public void CalculateScore_HeldCardJokers_UseRemainingHand(JokerId id, float chips, float mult, float xmult)
    {
        var played = Card(Rank.Two); SetEvaluation(PokerHandType.HighCard, new() { played }, new());
        var hand = id == JokerId.Blackboard ? new List<PlayingCard> { Card(Rank.Two, suit: Suit.Spades), Card(Rank.Three, suit: Suit.Clubs) } : new List<PlayingCard> { Card(Rank.Three), Card(Rank.King), Card(Rank.King) };
        var result = Calculate(new() { played }, hand: hand, jokers: new() { Joker(id) });
        Assert.Multiple(() => { Assert.That(result.JokerChips, Is.EqualTo(chips)); Assert.That(result.JokerMult, Is.EqualTo(mult)); Assert.That(result.JokerXMult, Is.EqualTo(xmult)); });
    }

    /// <summary>
    /// Verifies that resource-dependent jokers calculate their bonuses from
    /// the supplied money, discards, and deck-size context.
    /// </summary>
    [TestCase(JokerId.Banner, 60, 0f, 0, 2, 0)]
    [TestCase(JokerId.MysticSummit, 0, 15f, 0, 0, 0)]
    [TestCase(JokerId.AbstractJoker, 0, 3f, 0, 1, 0)]
    [TestCase(JokerId.Bull, 10, 0f, 5, 0, 0)]
    [TestCase(JokerId.BlueJoker, 8, 0f, 0, 0, 4)]
    public void CalculateScore_ResourceJokers_UseSuppliedContext(JokerId id, int chips, float mult, int money, int discards, int deck)
    {
        var card = Card(Rank.Two); SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var result = Calculate(new() { card }, jokers: new() { Joker(id) }, money: money, discards: discards, deck: deck);
        Assert.Multiple(() => { Assert.That(result.JokerChips, Is.EqualTo(chips)); Assert.That(result.JokerMult, Is.EqualTo(mult)); });
    }

    /// <summary>
    /// Verifies that suit-based jokers count matching cards and wild cards while
    /// ignoring debuffed cards.
    /// </summary>
    [TestCase(JokerId.GreedyJoker, Suit.Diamonds)]
    [TestCase(JokerId.LustyJoker, Suit.Hearts)]
    [TestCase(JokerId.WrathfulJoker, Suit.Spades)]
    [TestCase(JokerId.GluttonousJoker, Suit.Clubs)]
    public void CalculateScore_SuitJokers_CountMatchingAndWildCards(JokerId id, Suit suit)
    {
        var matching = Card(Rank.Two, suit: suit); var wild = Card(Rank.Three, EnhancePokerCard.WildCards); var bad = Card(Rank.Four, suit: suit); bad.IsDebuffed = true;
        SetEvaluation(PokerHandType.HighCard, new() { matching, wild, bad }, new());
        Assert.That(Calculate(new() { matching, wild, bad }, jokers: new() { Joker(id) }).JokerMult, Is.EqualTo(6));
    }

    /// <summary>
    /// Verifies that rank-based jokers apply their expected bonuses for matching
    /// ranks and ignore debuffed matching cards.
    /// </summary>
    [TestCase(JokerId.Fibonacci, Rank.Five, 0, 8f)]
    [TestCase(JokerId.EvenSteven, Rank.Eight, 0, 4f)]
    [TestCase(JokerId.OddTodd, Rank.Nine, 31, 0f)]
    [TestCase(JokerId.Scholar, Rank.Ace, 20, 4f)]
    [TestCase(JokerId.WalkieTalkie, Rank.Ten, 10, 4f)]
    public void CalculateScore_RankJokers_ApplyExpectedBonuses(JokerId id, Rank rank, int chips, float mult)
    {
        var match = Card(rank); var debuffed = Card(rank); debuffed.IsDebuffed = true; SetEvaluation(PokerHandType.HighCard, new() { match, debuffed }, new());
        var result = Calculate(new() { match, debuffed }, jokers: new() { Joker(id) });
        Assert.Multiple(() => { Assert.That(result.JokerChips, Is.EqualTo(chips)); Assert.That(result.JokerMult, Is.EqualTo(mult)); });
    }

    /// <summary>
    /// Verifies that multiplier jokers trigger when the evaluated hand type is
    /// compatible with each joker's condition.
    /// </summary>
    [TestCase(JokerId.JollyJoker, PokerHandType.FullHouse, 8f)]
    [TestCase(JokerId.ZanyJoker, PokerHandType.FullHouse, 12f)]
    [TestCase(JokerId.MadJoker, PokerHandType.TwoPair, 10f)]
    [TestCase(JokerId.CrazyJoker, PokerHandType.StraightFlush, 12f)]
    [TestCase(JokerId.DrollJoker, PokerHandType.StraightFlush, 10f)]
    public void CalculateScore_HandTypeMultJokers_TriggerForCompatibleHands(JokerId id, PokerHandType type, float expected)
    {
        var card = Card(Rank.Two); SetEvaluation(type, new() { card }, new());
        Assert.That(Calculate(new() { card }, jokers: new() { Joker(id) }).JokerMult, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that chip jokers trigger when the evaluated hand type is
    /// compatible with each joker's condition.
    /// </summary>
    [TestCase(JokerId.SlyJoker, PokerHandType.Pair, 50)]
    [TestCase(JokerId.WilyJoker, PokerHandType.FullHouse, 100)]
    [TestCase(JokerId.CleverJoker, PokerHandType.TwoPair, 80)]
    [TestCase(JokerId.DeviousJoker, PokerHandType.StraightFlush, 100)]
    [TestCase(JokerId.CraftyJoker, PokerHandType.StraightFlush, 80)]
    public void CalculateScore_HandTypeChipJokers_TriggerForCompatibleHands(JokerId id, PokerHandType type, int expected)
    {
        var card = Card(Rank.Two); SetEvaluation(type, new() { card }, new());
        Assert.That(Calculate(new() { card }, jokers: new() { Joker(id) }).JokerChips, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that conditional jokers do not trigger when their required
    /// condition is not met.
    /// </summary>
    [TestCase(JokerId.ScaryFace)]
    [TestCase(JokerId.JollyJoker)]
    [TestCase(JokerId.Banner)]
    public void CalculateScore_ConditionalJokerWithUnmatchedCondition_DoesNotTrigger(JokerId id)
    {
        var card = Card(Rank.Two); SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var result = Calculate(new() { card }, jokers: new() { Joker(id) });
        Assert.That(result.JokerTriggers, Is.Empty);
    }

    /// <summary>
    /// Verifies that Misprint contributes a random multiplier within its valid
    /// range and records a corresponding trigger message.
    /// </summary>
    [Test]
    public void CalculateScore_Misprint_AddsRandomMultWithinZeroToTwentyThree()
    {
        var card = Card(Rank.Two); SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var result = Calculate(new() { card }, jokers: new() { Joker(JokerId.Misprint) });
        Assert.Multiple(() => { Assert.That(result.JokerMult, Is.InRange(0f, 23f)); Assert.That(result.JokerTriggers.Single().Message, Does.Contain("Misprint")); });
    }

    /// <summary>
    /// Verifies that every triggered joker produces matching trigger metadata
    /// and a user-facing trigger message in joker order.
    /// </summary>
    [Test]
    public void CalculateScore_TriggerDtosAndMessages_DescribeEveryTriggeredJoker()
    {
        var card = Card(Rank.King); SetEvaluation(PokerHandType.HighCard, new() { card }, new());
        var jokers = new List<JokerCard> { Joker(JokerId.ScaryFace), Joker(JokerId.SmileyFace), Joker(JokerId.Photograph) };
        var result = Calculate(new() { card }, jokers: jokers);
        Assert.Multiple(() => { Assert.That(result.JokerTriggers, Has.Count.EqualTo(3)); Assert.That(result.JokerTriggers.Select(t => t.JokerId), Is.EqualTo(jokers.Select(j => j.Id))); Assert.That(result.JokerTriggers.Select(t => t.JokerIndex), Is.EqualTo(new[] { 0, 1, 2 })); Assert.That(result.JokerTriggerMessages, Has.Count.EqualTo(3)); });
    }

    private void SetEvaluation(PokerHandType type, List<PlayingCard> scoring, List<PlayingCard> unscored) =>
        _evaluator
            .Setup(e => e.Evaluate(It.IsAny<List<PlayingCard>>()))
            .Returns(new PokerHandEvaluationResult(type, scoring, unscored));
    private ScoreCalculationResultDto Calculate(List<PlayingCard> played, List<PlayingCard>? hand = null, List<JokerCard>? jokers = null, Dictionary<PokerHandType, int>? levels = null, BlindId? blind = null, int money = 0, int discards = 0, int deck = 0) => _service.CalculateScore(played, hand ?? new(), jokers ?? new(), levels ?? new() { [PokerHandType.HighCard] = 1 }, blind, money, discards, deck);
    private static PlayingCard Card(Rank rank, EnhancePokerCard enhancement = EnhancePokerCard.None, JokerEdition edition = JokerEdition.Base, Suit suit = Suit.Hearts) => new(suit, rank, enhancement) { Edition = edition };
    private static JokerCard Joker(JokerId id, JokerEdition edition = JokerEdition.Base, int chips = 0, float mult = 0, float xmult = 1) => new() { JokerId = id, Name = id.ToString(), Edition = edition, ChipsValue = chips, MultValue = mult, XMultValue = xmult };
}
