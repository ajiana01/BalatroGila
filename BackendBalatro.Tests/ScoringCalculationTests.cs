using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Evaluators;
using Xunit;

namespace BackendBalatro.Tests;

public class ScoringCalculationTests
{
    private readonly ScoringService _scoringService;

    public ScoringCalculationTests()
    {
        var evaluator = new PokerHandEvaluator();
        _scoringService = new ScoringService(evaluator);
    }

    [Fact]
    public void CalculateScore_BasicPairScoring()
    {
        var playedCards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ten),
            new(Suit.Diamonds, Rank.Ten),
            new(Suit.Clubs, Rank.Three)
        };
        var remainingInHand = new List<PlayingCard>();
        var jokers = new List<JokerCard>();
        var handLevels = new Dictionary<PokerHandType, int> { { PokerHandType.Pair, 1 } };

        // Pair Base: 10 Chips, 2 Mult
        // 10 of Hearts (10 chips) + 10 of Diamonds (10 chips) = 20 card chips
        // Total chips: 10 + 20 = 30 chips
        // Total mult: 2 mult
        // Final score: 30 * 2 = 60

        var result = _scoringService.CalculateScore(playedCards, remainingInHand, jokers, handLevels);

        Assert.Equal(PokerHandType.Pair, result.HandType);
        Assert.Equal(30, result.TotalChips);
        Assert.Equal(2f, result.TotalMult);
        Assert.Equal(60, result.FinalScore);
    }

    [Fact]
    public void CalculateScore_WithJokersAndEnhancements()
    {
        var playedCards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ace, EnhancePokerCard.BonusCards), // 11 base + 30 bonus = 41 chips
            new(Suit.Diamonds, Rank.Ace, EnhancePokerCard.MultCards) // 11 base chips, +4 mult
        };
        var remainingInHand = new List<PlayingCard>
        {
            new(Suit.Spades, Rank.King, EnhancePokerCard.SteelCards) // Held in hand: X1.5 Mult
        };
        var jokers = new List<JokerCard>
        {
            new("Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 4, 4), // +4 Mult
            new("Polychrome Foil Joker", JokerEdition.Polychrome, JokerRarity.Common, JokerModifierType.Chips, 50, 6) // +50 chips, X1.5 Mult
        };
        var handLevels = new Dictionary<PokerHandType, int> { { PokerHandType.Pair, 1 } };

        // Base Pair: 10 Chips, 2 Mult
        // Card chips: 41 + 11 = 52. Base + card chips = 62. Joker chips: +50 = 112 Total Chips.
        // Card Mult: 2 + 4 = 6. Steel in hand: 6 * 1.5 = 9. Joker Mult: +4 = 13. Joker XMult: * 1.5 = 19.5 Total Mult.
        // Final Score = 112 * 19.5 = 2184

        var result = _scoringService.CalculateScore(playedCards, remainingInHand, jokers, handLevels);

        Assert.Equal(PokerHandType.Pair, result.HandType);
        Assert.Equal(112, result.TotalChips);
        Assert.Equal(19.5f, result.TotalMult);
        Assert.Equal(2184, result.FinalScore);
    }

    [Fact]
    public void CalculateScore_UpgradedHandLevelIncreasesScore()
    {
        var playedCards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Nine)
        };
        var remainingInHand = new List<PlayingCard>();
        var jokers = new List<JokerCard>();
        // Pair Level 3: Base 10 + 2*15 = 40 Chips, Base 2 + 2*1 = 4 Mult
        var handLevels = new Dictionary<PokerHandType, int> { { PokerHandType.Pair, 3 } };

        // Card chips = 9 + 9 = 18 chips
        // Total chips = 40 + 18 = 58
        // Total mult = 4
        // Final score = 58 * 4 = 232

        var result = _scoringService.CalculateScore(playedCards, remainingInHand, jokers, handLevels);

        Assert.Equal(58, result.TotalChips);
        Assert.Equal(4f, result.TotalMult);
        Assert.Equal(232, result.FinalScore);
    }

    [Fact]
    public void CalculateScore_JokerIdEnumTriggers_CalculatesAccurately()
    {
        var playedCards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.King),
            new(Suit.Diamonds, Rank.King)
        };
        var remainingInHand = new List<PlayingCard>
        {
            new(Suit.Clubs, Rank.King) // King in hand for Baron
        };
        var jokers = new List<JokerCard>
        {
            new(JokerId.SmileyFace, "Smiley Face", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4), // 2 face cards scored = +10 Mult
            new(JokerId.Photograph, "Photograph", JokerEdition.Base, JokerRarity.Common, JokerModifierType.MultiplierMultiplier, 1.0f, 5), // First face scored = X2 Mult
            new(JokerId.TheDuo, "The Duo", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 1.0f, 8), // Pair = X2 Mult
            new(JokerId.Baron, "Baron", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 1.0f, 8) // 1 King in hand = X1.5 Mult
        };
        var handLevels = new Dictionary<PokerHandType, int> { { PokerHandType.Pair, 1 } };

        // Base Pair: 10 Chips, 2 Mult
        // Card chips: 10 + 10 = 20. Total chips = 30.
        // Base mult (2) + Smiley Face (+10) = 12 Mult.
        // Joker XMult: Photograph (X2) * TheDuo (X2) * Baron (X1.5) = X6 Mult.
        // Total mult = 12 * 6 = 72 Mult.
        // Final score = 30 * 72 = 2160

        var result = _scoringService.CalculateScore(playedCards, remainingInHand, jokers, handLevels);

        Assert.Equal(30, result.TotalChips);
        Assert.Equal(72f, result.TotalMult);
        Assert.Equal(2160, result.FinalScore);
    }
}
