using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Evaluators;
using Xunit;

namespace BackendBalatro.Tests;

public class JokerScoringTests
{
    private readonly ScoringService _scoringService;
    private readonly Dictionary<PokerHandType, int> _defaultLevels;

    public JokerScoringTests()
    {
        var evaluator = new PokerHandEvaluator();
        _scoringService = new ScoringService(evaluator);
        _defaultLevels = new Dictionary<PokerHandType, int>
        {
            { PokerHandType.HighCard, 1 },
            { PokerHandType.Pair, 1 },
            { PokerHandType.TwoPair, 1 },
            { PokerHandType.ThreeOfAKind, 1 },
            { PokerHandType.Straight, 1 },
            { PokerHandType.Flush, 1 },
            { PokerHandType.FullHouse, 1 },
            { PokerHandType.FourOfAKind, 1 },
            { PokerHandType.StraightFlush, 1 }
        };
    }

    private JokerCard CreateJoker(JokerId id, float value = 0, JokerModifierType type = JokerModifierType.AdditionMultiplier)
    {
        return new JokerCard(id, id.ToString(), JokerEdition.Base, JokerRarity.Common, type, value, 4);
    }

    // ==========================================
    // 1. Suit-Based Jokers (+4 Mult per scoring card)
    // ==========================================

    [Fact]
    public void GreedyJoker_Adds4Mult_PerScoringDiamondCard()
    {
        // Pair of Diamonds => Both Diamonds score
        var played = new List<PlayingCard>
        {
            new(Suit.Diamonds, Rank.Ten),
            new(Suit.Diamonds, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.GreedyJoker) };

        // Pair base: 10 chips, 2 mult. 2 Diamonds scored => +8 Mult.
        // Total Mult = 2 (base) + 8 (Greedy) = 10 Mult.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(10f, result.TotalMult);
    }

