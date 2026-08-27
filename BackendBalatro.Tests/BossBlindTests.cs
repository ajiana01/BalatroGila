using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class BossBlindTests
{
    private readonly GameEngine _engine;
    private readonly ScoringService _scoringService;

    public BossBlindTests()
    {
        var evaluator = new PokerHandEvaluator();
        _scoringService = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _engine = new GameEngine(_scoringService, shopService, consumableHandler);
    }

    private void SetCustomBlind(BlindId blindId, int scoreToDefeat = 600, int reward = 5)
    {
        _engine.StartGame();
        var boss = new Blind(blindId, blindId.ToString(), BlindType.Boss, scoreToDefeat, reward, "Test Boss")
        {
            Id = 3
        };
        _engine.BlindEnemies[_engine.CurrentAnte] = new List<Blind>
        {
            new("Small Blind", BlindType.Small, 300, 3) { Id = 1 },
            new("Big Blind", BlindType.Big, 450, 4) { Id = 2 },
            boss
        };
        _engine.SelectBlind(3);
    }

    [Fact]
    public void TheClub_DebuffsAllClubCards()
    {
        SetCustomBlind(BlindId.TheClub);

        var clubCards = _engine.Hand.Where(c => c.Suit == Suit.Clubs).ToList();
        var nonClubCards = _engine.Hand.Where(c => c.Suit != Suit.Clubs).ToList();

        Assert.All(clubCards, c => Assert.True(c.IsDebuffed));
        Assert.All(nonClubCards, c => Assert.False(c.IsDebuffed));
    }

    [Fact]
    public void TheGoad_DebuffsAllSpadeCards()
    {
        SetCustomBlind(BlindId.TheGoad);

        var spadeCards = _engine.Hand.Where(c => c.Suit == Suit.Spades).ToList();
        var nonSpadeCards = _engine.Hand.Where(c => c.Suit != Suit.Spades).ToList();

        Assert.All(spadeCards, c => Assert.True(c.IsDebuffed));
        Assert.All(nonSpadeCards, c => Assert.False(c.IsDebuffed));
    }

    [Fact]
    public void TheWindow_DebuffsAllDiamondCards()
    {
        SetCustomBlind(BlindId.TheWindow);

        var diamondCards = _engine.Hand.Where(c => c.Suit == Suit.Diamonds).ToList();
        var nonDiamondCards = _engine.Hand.Where(c => c.Suit != Suit.Diamonds).ToList();

        Assert.All(diamondCards, c => Assert.True(c.IsDebuffed));
        Assert.All(nonDiamondCards, c => Assert.False(c.IsDebuffed));
    }

    [Fact]
    public void TheHead_DebuffsAllHeartCards()
    {
        SetCustomBlind(BlindId.TheHead);

        var heartCards = _engine.Hand.Where(c => c.Suit == Suit.Hearts).ToList();
        var nonHeartCards = _engine.Hand.Where(c => c.Suit != Suit.Hearts).ToList();

        Assert.All(heartCards, c => Assert.True(c.IsDebuffed));
        Assert.All(nonHeartCards, c => Assert.False(c.IsDebuffed));
    }

    [Fact]
    public void ThePlant_DebuffsAllFaceCards()
    {
        SetCustomBlind(BlindId.ThePlant);

        var faceCards = _engine.Hand.Where(c => c.Rank is Rank.Jack or Rank.Queen or Rank.King).ToList();
        var nonFaceCards = _engine.Hand.Where(c => c.Rank is not (Rank.Jack or Rank.Queen or Rank.King)).ToList();

        Assert.All(faceCards, c => Assert.True(c.IsDebuffed));
        Assert.All(nonFaceCards, c => Assert.False(c.IsDebuffed));
    }

    [Fact]
    public void ThePsychic_RequiresExactly5Cards()
    {
        SetCustomBlind(BlindId.ThePsychic);

        // Try playing 3 cards -> should fail
        var (success3, message3, _) = _engine.PlayHand(_engine.Hand.Take(3).Select(c => c.Id).ToList());
        Assert.False(success3);
        Assert.Contains("Psychic", message3);

        // Playing 5 cards -> should succeed
        var (success5, _, _) = _engine.PlayHand(_engine.Hand.Take(5).Select(c => c.Id).ToList());
        Assert.True(success5);
    }

    [Fact]
    public void TheNeedle_SetsHandsToOne()
    {
        SetCustomBlind(BlindId.TheNeedle);
        Assert.Equal(1, _engine.HandsRemaining);
    }

    [Fact]
    public void TheWater_SetsDiscardsToZero()
    {
        SetCustomBlind(BlindId.TheWater);
        Assert.Equal(0, _engine.DiscardsRemaining);
    }

    [Fact]
    public void TheManacle_ReducesHandSizeByOne()
    {
        SetCustomBlind(BlindId.TheManacle);
        Assert.Equal(_engine.MaxHand - 1, _engine.Hand.Count);
    }

    [Fact]
    public void TheArm_DecreasesLevelOfPlayedPokerHand()
    {
        _engine.StartGame();
        _engine.PokerHandLevels[PokerHandType.HighCard] = 3;

        SetCustomBlind(BlindId.TheArm);
        _engine.PokerHandLevels[PokerHandType.HighCard] = 3;

        // Force a High Card play
        var singleCard = _engine.Hand.Take(1).Select(c => c.Id).ToList();
        _engine.PlayHand(singleCard);

        Assert.Equal(2, _engine.PokerHandLevels[PokerHandType.HighCard]);
    }

    [Fact]
    public void TheTooth_DeductsMoneyPerCardPlayed()
    {
        SetCustomBlind(BlindId.TheTooth);
        _engine.Money = 10;

        var cards = _engine.Hand.Take(4).Select(c => c.Id).ToList();
        _engine.PlayHand(cards);

        Assert.Equal(6, _engine.Money); // 10 - 4 = 6
    }

    [Fact]
    public void TheFlint_HalvesBaseChipsAndMult()
    {
        var playedCards = new List<PlayingCard>
        {
            new(Suit.Spades, Rank.Ace),
            new(Suit.Hearts, Rank.Ace)
        };
        var remainingInHand = new List<PlayingCard>();
        var jokers = new List<JokerCard>();
        var levels = new Dictionary<PokerHandType, int> { { PokerHandType.Pair, 1 } };

        // Normal scoring for Pair (Base 10 Chips, 2 Mult)
        var normalResult = _scoringService.CalculateScore(playedCards, remainingInHand, jokers, levels);
        Assert.Equal(10, normalResult.BaseChips);
        Assert.Equal(2f, normalResult.BaseMult);

        // Scoring under The Flint (Base 10/2 = 5 Chips, 2/2 = 1 Mult)
        var flintResult = _scoringService.CalculateScore(playedCards, remainingInHand, jokers, levels, BlindId.TheFlint);
        Assert.Equal(5, flintResult.BaseChips);
        Assert.Equal(1f, flintResult.BaseMult);
    }

    [Fact]
    public void TheEye_PreventsRepeatingHandTypesInRound()
    {
        SetCustomBlind(BlindId.TheEye, 100000); // High score so round doesn't end

        // First play: 1 card (High Card)
        var card1 = _engine.Hand.Take(1).Select(c => c.Id).ToList();
        var (s1, _, _) = _engine.PlayHand(card1);
        Assert.True(s1);

        // Second play: 1 card (High Card again) -> rejected
        var card2 = _engine.Hand.Take(1).Select(c => c.Id).ToList();
        var (s2, msg2, _) = _engine.PlayHand(card2);
        Assert.False(s2);
        Assert.Contains("The Eye", msg2);
    }

    [Fact]
    public void TheMouth_AllowsOnlyOneHandTypeInRound()
    {
        SetCustomBlind(BlindId.TheMouth, 100000);

        // Setup hand cards so we can test easily
        _engine.Hand.Clear();
        var c1 = new PlayingCard(Suit.Hearts, Rank.Two);
        var c2 = new PlayingCard(Suit.Diamonds, Rank.Two);
        var c3 = new PlayingCard(Suit.Clubs, Rank.Three);
        var c4 = new PlayingCard(Suit.Spades, Rank.Four);
        _engine.Hand.AddRange(new[] { c1, c2, c3, c4 });

        // Play Pair (c1 + c2)
        var (s1, _, _) = _engine.PlayHand(new List<string> { c1.Id, c2.Id });
        Assert.True(s1);

        // Now playing High Card (c3) should be rejected
        var (s2, msg2, _) = _engine.PlayHand(new List<string> { c3.Id });
        Assert.False(s2);
        Assert.Contains("The Mouth", msg2);
    }

    [Fact]
    public void TheOx_SetsMoneyToZeroWhenPlayingMostPlayedHand()
    {
        SetCustomBlind(BlindId.TheOx, 100000);
        _engine.PokerHandPlayed[PokerHandType.HighCard] = 10;
        _engine.PokerHandPlayed[PokerHandType.Pair] = 2;
        _engine.Money = 50;

        // Play High Card
        _engine.PlayHand(_engine.Hand.Take(1).Select(c => c.Id).ToList());

        Assert.Equal(0, _engine.Money);
    }

    [Fact]
    public void TheHook_DiscardsTwoRandomCardsAfterPlay()
    {
        SetCustomBlind(BlindId.TheHook, 100000);
        int initialHandCount = _engine.Hand.Count;

        // Play 2 cards. With 8 cards, 2 played -> 6 left -> Hook discards 2 -> 4 left -> draw 4 back to 8
        // We can test by checking discard pile count
        int initialDiscardCount = _engine.DiscardPile.Count;
        _engine.PlayHand(_engine.Hand.Take(2).Select(c => c.Id).ToList());

        // 2 played + 2 discarded by hook = 4 cards in discard pile
        Assert.Equal(initialDiscardCount + 4, _engine.DiscardPile.Count);
    }

    [Fact]
    public void VerdantLeaf_DebuffsAllCardsUntilJokerIsSold()
    {
        SetCustomBlind(BlindId.VerdantLeaf);

        // Add a Joker to be sold
        var testJoker = new JokerCard("Test Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 10, 2);
        _engine.Deck.JokerCards.Add(testJoker);

        // All cards should be debuffed
        Assert.All(_engine.Hand, c => Assert.True(c.IsDebuffed));

        // Sell the Joker
        var (sold, _) = _engine.SellCard(testJoker.Id);
        Assert.True(sold);

        // Cards in hand should no longer be debuffed
        Assert.All(_engine.Hand, c => Assert.False(c.IsDebuffed));
    }

    [Fact]
    public void ThePillar_DebuffsCardsPlayedPreviouslyInAnte()
    {
        _engine.StartGame();

        // Ante 1 - Select Small Blind (1)
        _engine.SelectBlind(1);
        var playedCard = _engine.Hand.First();
        var playedCardId = playedCard.Id;

        // Play the card to register it into this Ante's history
        _engine.PlayHand(new List<string> { playedCardId });

        // Now select The Pillar Boss Blind
        var pillar = new Blind(BlindId.ThePillar, "The Pillar", BlindType.Boss, 600, 5, "Debuffs previously played cards");
        _engine.BlindEnemies[_engine.CurrentAnte][2] = pillar;
        _engine.DefeatBlind();
        _engine.LeaveShop();
        _engine.SelectBlind(3);

        // Check if the played card (now recycled into draw/hand) is debuffed
        var matchingCardInHand = _engine.Hand.FirstOrDefault(c => c.Id == playedCardId);
        var matchingCardInDraw = _engine.DrawPile.PlayingCards.FirstOrDefault(c => c.Id == playedCardId);

        if (matchingCardInHand != null) Assert.True(matchingCardInHand.IsDebuffed);
        if (matchingCardInDraw != null) Assert.True(matchingCardInDraw.IsDebuffed);
    }

    [Fact]
    public void BlindsGeneration_AnteEight_GeneratesShowdownBoss()
    {
        _engine.StartGame();
        while (_engine.CurrentAnte < 8)
        {
            _engine.AdvanceAnte();
        }

        var blinds = _engine.GetAvailableBlinds();
        var boss = blinds.First(b => b.BlindType == BlindType.Boss);

        Assert.Contains(boss.BlindId, new[] { BlindId.VioletVessel, BlindId.VerdantLeaf });
        Assert.Equal(8, boss.RewardMoney); // Showdown reward is $8
    }
}