    [Fact]
    public void LustyJoker_Adds4Mult_PerScoringHeartCard()
    {
        // Pair of Hearts => Both Hearts score
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ten),
            new(Suit.Hearts, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.LustyJoker) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(10f, result.TotalMult);
    }

    [Fact]
    public void WrathfulJoker_Adds4Mult_PerScoringSpadeCard()
    {
        // Pair of Spades => Both Spades score
        var played = new List<PlayingCard>
        {
            new(Suit.Spades, Rank.Ten),
            new(Suit.Spades, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.WrathfulJoker) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(10f, result.TotalMult);
    }

    [Fact]
    public void GluttonousJoker_Adds4Mult_PerScoringClubCard()
    {
        // Pair of Clubs => Both Clubs score
        var played = new List<PlayingCard>
        {
            new(Suit.Clubs, Rank.Ten),
            new(Suit.Clubs, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.GluttonousJoker) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(10f, result.TotalMult);
    }

    [Fact]
    public void SuitJokers_RecognizeWildCards()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ace, EnhancePokerCard.WildCards) // Wild Card counts for any suit
        };
        var jokers = new List<JokerCard>
        {
            CreateJoker(JokerId.GreedyJoker),
            CreateJoker(JokerId.LustyJoker)
        };

        // Wild Card triggers both Greedy (+4) and Lusty (+4) => +8 Mult total
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(1f + 4f + 4f, result.TotalMult);
    }

    // ==========================================
    // 2. Rank / Specific Card Jokers
    // ==========================================

    [Fact]
    public void ScaryFace_Adds30Chips_PerScoringFaceCard()
    {
        // Three of a Kind with Kings => all 3 Kings score
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.King),
            new(Suit.Diamonds, Rank.King),
            new(Suit.Clubs, Rank.King)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.ScaryFace, type: JokerModifierType.Chips) };

        // Three of a Kind base = 30. Card chips = 10 + 10 + 10 = 30. Scary Face (3 * 30) = 90. Total = 150 Chips.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(150, result.TotalChips);
    }

    [Fact]
    public void SmileyFace_Adds5Mult_PerScoringFaceCard()
    {
        // Pair of Queens => both Queens score
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Queen),
            new(Suit.Diamonds, Rank.Queen)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.SmileyFace) };

        // Pair Base Mult (2) + 2 face cards (2 * 5 = +10) => 12 Mult
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(12f, result.TotalMult);
    }

    [Fact]
    public void Photograph_DoublesMult_ForFirstScoringFaceCard()
    {
        // Pair of Jacks => first face card doubles Mult
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Jack),
            new(Suit.Diamonds, Rank.Jack)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.Photograph, 1.0f, JokerModifierType.MultiplierMultiplier) };

        // Pair Base Mult = 2. Photograph doubles to 4.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(4f, result.TotalMult);
    }

    [Fact]
    public void Fibonacci_Adds8Mult_PerFibonacciRank()
    {
        // Straight with Ace, 2, 3, 4, 5 => A, 2, 3, 5 are Fibonacci (4 cards)
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.Two),
            new(Suit.Clubs, Rank.Three),
            new(Suit.Spades, Rank.Four),
            new(Suit.Hearts, Rank.Five)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.Fibonacci) };

        // Straight Base Mult (4) + 4 Fibonacci cards (4 * 8 = +32) => 36 Mult
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(4f + 32f, result.TotalMult);
    }

    [Fact]
    public void EvenSteven_Adds4Mult_PerEvenCard()
    {
        // Two Pair of 2s and 4s => all 4 cards are even
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Diamonds, Rank.Two),
            new(Suit.Clubs, Rank.Four),
            new(Suit.Spades, Rank.Four)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.EvenSteven) };

        // Two Pair Base Mult (2) + 4 Even cards (4 * 4 = +16) => 18 Mult
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(2f + 16f, result.TotalMult);
    }

    [Fact]
    public void OddTodd_Adds31Chips_PerOddCard()
    {
        // Two Pair of 3s and 5s => all 4 cards are odd
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Three),
            new(Suit.Diamonds, Rank.Three),
            new(Suit.Clubs, Rank.Five),
            new(Suit.Spades, Rank.Five)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.OddTodd, type: JokerModifierType.Chips) };

        // Two Pair Base (20) + card chips (3+3+5+5 = 16) + Odd Todd (4 * 31 = 124) => 160 Chips
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(160, result.TotalChips);
    }

    [Fact]
    public void Scholar_Adds20ChipsAnd4Mult_PerScoringAce()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.Ace)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.Scholar) };

        // Pair Base: 10 Chips, 2 Mult.
        // 2 Aces => +40 Chips, +8 Mult.
        // Card chips = 11 + 11 = 22. Total chips = 10 + 22 + 40 = 72. Total Mult = 2 + 8 = 10.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(72, result.TotalChips);
        Assert.Equal(10f, result.TotalMult);
    }

    [Fact]
    public void WalkieTalkie_Adds10ChipsAnd4Mult_PerTenOrFour()
    {
        // Two Pair of 10s and 4s => all 4 cards trigger Walkie Talkie
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ten),
            new(Suit.Diamonds, Rank.Ten),
            new(Suit.Clubs, Rank.Four),
            new(Suit.Spades, Rank.Four)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.WalkieTalkie) };

        // Two Pair Base: 20 Chips, 2 Mult.
        // Card chips = 10 + 10 + 4 + 4 = 28.
        // Walkie Talkie: 4 * 10 = +40 Chips, 4 * 4 = +16 Mult.
        // Total chips = 20 + 28 + 40 = 88. Total Mult = 2 + 16 = 18.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(88, result.TotalChips);
        Assert.Equal(18f, result.TotalMult);
    }

    // ==========================================
    // 3. Hand-State & Hand-Size Jokers
    // ==========================================

    [Fact]
    public void HalfJoker_Adds20Mult_WhenPlayedHandHas3OrFewerCards()
    {
        var playedThreeCards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Seven),
            new(Suit.Diamonds, Rank.Seven)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.HalfJoker) };

        // Played 2 cards (<=3) => +20 Mult
        var result = _scoringService.CalculateScore(playedThreeCards, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(2f + 20f, result.TotalMult);

        // Played 4 cards (>3) => No Half Joker bonus
        var playedFourCards = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Seven),
            new(Suit.Diamonds, Rank.Seven),
            new(Suit.Clubs, Rank.Two),
            new(Suit.Spades, Rank.Three)
        };
        var result2 = _scoringService.CalculateScore(playedFourCards, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(2f, result2.TotalMult);
    }

    [Fact]
    public void RaisedFist_AddsDoubleRankOfLowestCardHeldInHand()
    {
        var played = new List<PlayingCard> { new(Suit.Hearts, Rank.Ten) };
        var remainingInHand = new List<PlayingCard>
        {
            new(Suit.Diamonds, Rank.Three), // Lowest: 3
            new(Suit.Clubs, Rank.King)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.RaisedFist) };

        // Lowest held card is 3 => +6 Mult
        var result = _scoringService.CalculateScore(played, remainingInHand, jokers, _defaultLevels);

        Assert.Equal(1f + 6f, result.TotalMult);
    }

    [Fact]
    public void Baron_MultipliesX1_5Mult_ForEachKingHeldInHand()
    {
        var played = new List<PlayingCard> { new(Suit.Hearts, Rank.Ten) };
        var remainingInHand = new List<PlayingCard>
        {
            new(Suit.Diamonds, Rank.King),
            new(Suit.Clubs, Rank.King)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.Baron, 1.0f, JokerModifierType.MultiplierMultiplier) };

        // 2 Kings held in hand => 1.5 * 1.5 = 2.25 XMult
        var result = _scoringService.CalculateScore(played, remainingInHand, jokers, _defaultLevels);

        Assert.Equal(2.25f, result.TotalMult);
    }

    [Fact]
    public void Blackboard_MultipliesX3Mult_WhenAllHeldCardsAreSpadesOrClubs()
    {
        var played = new List<PlayingCard> { new(Suit.Hearts, Rank.Ten) };
        var allBlackHeld = new List<PlayingCard>
        {
            new(Suit.Spades, Rank.Two),
            new(Suit.Clubs, Rank.Five)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.Blackboard, 1.0f, JokerModifierType.MultiplierMultiplier) };

        // All held cards are black => X3 Mult
        var result1 = _scoringService.CalculateScore(played, allBlackHeld, jokers, _defaultLevels);
        Assert.Equal(3f, result1.TotalMult);

        // Contains Red card (Heart) => No X3 Mult
        var mixedHeld = new List<PlayingCard>
        {
            new(Suit.Spades, Rank.Two),
            new(Suit.Hearts, Rank.Five)
        };
        var result2 = _scoringService.CalculateScore(played, mixedHeld, jokers, _defaultLevels);
        Assert.Equal(1f, result2.TotalMult);
    }

    // ==========================================
    // 4. Hand-Type Additive Mult Jokers (+Mult)
    // ==========================================

    [Fact]
    public void JollyJoker_Adds8Mult_WhenPairPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Nine)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.JollyJoker) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(2f + 8f, result.TotalMult);
    }

    [Fact]
    public void ZanyJoker_Adds12Mult_WhenThreeOfAKindPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Nine),
            new(Suit.Clubs, Rank.Nine)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.ZanyJoker) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(3f + 12f, result.TotalMult);
    }

    [Fact]
    public void MadJoker_Adds10Mult_WhenTwoPairPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Nine),
            new(Suit.Clubs, Rank.Eight),
            new(Suit.Spades, Rank.Eight)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.MadJoker) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(2f + 10f, result.TotalMult);
    }

    [Fact]
    public void CrazyJoker_Adds12Mult_WhenStraightPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Eight),
            new(Suit.Clubs, Rank.Seven),
            new(Suit.Spades, Rank.Six),
            new(Suit.Hearts, Rank.Five)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.CrazyJoker) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(4f + 12f, result.TotalMult);
    }

    [Fact]
    public void DrollJoker_Adds10Mult_WhenFlushPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Hearts, Rank.Five),
            new(Suit.Hearts, Rank.Seven),
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Hearts, Rank.Jack)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.DrollJoker) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(4f + 10f, result.TotalMult);
    }

    // ==========================================
    // 5. Hand-Type Chips Jokers (+Chips)
    // ==========================================

    [Fact]
    public void SlyJoker_Adds50Chips_WhenPairPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Diamonds, Rank.Two)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.SlyJoker, type: JokerModifierType.Chips) };

        // Pair Base: 10 Chips + 4 card chips = 14. Sly Joker adds +50 => 64 Chips.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(64, result.TotalChips);
    }

    [Fact]
    public void WilyJoker_Adds100Chips_WhenThreeOfAKindPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Diamonds, Rank.Two),
            new(Suit.Clubs, Rank.Two)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.WilyJoker, type: JokerModifierType.Chips) };

        // Three of a Kind Base: 30 Chips + 6 card chips = 36. Wily Joker adds +100 => 136 Chips.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(136, result.TotalChips);
    }

    [Fact]
    public void CleverJoker_Adds80Chips_WhenTwoPairPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Diamonds, Rank.Two),
            new(Suit.Clubs, Rank.Three),
            new(Suit.Spades, Rank.Three)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.CleverJoker, type: JokerModifierType.Chips) };

        // Two Pair Base: 20 Chips + 10 card chips = 30. Clever Joker adds +80 => 110 Chips.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(110, result.TotalChips);
    }

    [Fact]
    public void DeviousJoker_Adds100Chips_WhenStraightPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Eight),
            new(Suit.Clubs, Rank.Seven),
            new(Suit.Spades, Rank.Six),
            new(Suit.Hearts, Rank.Five)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.DeviousJoker, type: JokerModifierType.Chips) };

        // Straight Base: 30 Chips + 35 card chips = 65. Devious Joker adds +100 => 165 Chips.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(165, result.TotalChips);
    }

    [Fact]
    public void CraftyJoker_Adds80Chips_WhenFlushPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Hearts, Rank.Four),
            new(Suit.Hearts, Rank.Six),
            new(Suit.Hearts, Rank.Eight),
            new(Suit.Hearts, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.CraftyJoker, type: JokerModifierType.Chips) };

        // Flush Base: 35 Chips + 30 card chips = 65. Crafty Joker adds +80 => 145 Chips.
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(145, result.TotalChips);
    }

    // ==========================================
    // 6. Hand-Type Multiplicative Mult (XMult) Jokers
    // ==========================================

    [Fact]
    public void TheDuo_MultipliesX2Mult_WhenPairPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ten),
            new(Suit.Diamonds, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.TheDuo, 1.0f, JokerModifierType.MultiplierMultiplier) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(2f * 2f, result.TotalMult);
    }

    [Fact]
    public void TheTrio_MultipliesX3Mult_WhenThreeOfAKindPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ten),
            new(Suit.Diamonds, Rank.Ten),
            new(Suit.Clubs, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.TheTrio, 1.0f, JokerModifierType.MultiplierMultiplier) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(3f * 3f, result.TotalMult);
    }

    [Fact]
    public void TheOrder_MultipliesX3Mult_WhenStraightPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Eight),
            new(Suit.Clubs, Rank.Seven),
            new(Suit.Spades, Rank.Six),
            new(Suit.Hearts, Rank.Five)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.TheOrder, 1.0f, JokerModifierType.MultiplierMultiplier) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(4f * 3f, result.TotalMult);
    }

    [Fact]
    public void TheTribe_MultipliesX2Mult_WhenFlushPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Hearts, Rank.Four),
            new(Suit.Hearts, Rank.Six),
            new(Suit.Hearts, Rank.Eight),
            new(Suit.Hearts, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.TheTribe, 1.0f, JokerModifierType.MultiplierMultiplier) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(4f * 2f, result.TotalMult);
    }

    [Fact]
    public void TheFamily_MultipliesX4Mult_WhenFourOfAKindPlayed()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Hearts, Rank.Ten),
            new(Suit.Diamonds, Rank.Ten),
            new(Suit.Clubs, Rank.Ten),
            new(Suit.Spades, Rank.Ten)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.TheFamily, 1.0f, JokerModifierType.MultiplierMultiplier) };

        // Four of a Kind Base Mult: 7 * 4 = 28 Mult
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);
        Assert.Equal(7f * 4f, result.TotalMult);
    }

    // ==========================================
    // 7. Misprint Joker (RNG Mult)
    // ==========================================

    [Fact]
    public void Misprint_AddsBetween0And23Mult()
    {
        var played = new List<PlayingCard> { new(Suit.Hearts, Rank.Ten) };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.Misprint) };

        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        // High Card base is 1. Misprint adds between 0 and 23.
        Assert.InRange(result.TotalMult, 1f, 24f);
    }

    // ==========================================
    // 8. Debuff & Unscored Interactions with Jokers
    // ==========================================

    [Fact]
    public void DebuffedCards_DoNotTrigger_ScaryFaceOrGreedyJoker()
    {
        var played = new List<PlayingCard>
        {
            new(Suit.Diamonds, Rank.King) { IsDebuffed = true }
        };
        var jokers = new List<JokerCard>
        {
            CreateJoker(JokerId.GreedyJoker),
            CreateJoker(JokerId.ScaryFace, type: JokerModifierType.Chips)
        };

        // Debuffed card gives 0 card chips, and does NOT trigger Greedy Joker (+4 Mult) or Scary Face (+30 Chips)
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(5, result.TotalChips); // Only base 5 chips
        Assert.Equal(1f, result.TotalMult); // Only base 1 mult
    }

    [Fact]
    public void UnscoredCards_InHighCard_DoNotTriggerOnScoredJokers()
    {
        // King of Diamonds + 2 of Diamonds in High Card: Only King of Diamonds is scored!
        var played = new List<PlayingCard>
        {
            new(Suit.Diamonds, Rank.King),
            new(Suit.Diamonds, Rank.Two)
        };
        var jokers = new List<JokerCard> { CreateJoker(JokerId.GreedyJoker) };

        // Only 1 Diamond (King) scored => +4 Mult (2 of Diamonds is unscored, does not trigger)
        var result = _scoringService.CalculateScore(played, new List<PlayingCard>(), jokers, _defaultLevels);

        Assert.Equal(1f + 4f, result.TotalMult);
    }
}
