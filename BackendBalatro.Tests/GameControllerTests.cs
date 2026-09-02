/*
 * GameControllerTests.cs - Unit Tests for Core Game Orchestration
 *
 * This file documents the game-controller contract: game lifecycle, blind
 * selection, hand play and scoring, shop interactions, consumables, inventory,
 * boss-blind rules, card drawing, and end-of-round progression.
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
using BackendBalatro.Models.Interfaces;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;

namespace BackendBalatro.Tests;

/// <summary>
/// Test fixture for <see cref="GameController"/>.
///
/// Each test uses mocked scoring, shop, and consumable services to isolate the
/// controller's game-state transitions and orchestration rules.
/// </summary>
[TestFixture]
public class GameControllerTests
{
    // System under test containing the current game session state.
    private GameController _gameController;

    // Mocked dependency used to calculate hand scores and previews.
    private Mock<IScoringService> _mockScoringService;

    // Mocked dependency used to generate and operate the shop.
    private Mock<IShopService> _mockShopService;

    // Mocked dependency used to apply consumable-card effects.
    private Mock<IConsumableEffectHandler> _mockConsumableHandler;

    /// <summary>
    /// Runs before every test to create fresh service mocks and a game controller.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockScoringService = new Mock<IScoringService>();
        _mockShopService = new Mock<IShopService>();
        _mockConsumableHandler = new Mock<IConsumableEffectHandler>();

        _gameController = new GameController(
            _mockScoringService.Object,
            _mockShopService.Object,
            _mockConsumableHandler.Object,
            NullLogger<GameController>.Instance);
    }

    #region Game Lifecycle Tests

    /// <summary>
    /// Verifies that StartGame initializes the default state and a standard 52-card deck for a new game.
    /// </summary>
    [Test]
    public void StartGame_NewGame_InitializesDefaultStateAnd52CardDeck()
    {
        var fakeVoucher = new Voucher("Overstock", VoucherEffect.Overstock, 10, "Extra shop slot");
        _mockShopService
            .Setup(s => s.GenerateVoucherForAnte(1, It.IsAny<List<Voucher>>()))
            .Returns(fakeVoucher);

        var result = _gameController.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "StartGame should return true on success.");
            Assert.That(_gameController.CurrentAnte, Is.EqualTo(1), "Initial Ante must be 1.");
            Assert.That(_gameController.CurrentRound, Is.EqualTo(1), "Initial Round must be 1.");
            Assert.That(_gameController.Money, Is.EqualTo(4), "Initial Money must be $4.");
            Assert.That(_gameController.MaxHands, Is.EqualTo(4), "Initial MaxHands must be 4.");
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(4), "Initial HandsRemaining must be 4.");
            Assert.That(_gameController.MaxDiscards, Is.EqualTo(4), "Initial MaxDiscards must be 4.");
            Assert.That(_gameController.DiscardsRemaining, Is.EqualTo(4), "Initial DiscardsRemaining must be 4.");
            Assert.That(_gameController.MaxHand, Is.EqualTo(8), "Initial MaxHand size must be 8.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.SelectingBlind), "Initial phase must be SelectingBlind.");
            Assert.That(_gameController.DrawPile.Count, Is.EqualTo(52), "Draw pile must contain standard 52 playing cards.");
            Assert.That(_gameController.Hand, Is.Empty, "Hand must be empty before selecting a blind.");
            Assert.That(_gameController.DiscardPile.Count, Is.EqualTo(0), "Discard pile must be empty initially.");
            Assert.That(_gameController.Deck.JokerCards, Is.Empty, "Joker cards must be empty at start.");
            Assert.That(_gameController.Deck.UsableCards, Is.Empty, "Consumable cards must be empty at start.");
            Assert.That(_gameController.PurchasedVouchers, Is.Empty, "Purchased vouchers must be empty at start.");
            Assert.That(_gameController.CurrentAnteVoucher, Is.EqualTo(fakeVoucher), "Current Ante voucher should be set from shop service.");
            Assert.That(_gameController.BlindEnemies.ContainsKey(1), Is.True, "Blind enemies for Ante 1 must be generated.");
            Assert.That(_gameController.BlindEnemies[1], Has.Count.EqualTo(3), "Ante 1 must generate 3 blinds (Small, Big, Boss).");
        });

        _mockShopService.Verify(
            s => s.GenerateVoucherForAnte(1, It.IsAny<List<Voucher>>()),
            Times.Once,
            "ShopService should be called once to generate voucher for Ante 1.");
    }

    /// <summary>
    /// Verifies that StartGame initializes every poker-hand level to one and every played count to zero.
    /// </summary>
    [Test]
    public void StartGame_PokerHands_InitializesAllLevelsToOneAndPlayedCountToZero()
    {
        var allHandTypes = Enum.GetValues<PokerHandType>();

        _gameController.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(_gameController.PokerHandLevels, Has.Count.EqualTo(allHandTypes.Length),
                "PokerHandLevels dictionary must contain all poker hand types.");
            Assert.That(_gameController.PokerHandPlayed, Has.Count.EqualTo(allHandTypes.Length),
                "PokerHandPlayed dictionary must contain all poker hand types.");

            foreach (var handType in allHandTypes)
            {
                Assert.That(_gameController.PokerHandLevels.ContainsKey(handType), Is.True,
                    $"PokerHandLevels must contain key {handType}.");
                Assert.That(_gameController.PokerHandLevels[handType], Is.EqualTo(1),
                    $"Initial level for {handType} must be 1.");

                Assert.That(_gameController.PokerHandPlayed.ContainsKey(handType), Is.True,
                    $"PokerHandPlayed must contain key {handType}.");
                Assert.That(_gameController.PokerHandPlayed[handType], Is.EqualTo(0),
                    $"Initial played count for {handType} must be 0.");
            }
        });
    }

    /// <summary>
    /// Verifies that StartGame clears existing hand, discard, inventory, and voucher state.
    /// </summary>
    [Test]
    public void StartGame_ExistingState_CleansUpHandDiscardAndVouchers()
    {
        _gameController.Hand.Add(new PlayingCard(Suit.Hearts, Rank.Ace, EnhancePokerCard.None, 1));
        _gameController.DiscardPile.DiscardCards(new List<PlayingCard>
        {
            new PlayingCard(Suit.Clubs, Rank.King, EnhancePokerCard.None, 1)
        });
        _gameController.Deck.JokerCards.Add(new JokerCard("Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 4f, 2));
        _gameController.Deck.UsableCards.Add(new TarotCard("The Fool", 3, TarotType.TheFool));
        _gameController.PurchasedVouchers.Add(new Voucher("Overstock", VoucherEffect.Overstock, 10));
        _gameController.LastTarotUsed = new TarotCard("The Magician", 3, TarotType.TheMagician);
        _gameController.LastPlanetUsed = PlanetCard.CreateForHand(PokerHandType.HighCard);

        var newVoucher = new Voucher("Grabber", VoucherEffect.Grabber, 10);
        _mockShopService
            .Setup(s => s.GenerateVoucherForAnte(1, It.IsAny<List<Voucher>>()))
            .Returns(newVoucher);

        var result = _gameController.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "StartGame should return true.");
            Assert.That(_gameController.Hand, Is.Empty, "Hand must be cleared on restart.");
            Assert.That(_gameController.DiscardPile.Count, Is.EqualTo(0), "DiscardPile must be cleared on restart.");
            Assert.That(_gameController.Deck.JokerCards, Is.Empty, "Joker cards must be cleared on restart.");
            Assert.That(_gameController.Deck.UsableCards, Is.Empty, "Usable cards must be cleared on restart.");
            Assert.That(_gameController.PurchasedVouchers, Is.Empty, "Purchased vouchers must be cleared on restart.");
            Assert.That(_gameController.LastTarotUsed, Is.Null, "LastTarotUsed must be reset to null.");
            Assert.That(_gameController.LastPlanetUsed, Is.Null, "LastPlanetUsed must be reset to null.");
            Assert.That(_gameController.IsAnteVoucherPurchased, Is.False, "IsAnteVoucherPurchased must be reset to false.");
            Assert.That(_gameController.IsBossBlindRerolledThisAnte, Is.False, "IsBossBlindRerolledThisAnte must be reset to false.");
            Assert.That(_gameController.DrawPile.Count, Is.EqualTo(52), "DrawPile must be reset with fresh 52 cards.");
        });
    }

    /// <summary>
    /// Verifies that Win sets the phase to Victory and raises the OnWinGame event.
    /// </summary>
    [Test]
    public void Win_WhenInvoked_SetsPhaseToVictoryAndFiresOnWinGameEvent()
    {
        bool eventFired = false;
        _gameController.OnWinGame += () => eventFired = true;

        var result = _gameController.Win();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Win should return true.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Victory), "Phase must transition to GameStatePhase.Victory.");
            Assert.That(eventFired, Is.True, "OnWinGame event must be fired.");
        });
    }

    /// <summary>
    /// Verifies that GameOver sets the phase to GameOver and raises the OnGameOver event.
    /// </summary>
    [Test]
    public void GameOver_WhenInvoked_SetsPhaseToGameOverAndFiresOnGameOverEvent()
    {
        bool eventFired = false;
        _gameController.OnGameOver += () => eventFired = true;

        var result = _gameController.GameOver();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "GameOver should return true.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.GameOver), "Phase must transition to GameStatePhase.GameOver.");
            Assert.That(eventFired, Is.True, "OnGameOver event must be fired.");
        });
    }

    /// <summary>
    /// Verifies that AdvanceAnte increments the ante, resets ante-specific state, and generates new blinds.
    /// </summary>
    [Test]
    public void AdvanceAnte_NextAnte_IncrementsAnteResetsDebuffTrackerAndGeneratesNewBlinds()
    {
        _gameController.StartGame();

        int eventAnte = 0;
        _gameController.OnAnteAdvance += ante => eventAnte = ante;

        var ante2Voucher = new Voucher("ClearanceSale", VoucherEffect.ClearanceSale, 10);
        _mockShopService
            .Setup(s => s.GenerateVoucherForAnte(2, It.IsAny<List<Voucher>>()))
            .Returns(ante2Voucher);

        var result = _gameController.AdvanceAnte();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "AdvanceAnte should return true.");
            Assert.That(_gameController.CurrentAnte, Is.EqualTo(2), "CurrentAnte should increment from 1 to 2.");
            Assert.That(_gameController.IsAnteVoucherPurchased, Is.False, "IsAnteVoucherPurchased must be reset to false.");
            Assert.That(_gameController.IsBossBlindRerolledThisAnte, Is.False, "IsBossBlindRerolledThisAnte must be reset to false.");
            Assert.That(_gameController.CurrentAnteVoucher, Is.EqualTo(ante2Voucher), "CurrentAnteVoucher should be updated for Ante 2.");
            Assert.That(_gameController.BlindEnemies.ContainsKey(2), Is.True, "BlindEnemies must contain entries for Ante 2.");
            Assert.That(_gameController.BlindEnemies[2], Has.Count.EqualTo(3), "Ante 2 must contain 3 blinds (Small, Big, Boss).");
            Assert.That(eventAnte, Is.EqualTo(2), "OnAnteAdvance event must be fired with new Ante value 2.");
        });

        _mockShopService.Verify(
            s => s.GenerateVoucherForAnte(2, It.IsAny<List<Voucher>>()),
            Times.Once,
            "ShopService should be called once to generate voucher for Ante 2.");
    }

    /// <summary>
    /// Verifies that GetGameState returns complete shop DTO and game state when in shop phase.
    /// </summary>
    [Test]
    public void GetGameState_WhenInShopPhase_ReturnsCompleteShopDtoAndGameState()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();

        var testJoker = new JokerCard("Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 4f, 2);
        _gameController.Shop.JokerCardOffers.Add(testJoker);

        var lastScore = new ScoreCalculationResultDto
        {
            HandType = PokerHandType.Flush,
            FinalScore = 450
        };
        string testMessage = "Welcome to Shop!";

        var state = _gameController.GetGameState(testMessage, lastScore);

        Assert.Multiple(() =>
        {
            Assert.That(state, Is.Not.Null, "GameStateResponseDto must not be null.");
            Assert.That(state.SessionId, Is.EqualTo(_gameController.SessionId), "SessionId should match.");
            Assert.That(state.Phase, Is.EqualTo(GameStatePhase.InShop), "Phase must be InShop.");
            Assert.That(state.CurrentAnte, Is.EqualTo(_gameController.CurrentAnte), "CurrentAnte should match.");
            Assert.That(state.CurrentRound, Is.EqualTo(_gameController.CurrentRound), "CurrentRound should match.");
            Assert.That(state.Money, Is.EqualTo(_gameController.Money), "Money should match.");
            Assert.That(state.HandsRemaining, Is.EqualTo(_gameController.HandsRemaining), "HandsRemaining should match.");
            Assert.That(state.DiscardsRemaining, Is.EqualTo(_gameController.DiscardsRemaining), "DiscardsRemaining should match.");
            Assert.That(state.LastMessage, Is.EqualTo(testMessage), "LastMessage should be set.");
            Assert.That(state.LastScoreResult, Is.EqualTo(lastScore), "LastScoreResult should be set.");
            Assert.That(state.Shop, Is.Not.Null, "Shop DTO must not be null when Phase is InShop.");
            Assert.That(state.Shop!.JokerCards, Does.Contain(testJoker), "Shop DTO should contain shop joker offers.");
            Assert.That(state.PokerHandLevels, Has.Count.EqualTo(Enum.GetValues<PokerHandType>().Length), "PokerHandLevels should contain all hand types.");
        });
    }

    /// <summary>
    /// Verifies that StartGame handles null voucher gracefully when shop service returns null.
    /// </summary>
    [Test]
    public void StartGame_WhenShopServiceReturnsNull_HandlesNullVoucherGracefully()
    {
        _mockShopService
            .Setup(s => s.GenerateVoucherForAnte(1, It.IsAny<List<Voucher>>()))
            .Returns((Voucher?)null);

        var result = _gameController.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "StartGame should return true even if voucher is null.");
            Assert.That(_gameController.CurrentAnteVoucher, Is.Null, "CurrentAnteVoucher should be null.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.SelectingBlind), "Game should successfully reach SelectingBlind phase.");
            Assert.That(_gameController.DrawPile.Count, Is.EqualTo(52), "Draw pile should still be initialized.");
        });

        _mockShopService.Verify(
            s => s.GenerateVoucherForAnte(1, It.IsAny<List<Voucher>>()),
            Times.Once,
            "ShopService should be called once.");
    }

    /// <summary>
    /// Verifies that AdvanceAnte calculates exponential base score correctly beyond max ante.
    /// </summary>
    [Test]
    public void AdvanceAnte_BeyondMaxAnte_CalculatesExponentialBaseScoreCorrectly()
    {
        _gameController.StartGame();

        for (var ante = 2; ante <= 9; ante++)
        {
            _gameController.AdvanceAnte();
        }

        var ante9Blinds = _gameController.GetAvailableBlinds();
        var expectedAnte9BaseScore = (int)(50000 * Math.Pow(1.5, 9 - 8));

        Assert.Multiple(() =>
        {
            Assert.That(_gameController.CurrentAnte, Is.EqualTo(9), "CurrentAnte should advance to Ante 9.");
            Assert.That(ante9Blinds, Has.Count.EqualTo(3), "Ante 9 should contain three blinds.");
            Assert.That(ante9Blinds.Single(b => b.BlindType == BlindType.Small).ScoreToDefeat,
                Is.EqualTo(expectedAnte9BaseScore), "Ante 9 Small Blind should use the exponential base score.");
            Assert.That(ante9Blinds.Single(b => b.BlindType == BlindType.Big).ScoreToDefeat,
                Is.EqualTo((int)(expectedAnte9BaseScore * 1.5)), "Ante 9 Big Blind should scale from the exponential base score.");
            Assert.That(ante9Blinds.All(b => b.ScoreToDefeat > 0), Is.True,
                "Endless mode blind scores must remain positive and must not overflow.");
        });

        _gameController.AdvanceAnte();
        var ante10Blinds = _gameController.GetAvailableBlinds();
        var expectedAnte10BaseScore = (int)(50000 * Math.Pow(1.5, 10 - 8));

        Assert.That(ante10Blinds.Single(b => b.BlindType == BlindType.Small).ScoreToDefeat,
            Is.EqualTo(expectedAnte10BaseScore), "Ante 10 Small Blind should continue the exponential progression.");
    }

    /// <summary>
    /// Verifies that GetGameState does not throw null-reference when shop collections are empty.
    /// </summary>
    [Test]
    public void GetGameState_WhenShopCollectionsAreEmpty_DoesNotThrowNullReference()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();

        _gameController.Shop.JokerCardOffers = null!;
        _gameController.Shop.PlayingCardOffers = null!;
        _gameController.Shop.TarotCardOffers = null!;
        _gameController.Shop.PlanetCardOffers = null!;
        _gameController.Shop.SpectralCardOffers = null!;
        _gameController.Shop.BoosterPacks = null!;

        GameStateResponseDto? state = null;
        Assert.DoesNotThrow(() => state = _gameController.GetGameState(),
            "GetGameState should handle null shop collections without throwing.");

        Assert.Multiple(() =>
        {
            Assert.That(state, Is.Not.Null, "GameStateResponseDto should still be returned.");
            Assert.That(state!.Shop, Is.Not.Null, "Shop DTO should be available in shop phase.");
            Assert.That(state.Shop!.JokerCards, Is.Empty, "Null joker offers should become an empty list.");
            Assert.That(state.Shop.PlayingCards, Is.Empty, "Null playing-card offers should become an empty list.");
            Assert.That(state.Shop.TarotCards, Is.Empty, "Null tarot offers should become an empty list.");
            Assert.That(state.Shop.PlanetCards, Is.Empty, "Null planet offers should become an empty list.");
            Assert.That(state.Shop.SpectralCards, Is.Empty, "Null spectral offers should become an empty list.");
            Assert.That(state.Shop.BoosterPacks, Is.Empty, "Null booster offers should become an empty list.");
        });
    }

    /// <summary>
    /// Verifies that Win produces a consistent Victory state when invoked during the GameOver phase.
    /// </summary>
    [Test]
    public void Win_WhenInvokedDuringGameOverPhase_DoesNotProduceInconsistentState()
    {
        _gameController.GameOver();

        var winEventFired = false;
        _gameController.OnWinGame += () => winEventFired = true;

        var result = _gameController.Win();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False, "Win should be rejected after the game has ended.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.GameOver),
                "GameOver must remain the terminal phase.");
            Assert.That(winEventFired, Is.False, "OnWinGame must not fire from GameOver phase.");
        });
    }
    
    #endregion

    #region Blind Selection & Boss Generation Tests

    /// <summary>
    /// Verifies that selecting a valid small blind enters the Playing phase and draws the initial hand.
    /// </summary>
    [Test]
    public void SelectBlind_ValidSmallBlind_TransitionsToPlayingPhaseAndDrawsInitialHand()
    {
        _gameController.StartGame();
        Blind? selectedBlindFromEvent = null;
        _gameController.OnBlindSelected += blind => selectedBlindFromEvent = blind;

        var result = _gameController.SelectBlind(1);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Selecting a valid Small Blind should succeed.");
            Assert.That(_gameController.CurrentBlind, Is.Not.Null, "CurrentBlind should be populated.");
            Assert.That(_gameController.CurrentBlind!.Id, Is.EqualTo(1), "Small Blind has ID 1.");
            Assert.That(_gameController.CurrentBlind.BlindType, Is.EqualTo(BlindType.Small));
            Assert.That(_gameController.Hand, Has.Count.EqualTo(_gameController.MaxHand),
                "Initial hand should contain MaxHand cards.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
            Assert.That(selectedBlindFromEvent, Is.SameAs(_gameController.CurrentBlind),
                "OnBlindSelected should fire with the selected blind.");
        });
    }

    /// <summary>
    /// Verifies that SelectBlind recycles and clears debuffs for cards from previous round.
    /// </summary>
    [Test]
    public void SelectBlind_CardsFromPreviousRound_RecyclesAndClearsDebuffs()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);

        var cardMovedToDiscard = _gameController.Hand[0];
        _gameController.Hand.RemoveAt(0);
        _gameController.DiscardPile.DiscardCards(new[] { cardMovedToDiscard });

        foreach (var card in _gameController.Hand
                     .Concat(_gameController.DrawPile.PlayingCards)
                     .Concat(_gameController.DiscardPile.PlayingCards))
        {
            card.IsDebuffed = true;
        }

        _gameController.DefeatBlind();
        _gameController.LeaveShop();

        var result = _gameController.SelectBlind(2);
        var allCards = _gameController.Hand
            .Concat(_gameController.DrawPile.PlayingCards)
            .Concat(_gameController.DiscardPile.PlayingCards)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Selecting the next valid blind should succeed.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
            Assert.That(_gameController.Hand, Has.Count.EqualTo(_gameController.MaxHand));
            Assert.That(_gameController.DiscardPile, Is.Empty,
                "DiscardPile should be empty after cards are recycled.");
            Assert.That(allCards, Has.Count.EqualTo(52), "The complete deck should be preserved.");
            Assert.That(allCards.Any(card => card.IsDebuffed), Is.False,
                "All cards should have their debuff cleared before the new blind.");
        });
    }

    /// <summary>
    /// Verifies that an invalid blind ID is rejected without changing the current phase.
    /// </summary>
    [TestCase(999)]
    [TestCase(-1)]
    public void SelectBlind_InvalidBlindId_ReturnsFalseAndPhaseUnchanged(int invalidBlindId)
    {
        _gameController.StartGame();

        var result = _gameController.SelectBlind(invalidBlindId);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False, "Selecting an invalid blind ID should fail.");
            Assert.That(_gameController.CurrentBlind, Is.Null, "CurrentBlind should remain null.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.SelectingBlind),
                "Phase should remain SelectingBlind.");
        });
    }
    /// <summary>
    /// Verifies that SelectBlind returns false for already defeated blind.
    /// </summary>
    [Test]
    public void SelectBlind_AlreadyDefeatedBlind_ReturnsFalse()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var defeatedBlind = _gameController.CurrentBlind;
        _gameController.DefeatBlind();
        _gameController.LeaveShop();

        var handCountBeforeRetry = _gameController.Hand.Count;
        var result = _gameController.SelectBlind(1);

        Assert.Multiple(() =>
        {
            Assert.That(defeatedBlind!.IsDefeated, Is.True);
            Assert.That(result, Is.False);
            Assert.That(_gameController.CurrentBlind, Is.SameAs(defeatedBlind));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.SelectingBlind));
            Assert.That(_gameController.Hand, Has.Count.EqualTo(handCountBeforeRetry));
        });
    }

    /// <summary>
    /// Verifies that SelectBlind returns false when not in selecting blind phase.
    /// </summary>
    [TestCase(GameStatePhase.Playing)]
    [TestCase(GameStatePhase.InShop)]
    public void SelectBlind_WhenNotInSelectingBlindPhase_ReturnsFalse(GameStatePhase phase)
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        if (phase == GameStatePhase.InShop)
        {
            _gameController.DefeatBlind();
        }

        var currentBlindBeforeRetry = _gameController.CurrentBlind;
        var handCountBeforeRetry = _gameController.Hand.Count;
        var result = _gameController.SelectBlind(2);

        Assert.Multiple(() =>
        {
            Assert.That(_gameController.Phase, Is.EqualTo(phase));
            Assert.That(result, Is.False);
            Assert.That(_gameController.CurrentBlind, Is.SameAs(currentBlindBeforeRetry));
            Assert.That(_gameController.Hand, Has.Count.EqualTo(handCountBeforeRetry));
        });
    }

    /// <summary>
    /// Verifies that Director's Cut and sufficient money allow RerollBossBlind to replace the boss blind.
    /// </summary>
    [Test]
    public void RerollBossBlind_WithDirectorsCutVoucherAndSufficientMoney_RerollsBossBlind()
    {
        _gameController.StartGame();
        _gameController.PurchasedVouchers.Add(
            new Voucher("DirectorsCut", VoucherEffect.DirectorsCut, 10));
        _gameController.Money = 20;

        var oldBossBlind = _gameController.BlindEnemies[_gameController.CurrentAnte]
            .Single(blind => blind.BlindType == BlindType.Boss);
        var result = _gameController.RerollBossBlind();
        var newBossBlind = _gameController.BlindEnemies[_gameController.CurrentAnte]
            .Single(blind => blind.BlindType == BlindType.Boss);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(10));
            Assert.That(_gameController.IsBossBlindRerolledThisAnte, Is.True);
            Assert.That(newBossBlind, Is.Not.SameAs(oldBossBlind));
            Assert.That(newBossBlind.BlindType, Is.EqualTo(BlindType.Boss));
        });
    }

    /// <summary>
    /// Verifies that RerollBossBlind fails without the Director's Cut voucher.
    /// </summary>
    [Test]
    public void RerollBossBlind_WithoutDirectorsCutVoucher_ReturnsFailure()
    {
        _gameController.StartGame();

        var result = _gameController.RerollBossBlind();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Director's Cut voucher required to reroll Boss Blind."));
            Assert.That(_gameController.Money, Is.EqualTo(4));
            Assert.That(_gameController.IsBossBlindRerolledThisAnte, Is.False);
        });
    }

    /// <summary>
    /// Verifies that RerollBossBlind fails after the boss has already been rerolled in the same ante.
    /// </summary>
    [Test]
    public void RerollBossBlind_AlreadyRerolledInSameAnte_ReturnsFailure()
    {
        _gameController.StartGame();
        _gameController.PurchasedVouchers.Add(
            new Voucher("DirectorsCut", VoucherEffect.DirectorsCut, 10));
        _gameController.Money = 20;

        var firstResult = _gameController.RerollBossBlind();
        var moneyAfterFirstReroll = _gameController.Money;
        var secondResult = _gameController.RerollBossBlind();

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True);
            Assert.That(secondResult.Success, Is.False);
            Assert.That(secondResult.Message,
                Is.EqualTo("Boss Blind can only be rerolled once per Ante."));
            Assert.That(_gameController.Money, Is.EqualTo(moneyAfterFirstReroll));
            Assert.That(_gameController.IsBossBlindRerolledThisAnte, Is.True);
        });
    }

    /// <summary>
    /// Verifies that RerollBossBlind returns failure for insufficient money.
    /// </summary>
    [Test]
    public void RerollBossBlind_InsufficientMoney_ReturnsFailure()
    {
        _gameController.StartGame();
        _gameController.PurchasedVouchers.Add(
            new Voucher("DirectorsCut", VoucherEffect.DirectorsCut, 10));
        _gameController.Money = 9;

        var result = _gameController.RerollBossBlind();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message,
                Is.EqualTo("Not enough money to reroll Boss Blind (Costs $10)."));
            Assert.That(_gameController.Money, Is.EqualTo(9));
            Assert.That(_gameController.IsBossBlindRerolledThisAnte, Is.False);
        });
    }
    
    #endregion

    #region Play Hand Mechanics & Scoring Tests

    /// <summary>
    /// Verifies that PlayHand scores valid selected cards, consumes a hand, and draws replacements.
    /// </summary>
    [Test]
    public void PlayHand_ValidCardsSelected_CalculatesScoreReducesHandsAndDrawsCards()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var cardToPlay = _gameController.Hand[0];
        var scoreResult = new ScoreCalculationResultDto
        {
            HandType = PokerHandType.HighCard,
            FinalScore = 100,
            ScoringCards = new List<PlayingCard> { cardToPlay }
        };
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(scoreResult);

        var result = _gameController.PlayHand(new List<string> { cardToPlay.Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.SameAs(scoreResult));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(3));
            Assert.That(_gameController.RoundScore, Is.EqualTo(100));
            Assert.That(_gameController.Hand, Has.Count.EqualTo(_gameController.MaxHand),
                "A replacement card should be drawn after playing a card.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });

        _mockScoringService.Verify(
            s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that PlayHand defeats the blind and opens the shop when the score reaches the target.
    /// </summary>
    [Test]
    public void PlayHand_ScoreReachesTarget_DefeatsBlindAndOpensShop()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var cardToPlay = _gameController.Hand[0];
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto
            {
                HandType = PokerHandType.HighCard,
                FinalScore = _gameController.CurrentBlind!.ScoreToDefeat,
                ScoringCards = new List<PlayingCard> { cardToPlay }
            });

        var result = _gameController.PlayHand(new List<string> { cardToPlay.Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.CurrentBlind!.IsDefeated, Is.True);
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.InShop));
        });
    }

    /// <summary>
    /// Verifies that PlayHand adds money won from a Lucky Card to the player's balance.
    /// </summary>
    [Test]
    public void PlayHand_WithLuckyCardMoneyWon_AddsBonusMoneyToPlayer()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var cardToPlay = _gameController.Hand[0];
        const int luckyMoneyWon = 7;
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto
            {
                HandType = PokerHandType.HighCard,
                FinalScore = 100,
                LuckyMoneyWon = luckyMoneyWon,
                ScoringCards = new List<PlayingCard> { cardToPlay }
            });

        var result = _gameController.PlayHand(new List<string> { cardToPlay.Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(4 + luckyMoneyWon));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that PlayHand destroys shattered Glass Cards and discards the surviving cards.
    /// </summary>
    [Test]
    public void PlayHand_WithGlassCard_DestroysShatteredCardsAndDiscardsSurviving()
    {
        var shattered = false;

        // Shatter memiliki peluang acak 1/4, sehingga ulangi pada game baru sampai cabang shatter teruji.
        for (var attempt = 0; attempt < 100 && !shattered; attempt++)
        {
            _mockScoringService.Reset();
            _gameController = new GameController(
                _mockScoringService.Object,
                _mockShopService.Object,
                _mockConsumableHandler.Object,
                NullLogger<GameController>.Instance);
            _gameController.StartGame();
            _gameController.SelectBlind(1);

            var glassCard = _gameController.Hand[0];
            var survivingCard = _gameController.Hand[1];
            glassCard.Enhancement = EnhancePokerCard.GlassCards;
            survivingCard.Enhancement = EnhancePokerCard.None;

            _mockScoringService
                .Setup(s => s.CalculateScore(
                    It.IsAny<List<PlayingCard>>(),
                    It.IsAny<List<PlayingCard>>(),
                    It.IsAny<List<JokerCard>>(),
                    It.IsAny<Dictionary<PokerHandType, int>>(),
                    It.IsAny<BlindId?>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns(new ScoreCalculationResultDto
                {
                    HandType = PokerHandType.HighCard,
                    FinalScore = 1,
                    ScoringCards = new List<PlayingCard> { glassCard, survivingCard }
                });

            var result = _gameController.PlayHand(new List<string> { glassCard.Id, survivingCard.Id });
            Assert.That(result.Success, Is.True);

            if (result.Data!.JokerTriggerMessages.Any(message => message.Contains("Shattered!")))
            {
                shattered = true;
                Assert.Multiple(() =>
                {
                    Assert.That(_gameController.DiscardPile.PlayingCards, Does.Contain(survivingCard));
                    Assert.That(_gameController.DiscardPile.PlayingCards, Does.Not.Contain(glassCard));
                });
            }
        }

        Assert.That(shattered, Is.True, "At least one Glass card should shatter during the attempts.");
    }

    /// <summary>
    /// Verifies that PlayHand returns failure result when not in playing phase.
    /// </summary>
    [TestCase(GameStatePhase.SelectingBlind)]
    [TestCase(GameStatePhase.InShop)]
    public void PlayHand_WhenNotInPlayingPhase_ReturnsFailureResult(GameStatePhase phase)
    {
        _gameController.StartGame();
        if (phase == GameStatePhase.InShop)
        {
            _gameController.SelectBlind(1);
            _gameController.DefeatBlind();
        }

        var result = _gameController.PlayHand(new List<string> { "card-not-in-play" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo($"Cannot play hand while in {phase} phase."));
            Assert.That(result.Data, Is.Null);
            Assert.That(_gameController.Phase, Is.EqualTo(phase));
        });
    }

    /// <summary>
    /// Verifies that PlayHand returns failure result for empty card list.
    /// </summary>
    [Test]
    public void PlayHand_EmptyCardList_ReturnsFailureResult()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        List<string>? nullCardIds = null;

        var nullResult = _gameController.PlayHand(nullCardIds!);
        var emptyResult = _gameController.PlayHand(new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(nullResult.Success, Is.False);
            Assert.That(nullResult.Message, Is.EqualTo("Must play between 1 and 5 cards."));
            Assert.That(emptyResult.Success, Is.False);
            Assert.That(emptyResult.Message, Is.EqualTo("Must play between 1 and 5 cards."));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that PlayHand rejects a selection containing more than five cards.
    /// </summary>
    [Test]
    public void PlayHand_ExceedsFiveCards_ReturnsFailureResult()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var initialHandsRemaining = _gameController.HandsRemaining;
        var sixCardIds = _gameController.Hand.Take(6).Select(card => card.Id).ToList();

        var result = _gameController.PlayHand(sixCardIds);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Must play between 1 and 5 cards."));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(initialHandsRemaining));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that PlayHand returns failure result for card not in hand.
    /// </summary>
    [Test]
    public void PlayHand_CardNotInHand_ReturnsFailureResult()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var initialHandsRemaining = _gameController.HandsRemaining;

        var result = _gameController.PlayHand(new List<string> { "card-not-in-hand" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("One or more selected cards are not in hand."));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(initialHandsRemaining));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that PlayHand triggers game over when the final hand does not meet the blind target.
    /// </summary>
    [Test]
    public void PlayHand_LastHandExhaustedWithoutMeetingTarget_TriggersGameOver()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto
            {
                HandType = PokerHandType.HighCard,
                FinalScore = 1
            });

        for (var hand = 0; hand < 3; hand++)
        {
            var cardId = _gameController.Hand[0].Id;
            var intermediateResult = _gameController.PlayHand(new List<string> { cardId });
            Assert.That(intermediateResult.Success, Is.True);
        }

        var lastCardId = _gameController.Hand[0].Id;
        var result = _gameController.PlayHand(new List<string> { lastCardId });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Game Over! Hands exhausted before reaching target score."));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(0));
            Assert.That(_gameController.RoundScore, Is.EqualTo(4));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.GameOver));
        });
    }

    #endregion

    #region Boss Blind Specific Rules & Restrictions Tests

    /// <summary>
    /// Verifies that The Psychic rejects a hand containing fewer than five played cards.
    /// </summary>
    [Test]
    public void PlayHand_ThePsychicBoss_FailsWhenPlayingLessThanFiveCards()
    {
        _gameController.StartGame();
        var psychic = new Blind(BlindId.ThePsychic, "The Psychic", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = psychic;
        _gameController.SelectBlind(3);

        var lessThanFiveCards = _gameController.Hand.Take(4).Select(card => card.Id).ToList();
        var result = _gameController.PlayHand(lessThanFiveCards);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("The Psychic forces you to play exactly 5 cards!"));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(4));
        });

        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 1 });

        var fiveCardResult = _gameController.PlayHand(_gameController.Hand.Take(5).Select(card => card.Id).ToList());
        Assert.That(fiveCardResult.Success, Is.True, "The Psychic should allow exactly five cards.");
    }

    /// <summary>
    /// Verifies that The Eye rejects a poker-hand type already played during the round.
    /// </summary>
    [Test]
    public void PlayHand_TheEyeBoss_FailsWhenPlayingRepeatedPokerHandType()
    {
        _gameController.StartGame();
        var theEye = new Blind(BlindId.TheEye, "The Eye", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theEye;
        _gameController.SelectBlind(3);
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 1 });

        var firstResult = _gameController.PlayHand(new List<string> { _gameController.Hand[0].Id });
        var secondResult = _gameController.PlayHand(new List<string> { _gameController.Hand[0].Id });

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True);
            Assert.That(secondResult.Success, Is.False);
            Assert.That(secondResult.Message, Is.EqualTo("The Eye does not allow repeating HighCard in this round!"));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(3));
        });
    }

    /// <summary>
    /// Verifies that The Mouth rejects a poker-hand type different from the first hand played.
    /// </summary>
    [Test]
    public void PlayHand_TheMouthBoss_FailsWhenPlayingDifferentPokerHandType()
    {
        _gameController.StartGame();
        var theMouth = new Blind(BlindId.TheMouth, "The Mouth", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theMouth;
        _gameController.SelectBlind(3);
        _mockScoringService
            .SetupSequence(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 1 })
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.Pair, FinalScore = 1 });

        var firstResult = _gameController.PlayHand(new List<string> { _gameController.Hand[0].Id });
        var secondResult = _gameController.PlayHand(new List<string> { _gameController.Hand[0].Id });

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True);
            Assert.That(secondResult.Success, Is.False);
            Assert.That(secondResult.Message, Is.EqualTo("The Mouth only allows playing HighCard this round!"));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(3));
        });
    }

    /// <summary>
    /// Verifies that SelectBlind sets initial hands to one for the needle boss.
    /// </summary>
    [Test]
    public void SelectBlind_TheNeedleBoss_SetsInitialHandsToOne()
    {
        _gameController.StartGame();
        var theNeedle = new Blind(BlindId.TheNeedle, "The Needle", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theNeedle;

        var result = _gameController.SelectBlind(3);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_gameController.CurrentBlind, Is.SameAs(theNeedle));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(1));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that SelectBlind sets initial discards to zero for the water boss.
    /// </summary>
    [Test]
    public void SelectBlind_TheWaterBoss_SetsInitialDiscardsToZero()
    {
        _gameController.StartGame();
        var theWater = new Blind(BlindId.TheWater, "The Water", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theWater;

        var result = _gameController.SelectBlind(3);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_gameController.CurrentBlind, Is.SameAs(theWater));
            Assert.That(_gameController.DiscardsRemaining, Is.EqualTo(0));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that SelectBlind reduces effective hand size by one for the manacle boss.
    /// </summary>
    [Test]
    public void SelectBlind_TheManacleBoss_ReducesEffectiveHandSizeByOne()
    {
        _gameController.StartGame();
        var theManacle = new Blind(BlindId.TheManacle, "The Manacle", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theManacle;

        var result = _gameController.SelectBlind(3);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_gameController.Hand, Has.Count.EqualTo(_gameController.MaxHand - 1));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that PlayHand decreases played poker hand level by one for the arm boss.
    /// </summary>
    [Test]
    public void PlayHand_TheArmBoss_DecreasesPlayedPokerHandLevelByOne()
    {
        _gameController.StartGame();
        var theArm = new Blind(BlindId.TheArm, "The Arm", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theArm;
        _gameController.SelectBlind(3);
        _gameController.PokerHandLevels[PokerHandType.HighCard] = 2;
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 1 });

        var result = _gameController.PlayHand(new List<string> { _gameController.Hand[0].Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.PokerHandLevels[PokerHandType.HighCard], Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that PlayHand deducts one dollar per played card for the tooth boss.
    /// </summary>
    [Test]
    public void PlayHand_TheToothBoss_DeductsOneDollarPerPlayedCard()
    {
        _gameController.StartGame();
        var theTooth = new Blind(BlindId.TheTooth, "The Tooth", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theTooth;
        _gameController.SelectBlind(3);
        _gameController.Money = 20;
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 1 });

        var result = _gameController.PlayHand(_gameController.Hand.Take(5).Select(card => card.Id).ToList());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(15));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that PlayHand resets money to zero when playing most played hand for the ox boss.
    /// </summary>
    [Test]
    public void PlayHand_TheOxBoss_ResetsMoneyToZeroWhenPlayingMostPlayedHand()
    {
        _gameController.StartGame();
        var theOx = new Blind(BlindId.TheOx, "The Ox", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theOx;
        _gameController.SelectBlind(3);
        _gameController.PokerHandPlayed[PokerHandType.HighCard] = 3;
        _gameController.PokerHandPlayed[PokerHandType.Pair] = 1;
        _gameController.Money = 20;
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 1 });

        var result = _gameController.PlayHand(new List<string> { _gameController.Hand[0].Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that PlayHand discards two random cards from hand for the hook boss.
    /// </summary>
    [Test]
    public void PlayHand_TheHookBoss_DiscardsTwoRandomCardsFromHand()
    {
        _gameController.StartGame();
        var theHook = new Blind(BlindId.TheHook, "The Hook", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = theHook;
        _gameController.SelectBlind(3);
        var playedCard = _gameController.Hand[0];
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 1 });

        var result = _gameController.PlayHand(new List<string> { playedCard.Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Hand, Has.Count.EqualTo(_gameController.MaxHand));
            Assert.That(_gameController.DiscardPile.PlayingCards, Has.Count.EqualTo(3));
            Assert.That(_gameController.DiscardPile.PlayingCards, Does.Contain(playedCard));
        });
    }

    /// <summary>
    /// Verifies that SelectBlind debuffs matching cards for suit debuff bosses.
    /// </summary>
    [TestCase(BlindId.TheClub, Suit.Clubs, Rank.Ace)]
    [TestCase(BlindId.TheGoad, Suit.Spades, Rank.Ace)]
    [TestCase(BlindId.TheWindow, Suit.Diamonds, Rank.Ace)]
    [TestCase(BlindId.TheHead, Suit.Hearts, Rank.Ace)]
    [TestCase(BlindId.ThePlant, Suit.Hearts, Rank.Jack)]
    public void SelectBlind_SuitDebuffBosses_DebuffsMatchingCards(BlindId bossId, Suit matchingSuit, Rank matchingRank)
    {
        _gameController.StartGame();
        var matchingCard = new PlayingCard(matchingSuit, matchingRank);
        var nonMatchingCard = bossId == BlindId.ThePlant
            ? new PlayingCard(Suit.Hearts, Rank.Ace)
            : new PlayingCard(matchingSuit == Suit.Clubs ? Suit.Spades : Suit.Clubs, Rank.Two);
        _gameController.DrawPile.PlayingCards.Clear();
        _gameController.DrawPile.AddCards(new[]
        {
            matchingCard, nonMatchingCard, new PlayingCard(Suit.Hearts, Rank.Two),
            new PlayingCard(Suit.Spades, Rank.Three), new PlayingCard(Suit.Clubs, Rank.Four),
            new PlayingCard(Suit.Diamonds, Rank.Five), new PlayingCard(Suit.Hearts, Rank.Six),
            new PlayingCard(Suit.Spades, Rank.Seven)
        });
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] =
            new Blind(bossId, bossId.ToString(), BlindType.Boss, 1000) { Id = 3 };

        var result = _gameController.SelectBlind(3);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(matchingCard.IsDebuffed, Is.True);
            Assert.That(nonMatchingCard.IsDebuffed, Is.False);
        });
    }

    /// <summary>
    /// Verifies that selling a joker against The Verdant Leaf removes all card debuffs.
    /// </summary>
    [Test]
    public void SellCard_VerdantLeafBoss_LiftsAllCardDebuffsWhenJokerSold()
    {
        _gameController.StartGame();
        for (var ante = 2; ante <= 8; ante++)
        {
            _gameController.AdvanceAnte();
        }

        var verdantLeaf = new Blind(BlindId.VerdantLeaf, "Verdant Leaf", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = verdantLeaf;
        _gameController.SelectBlind(3);
        var discardedCard = _gameController.Hand[0];
        _gameController.Hand.RemoveAt(0);
        _gameController.DiscardPile.DiscardCards(new[] { discardedCard });
        var joker = new JokerCard("Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 4f, 4);
        _gameController.Deck.JokerCards.Add(joker);
        discardedCard.IsDebuffed = true;

        var result = _gameController.SellCard(joker.Id);
        var allCards = _gameController.Hand
            .Concat(_gameController.DrawPile.PlayingCards)
            .Concat(_gameController.DiscardPile.PlayingCards);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.JokerCards, Does.Not.Contain(joker));
            Assert.That(allCards.Any(card => card.IsDebuffed), Is.False);
        });
    }

    #endregion

    #region Discard & Preview Mechanics Tests

    /// <summary>
    /// Verifies that DiscardCards discards and draws replacement cards for valid cards.
    /// </summary>
    [Test]
    public void DiscardCards_ValidCards_DiscardsAndDrawsReplacementCards()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var cardsToDiscard = _gameController.Hand.Take(2).ToList();

        var result = _gameController.DiscardCards(cardsToDiscard.Select(card => card.Id).ToList());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.DiscardsRemaining, Is.EqualTo(3));
            Assert.That(_gameController.Hand, Has.Count.EqualTo(_gameController.MaxHand));
            Assert.That(_gameController.DiscardPile.PlayingCards, Has.Count.EqualTo(2));
            Assert.That(_gameController.DiscardPile.PlayingCards, Does.Contain(cardsToDiscard[0]));
            Assert.That(_gameController.DiscardPile.PlayingCards, Does.Contain(cardsToDiscard[1]));
        });
    }

    /// <summary>
    /// Verifies that DiscardCards fails when no discards remain.
    /// </summary>
    [Test]
    public void DiscardCards_NoDiscardsRemaining_ReturnsFailure()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);

        for (var discard = 0; discard < 4; discard++)
        {
            var intermediateResult = _gameController.DiscardCards(new List<string> { _gameController.Hand[0].Id });
            Assert.That(intermediateResult.Success, Is.True);
        }

        var handCountBeforeRetry = _gameController.Hand.Count;
        var result = _gameController.DiscardCards(new List<string> { _gameController.Hand[0].Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("No discards remaining."));
            Assert.That(_gameController.DiscardsRemaining, Is.EqualTo(0));
            Assert.That(_gameController.Hand, Has.Count.EqualTo(handCountBeforeRetry));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that DiscardCards returns failure when not in playing phase.
    /// </summary>
    [TestCase(GameStatePhase.SelectingBlind)]
    [TestCase(GameStatePhase.InShop)]
    public void DiscardCards_WhenNotInPlayingPhase_ReturnsFailure(GameStatePhase phase)
    {
        _gameController.StartGame();
        if (phase == GameStatePhase.InShop)
        {
            _gameController.SelectBlind(1);
            _gameController.DefeatBlind();
        }

        var result = _gameController.DiscardCards(new List<string> { "card-not-in-play" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo($"Cannot discard cards while in {phase} phase."));
            Assert.That(_gameController.Phase, Is.EqualTo(phase));
        });
    }

    /// <summary>
    /// Verifies that DiscardCards returns failure for cards not in hand.
    /// </summary>
    [Test]
    public void DiscardCards_CardsNotInHand_ReturnsFailure()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var handCountBeforeRetry = _gameController.Hand.Count;
        var discardsBeforeRetry = _gameController.DiscardsRemaining;

        var result = _gameController.DiscardCards(new List<string> { "card-not-in-hand" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("One or more selected cards are not in hand."));
            Assert.That(_gameController.Hand, Has.Count.EqualTo(handCountBeforeRetry));
            Assert.That(_gameController.DiscardsRemaining, Is.EqualTo(discardsBeforeRetry));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Playing));
        });
    }

    /// <summary>
    /// Verifies that GetScorePreview returns calculated preview without mutating state for valid cards.
    /// </summary>
    [Test]
    public void GetScorePreview_ValidCards_ReturnsCalculatedPreviewWithoutMutatingState()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        var card = _gameController.Hand[0];
        var expectedScore = new ScoreCalculationResultDto
        {
            HandType = PokerHandType.HighCard,
            FinalScore = 125
        };
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(expectedScore);

        var handsBeforePreview = _gameController.HandsRemaining;
        var moneyBeforePreview = _gameController.Money;
        var discardCountBeforePreview = _gameController.DiscardPile.Count;
        var result = _gameController.GetScorePreview(new List<string> { card.Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Score preview calculated."));
            Assert.That(result.Data, Is.SameAs(expectedScore));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(handsBeforePreview));
            Assert.That(_gameController.Money, Is.EqualTo(moneyBeforePreview));
            Assert.That(_gameController.DiscardPile.Count, Is.EqualTo(discardCountBeforePreview));
            Assert.That(_gameController.Hand, Does.Contain(card));
        });

        _mockScoringService.Verify(
            s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    /// <summary>
    /// Verifies that GetScorePreview returns failure for empty or more than five cards.
    /// </summary>
    [Test]
    public void GetScorePreview_EmptyOrMoreThanFiveCards_ReturnsFailure()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        List<string>? nullCardIds = null;

        var emptyResult = _gameController.GetScorePreview(nullCardIds!);
        var tooManyResult = _gameController.GetScorePreview(
            _gameController.Hand.Take(6).Select(card => card.Id).ToList());

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Success, Is.False);
            Assert.That(emptyResult.Message, Is.EqualTo("Select 1 to 5 cards for score preview."));
            Assert.That(tooManyResult.Success, Is.False);
            Assert.That(tooManyResult.Message, Is.EqualTo("Select 1 to 5 cards for score preview."));
            Assert.That(_gameController.HandsRemaining, Is.EqualTo(4));
            Assert.That(_gameController.Money, Is.EqualTo(4));
        });
    }

    #endregion

    #region Blind Defeat, Cashout, & End-of-Round Effects Tests

    /// <summary>
    /// Verifies that DefeatBlind calculates reward hands and interest for standard cashout.
    /// </summary>
    [Test]
    public void DefeatBlind_StandardCashout_CalculatesRewardHandsAndInterest()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.Money = 15;
        _mockScoringService
            .Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(), It.IsAny<List<JokerCard>>(),
                It.IsAny<Dictionary<PokerHandType, int>>(), It.IsAny<BlindId?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 1 });

        for (var hand = 0; hand < 2; hand++)
        {
            var playResult = _gameController.PlayHand(new List<string> { _gameController.Hand[0].Id });
            Assert.That(playResult.Success, Is.True);
        }

        var result = _gameController.DefeatBlind();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(23),
                "Cashout should add $3 reward, $2 for remaining hands, and $3 interest.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.InShop));
        });
    }

    /// <summary>
    /// Verifies that DefeatBlind caps interest at ten dollars with seed money voucher.
    /// </summary>
    [Test]
    public void DefeatBlind_WithSeedMoneyVoucher_CapsInterestAtTenDollars()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.PurchasedVouchers.Add(
            new Voucher("SeedMoney", VoucherEffect.SeedMoney, 10));
        _gameController.Money = 60;

        var result = _gameController.DefeatBlind();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(77),
                "Cashout should include the $10 SeedMoney interest cap.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.InShop));
        });
    }

    /// <summary>
    /// Verifies that DefeatBlind awards three dollars per gold card with gold cards in hand.
    /// </summary>
    [Test]
    public void DefeatBlind_WithGoldCardsInHand_AwardsThreeDollarsPerGoldCard()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.Money = 0;
        _gameController.Hand[0].Enhancement = EnhancePokerCard.GoldCards;
        _gameController.Hand[1].Enhancement = EnhancePokerCard.GoldCards;

        var result = _gameController.DefeatBlind();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(13),
                "Cashout adds $3 reward, $4 for remaining hands, and $6 for two Gold Cards.");
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.InShop));
        });
    }

    /// <summary>
    /// Verifies that DefeatBlind applies end-of-round effects from owned jokers.
    /// </summary>
    [Test]
    public void DefeatBlind_WithJokers_TriggersEndOfRoundJokerEffects()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.Money = 0;
        var goldenJoker = new JokerCard(JokerId.GoldenJoker, "Golden Joker", JokerEdition.Base,
            JokerRarity.Common, JokerModifierType.AdditionMultiplier, 4f, 4);
        var popcorn = new JokerCard(JokerId.Popcorn, "Popcorn", JokerEdition.Base,
            JokerRarity.Common, JokerModifierType.AdditionMultiplier, 10f, 4);
        _gameController.Deck.JokerCards.Add(goldenJoker);
        _gameController.Deck.JokerCards.Add(popcorn);

        var result = _gameController.DefeatBlind();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(11),
                "Cashout adds $3 reward, $4 for remaining hands, and $4 from Golden Joker.");
            Assert.That(popcorn.MultValue, Is.EqualTo(6f));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.InShop));
        });
    }

    #endregion

    #region Shop Purchases & Boosters Tests
    
    /// <summary>
    /// Verifies that BuyCardFromShop adds to jokers and deducts money for affordable joker.
    /// </summary>
    [Test]
    public void BuyCardFromShop_AffordableJoker_AddsToJokersAndDeductsMoney()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();
        var joker = new JokerCard("Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 4f, 4);
        _gameController.Shop.JokerCardOffers.Add(joker);
        _gameController.Money = 10;

        var result = _gameController.BuyCardFromShop(joker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(6));
            Assert.That(_gameController.Deck.JokerCards, Does.Contain(joker));
            Assert.That(_gameController.Shop.JokerCardOffers, Does.Not.Contain(joker));
        });
    }

    /// <summary>
    /// Verifies that BuyCardFromShop permits a Negative Joker purchase when joker slots are full.
    /// </summary>
    [Test]
    public void BuyCardFromShop_NegativeJokerWhenSlotsFull_SuccessfullyPurchases()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();
        for (var slot = 0; slot < 5; slot++)
        {
            _gameController.Deck.JokerCards.Add(new JokerCard($"Joker {slot}", JokerEdition.Base,
                JokerRarity.Common, JokerModifierType.AdditionMultiplier, 1f, 1));
        }
        var negativeJoker = new JokerCard("Negative Joker", JokerEdition.Negative, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 4f, 4);
        _gameController.Shop.JokerCardOffers.Add(negativeJoker);
        _gameController.Money = 10;

        var result = _gameController.BuyCardFromShop(negativeJoker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(6));
            Assert.That(_gameController.Deck.JokerCards, Does.Contain(negativeJoker));
            Assert.That(_gameController.Shop.JokerCardOffers, Does.Not.Contain(negativeJoker));
        });
    }

    /// <summary>
    /// Verifies that BuyCardFromShop returns failure for insufficient money.
    /// </summary>
    [Test]
    public void BuyCardFromShop_InsufficientMoney_ReturnsFailure()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();
        var joker = new JokerCard("Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 4f, 4);
        _gameController.Shop.JokerCardOffers.Add(joker);
        _gameController.Money = 3;

        var result = _gameController.BuyCardFromShop(joker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Not enough money."));
            Assert.That(_gameController.Money, Is.EqualTo(3));
            Assert.That(_gameController.Deck.JokerCards, Does.Not.Contain(joker));
            Assert.That(_gameController.Shop.JokerCardOffers, Does.Contain(joker));
        });
    }

    /// <summary>
    /// Verifies that BuyCardFromShop returns failure for joker slots full.
    /// </summary>
    [Test]
    public void BuyCardFromShop_JokerSlotsFull_ReturnsFailure()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();
        for (var slot = 0; slot < 5; slot++)
        {
            _gameController.Deck.JokerCards.Add(new JokerCard($"Joker {slot}", JokerEdition.Base,
                JokerRarity.Common, JokerModifierType.AdditionMultiplier, 1f, 1));
        }
        var joker = new JokerCard("Joker Offer", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 4f, 4);
        _gameController.Shop.JokerCardOffers.Add(joker);
        _gameController.Money = 10;

        var result = _gameController.BuyCardFromShop(joker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Joker slots are full."));
            Assert.That(_gameController.Money, Is.EqualTo(10));
            Assert.That(_gameController.Shop.JokerCardOffers, Does.Contain(joker));
        });
    }

    /// <summary>
    /// Verifies that BuyCardFromShop returns failure for consumable slots full.
    /// </summary>
    [Test]
    public void BuyCardFromShop_ConsumableSlotsFull_ReturnsFailure()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();
        _gameController.Deck.UsableCards.Add(new TarotCard("The Fool", 3, TarotType.TheFool));
        _gameController.Deck.UsableCards.Add(new TarotCard("The Magician", 3, TarotType.TheMagician));
        var tarot = new TarotCard("The Empress", 3, TarotType.TheEmpress);
        _gameController.Shop.TarotCardOffers.Add(tarot);
        _gameController.Money = 10;

        var result = _gameController.BuyCardFromShop(tarot.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Consumable slots are full."));
            Assert.That(_gameController.Money, Is.EqualTo(10));
            Assert.That(_gameController.Shop.TarotCardOffers, Does.Contain(tarot));
        });
    }

    /// <summary>
    /// Verifies that RerollShop reduces reroll cost by two with reroll surplus voucher.
    /// </summary>
    [Test]
    public void RerollShop_WithRerollSurplusVoucher_ReducesRerollCostByTwo()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();
        _gameController.PurchasedVouchers.Add(
            new Voucher("RerollSurplus", VoucherEffect.RerollSurplus, 10));
        _gameController.Money = 10;

        var result = _gameController.RerollShop();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(7));
            Assert.That(_gameController.Shop.RerollCount, Is.EqualTo(1));
        });

        _mockShopService.Verify(
            service => service.RerollShop(_gameController.Shop, _gameController.CurrentAnte,
                _gameController.PurchasedVouchers), Times.Once);
    }

    /// <summary>
    /// Verifies that BuyBoosterPack deducts money and opens pack for valid pack.
    /// </summary>
    [Test]
    public void BuyBoosterPack_ValidPack_DeductsMoneyAndOpensPack()
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();
        var pack = new BoosterPack("Standard Pack", 4, 1, 1, BoosterType.Standard, PackSize.Normal);
        pack.PlayingCards.Add(new PlayingCard(Suit.Hearts, Rank.Ace));
        _gameController.Shop.BoosterPacks.Add(pack);
        _gameController.Money = 10;
        _mockShopService
            .Setup(service => service.OpenBoosterPack(pack, It.IsAny<List<Voucher>>(), It.IsAny<PokerHandType>()))
            .Returns(pack);

        var result = _gameController.BuyBoosterPack(pack.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.SameAs(pack));
            Assert.That(_gameController.Money, Is.EqualTo(6));
            Assert.That(_gameController.Shop.BoosterPacks, Does.Not.Contain(pack));
            Assert.That(_gameController.Shop.OpenedBoosterPack, Is.SameAs(pack));
        });
    }

    /// <summary>
    /// Verifies that SelectBoosterCard closes the pack when the maximum pick count is reached.
    /// </summary>
    [Test]
    public void SelectBoosterCard_PicksCardUntilMaxPickReached_ClosesPack()
    {
        _gameController.StartGame();
        var pack = new BoosterPack("Mega Pack", 4, 2, 2, BoosterType.Standard, PackSize.Normal);
        var firstCard = new PlayingCard(Suit.Hearts, Rank.Ace);
        var secondCard = new PlayingCard(Suit.Spades, Rank.King);
        pack.PlayingCards.Add(firstCard);
        pack.PlayingCards.Add(secondCard);
        _gameController.Shop.OpenedBoosterPack = pack;

        var firstResult = _gameController.SelectBoosterCard(firstCard.Id);
        var secondResult = _gameController.SelectBoosterCard(secondCard.Id);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True);
            Assert.That(secondResult.Success, Is.True);
            Assert.That(_gameController.DrawPile.PlayingCards, Does.Contain(firstCard));
            Assert.That(_gameController.DrawPile.PlayingCards, Does.Contain(secondCard));
            Assert.That(_gameController.Shop.OpenedBoosterPack, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that BuyVoucher applies respective permanent bonus for voucher effects.
    /// </summary>
    [TestCase(VoucherEffect.Grabber, 5, 4, 8, 1)]
    [TestCase(VoucherEffect.Wasteful, 4, 5, 8, 1)]
    [TestCase(VoucherEffect.PaintBrush, 4, 4, 9, 1)]
    [TestCase(VoucherEffect.Hieroglyph, 3, 4, 8, 1)]
    public void BuyVoucher_VoucherEffects_AppliesRespectivePermanentBonus(
        VoucherEffect effect, int expectedMaxHands, int expectedMaxDiscards, int expectedMaxHand, int expectedAnte)
    {
        _gameController.StartGame();
        _gameController.SelectBlind(1);
        _gameController.DefeatBlind();
        var voucher = new Voucher(effect.ToString(), effect, 10);
        _gameController.Shop.Voucher = voucher;
        _gameController.Money = 20;

        var result = _gameController.BuyVoucher(voucher.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(10));
            Assert.That(_gameController.MaxHands, Is.EqualTo(expectedMaxHands));
            Assert.That(_gameController.MaxDiscards, Is.EqualTo(expectedMaxDiscards));
            Assert.That(_gameController.MaxHand, Is.EqualTo(expectedMaxHand));
            Assert.That(_gameController.CurrentAnte, Is.EqualTo(expectedAnte));
            Assert.That(_gameController.PurchasedVouchers, Does.Contain(voucher));
            Assert.That(_gameController.Shop.Voucher, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that LeaveShop triggers victory for defeated ante eight boss.
    /// </summary>
    [Test]
    public void LeaveShop_DefeatedAnteEightBoss_TriggersVictory()
    {
        _gameController.StartGame();
        for (var ante = 2; ante <= 8; ante++)
        {
            _gameController.AdvanceAnte();
        }

        var boss = new Blind(BlindId.TheClub, "The Club", BlindType.Boss, 1000) { Id = 3 };
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] = boss;
        _gameController.SelectBlind(3);
        _gameController.DefeatBlind();

        var result = _gameController.LeaveShop();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Victory! You have defeated the Ante 8 Boss Blind!"));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.Victory));
        });
    }

    #endregion

    #region Consumables & Inventory Management Tests

    /// <summary>
    /// Verifies that UseConsumable executes effect and removes from deck for valid tarot card.
    /// </summary>
    [Test]
    public void UseConsumable_ValidTarotCard_ExecutesEffectAndRemovesFromDeck()
    {
        var tarot = new TarotCard("The Magician", 3, TarotType.TheMagician);
        _gameController.Deck.UsableCards.Add(tarot);
        var targetCardIds = new List<string>();
        var handlerMessage = "Tarot effect applied.";
        _mockConsumableHandler
            .Setup(handler => handler.UseTarot(_gameController, tarot, targetCardIds, out handlerMessage))
            .Returns(true);

        var result = _gameController.UseConsumable(tarot.Id, targetCardIds);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo(handlerMessage));
            Assert.That(_gameController.LastTarotUsed, Is.SameAs(tarot));
            Assert.That(_gameController.Deck.UsableCards, Does.Not.Contain(tarot));
        });

        _mockConsumableHandler.Verify(
            handler => handler.UseTarot(_gameController, tarot, targetCardIds, out handlerMessage), Times.Once);
    }

    /// <summary>
    /// Verifies that UseConsumable returns failure for card not found.
    /// </summary>
    [Test]
    public void UseConsumable_CardNotFound_ReturnsFailure()
    {
        var result = _gameController.UseConsumable("missing-consumable", new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Consumable card not found in inventory."));
        });
    }

    /// <summary>
    /// Verifies that SellCard removes joker and increases money by sell value for existing joker.
    /// </summary>
    [Test]
    public void SellCard_ExistingJoker_RemovesJokerAndIncreasesMoneyBySellValue()
    {
        var joker = new JokerCard("Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 4f, 6);
        _gameController.Deck.JokerCards.Add(joker);
        _gameController.Money = 4;

        var result = _gameController.SellCard(joker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.JokerCards, Does.Not.Contain(joker));
            Assert.That(_gameController.Money, Is.EqualTo(7), "SellValue should be half of the $6 Joker price.");
        });
    }

    /// <summary>
    /// Verifies that SellCard removes consumable and adds half price for existing consumable.
    /// </summary>
    [Test]
    public void SellCard_ExistingConsumable_RemovesConsumableAndAddsHalfPrice()
    {
        var tarot = new TarotCard("The Fool", 5, TarotType.TheFool);
        _gameController.Deck.UsableCards.Add(tarot);
        _gameController.Money = 4;

        var result = _gameController.SellCard(tarot.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.UsableCards, Does.Not.Contain(tarot));
            Assert.That(_gameController.Money, Is.EqualTo(6));
        });
    }

    /// <summary>
    /// Verifies that SellCard returns failure for card not in inventory.
    /// </summary>
    [Test]
    public void SellCard_CardNotInInventory_ReturnsFailure()
    {
        var result = _gameController.SellCard("missing-card");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Card not found in Jokers or Consumables."));
            Assert.That(_gameController.Money, Is.EqualTo(4));
        });
    }

    /// <summary>
    /// Verifies that ArrangeJokers reorders joker deck for valid order.
    /// </summary>
    [Test]
    public void ArrangeJokers_ValidOrder_ReordersJokerDeck()
    {
        var firstJoker = new JokerCard("First Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 1f, 4);
        var secondJoker = new JokerCard("Second Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 1f, 4);
        var thirdJoker = new JokerCard("Third Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 1f, 4);
        _gameController.Deck.JokerCards.AddRange(new[] { firstJoker, secondJoker, thirdJoker });

        var result = _gameController.ArrangeJokers(new List<string>
        {
            thirdJoker.Id, firstJoker.Id, secondJoker.Id
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.JokerCards,
                Is.EqualTo(new[] { thirdJoker, firstJoker, secondJoker }));
        });
    }

    /// <summary>
    /// Verifies that ArrangeJokers returns failure for count mismatch or invalid ID.
    /// </summary>
    [Test]
    public void ArrangeJokers_CountMismatchOrInvalidId_ReturnsFailure()
    {
        var firstJoker = new JokerCard("First Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 1f, 4);
        var secondJoker = new JokerCard("Second Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 1f, 4);
        _gameController.Deck.JokerCards.AddRange(new[] { firstJoker, secondJoker });
        var originalOrder = _gameController.Deck.JokerCards.ToList();

        var countMismatchResult = _gameController.ArrangeJokers(new List<string> { firstJoker.Id });
        var invalidIdResult = _gameController.ArrangeJokers(new List<string> { firstJoker.Id, "missing-joker" });

        Assert.Multiple(() =>
        {
            Assert.That(countMismatchResult.Success, Is.False);
            Assert.That(countMismatchResult.Message,
                Is.EqualTo("Must provide all existing Joker IDs in the desired order."));
            Assert.That(invalidIdResult.Success, Is.False);
            Assert.That(invalidIdResult.Message, Is.EqualTo("Joker with ID missing-joker not found."));
            Assert.That(_gameController.Deck.JokerCards, Is.EqualTo(originalOrder));
        });
    }

    #endregion
    
    #region Voucher Branches

    /// <summary>
    /// Verifies that BuyVoucher fails outside the shop, without an offer, with a wrong ID, or without
    /// sufficient money.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void BuyVoucher_WhenNotInShopMissingVoucherWrongIdOrInsufficientMoney_ReturnsFailure(int failureCase)
    {
        if (failureCase == 0)
        {
            _gameController.StartGame();
        }
        else
        {
            PrepareShopForGapTest();
            if (failureCase != 1)
            {
                _gameController.Shop.Voucher = new Voucher("Crystal Ball", VoucherEffect.CrystalBall, 10);
                if (failureCase == 3) _gameController.Money = 9;
            }
        }

        var voucherId = failureCase == 2 ? "wrong-voucher-id" :
            failureCase == 0 ? "not-in-shop-phase" :
            failureCase == 1 ? "missing-voucher" : _gameController.Shop.Voucher!.Id;
        var result = _gameController.BuyVoucher(voucherId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo(failureCase switch
            {
                0 => "Can only buy vouchers in Shop phase.",
                1 or 2 => "Voucher not available in shop.",
                _ => "Not enough money for voucher (Costs $10)."
            }));
        });
    }

    /// <summary>
    /// Verifies that BuyVoucher increases consumable slots for crystal ball.
    /// </summary>
    [Test]
    public void BuyVoucher_CrystalBall_IncreasesConsumableSlots()
    {
        PrepareShopForGapTest();
        var voucher = new Voucher("Crystal Ball", VoucherEffect.CrystalBall, 10);
        _gameController.Shop.Voucher = voucher;
        _gameController.Money = 20;
        var initialSlots = _gameController.Deck.MaxConsumableContainer;

        var result = _gameController.BuyVoucher(voucher.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.MaxConsumableContainer, Is.EqualTo(initialSlots + 1));
            Assert.That(_gameController.Money, Is.EqualTo(10));
        });
    }

    /// <summary>
    /// Verifies that utility vouchers without an immediate stat effect leave controller statistics unchanged.
    /// </summary>
    [TestCase(VoucherEffect.RerollSurplus)]
    [TestCase(VoucherEffect.SeedMoney)]
    [TestCase(VoucherEffect.Blank)]
    [TestCase(VoucherEffect.TarotMerchant)]
    [TestCase(VoucherEffect.PlanetMerchant)]
    [TestCase(VoucherEffect.MagicTrick)]
    [TestCase(VoucherEffect.DirectorsCut)]
    public void BuyVoucher_RerollSurplusSeedMoneyBlankTarotMerchantPlanetMerchantMagicTrickDirectorsCut_HasNoImmediateStatMutation(VoucherEffect effect)
    {
        PrepareShopForGapTest();
        var voucher = new Voucher(effect.ToString(), effect, 10);
        _gameController.Shop.Voucher = voucher;
        _gameController.Money = 20;
        var initialAnte = _gameController.CurrentAnte;
        var initialHands = _gameController.MaxHands;
        var initialDiscards = _gameController.MaxDiscards;
        var initialHandSize = _gameController.MaxHand;
        var initialConsumableSlots = _gameController.Deck.MaxConsumableContainer;
        var initialOfferSlots = _gameController.Shop.MaxItemCardOffers;

        var result = _gameController.BuyVoucher(voucher.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(10));
            Assert.That(_gameController.PurchasedVouchers, Does.Contain(voucher));
            Assert.That(_gameController.IsAnteVoucherPurchased, Is.True);
            Assert.That(_gameController.CurrentAnte, Is.EqualTo(initialAnte));
            Assert.That(_gameController.MaxHands, Is.EqualTo(initialHands));
            Assert.That(_gameController.MaxDiscards, Is.EqualTo(initialDiscards));
            Assert.That(_gameController.MaxHand, Is.EqualTo(initialHandSize));
            Assert.That(_gameController.Deck.MaxConsumableContainer, Is.EqualTo(initialConsumableSlots));
            Assert.That(_gameController.Shop.MaxItemCardOffers, Is.EqualTo(initialOfferSlots));
            Assert.That(_gameController.Shop.Voucher, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that BuyVoucher does not drop ante below one and reduces hands for hieroglyph at ante
    /// one.
    /// </summary>
    [Test]
    public void BuyVoucher_HieroglyphAtAnteOne_DoesNotDropAnteBelowOneAndReducesHands()
    {
        PrepareShopForGapTest();
        var voucher = new Voucher("Hieroglyph", VoucherEffect.Hieroglyph, 10);
        _gameController.Shop.Voucher = voucher;
        _gameController.Money = 20;
        var initialHands = _gameController.MaxHands;

        var result = _gameController.BuyVoucher(voucher.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.CurrentAnte, Is.EqualTo(1));
            Assert.That(_gameController.MaxHands, Is.EqualTo(initialHands - 1));
            Assert.That(_gameController.Money, Is.EqualTo(10));
        });
    }

    /// <summary>
    /// Verifies that BuyVoucher increases maximum hand for paint brush.
    /// </summary>
    [Test]
    public void BuyVoucher_PaintBrush_IncreasesMaxHand()
    {
        PrepareShopForGapTest();
        var voucher = new Voucher("Paint Brush", VoucherEffect.PaintBrush, 10);
        _gameController.Shop.Voucher = voucher;
        _gameController.Money = 20;
        var initialHandSize = _gameController.MaxHand;

        var result = _gameController.BuyVoucher(voucher.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.MaxHand, Is.EqualTo(initialHandSize + 1));
            Assert.That(_gameController.Money, Is.EqualTo(10));
        });
    }

    #endregion

    #region Booster Pack Selection and Skipping

    /// <summary>
    /// Verifies that BuyBoosterPack fails outside the shop, for an unknown ID, or with insufficient money.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void BuyBoosterPack_WhenNotInShopUnknownIdOrInsufficientMoney_ReturnsFailure(int failureCase)
    {
        if (failureCase == 0)
        {
            _gameController.StartGame();
        }
        else
        {
            PrepareShopForGapTest();
            var pack = new BoosterPack("Standard Pack", 5, 1, 1, BoosterType.Standard, PackSize.Normal);
            _gameController.Shop.BoosterPacks.Add(pack);
            if (failureCase == 2) _gameController.Money = 4;
        }

        var boosterId = failureCase == 0 ? "not-in-shop-phase" :
            failureCase == 1 ? "unknown-pack" : _gameController.Shop.BoosterPacks[0].Id;
        var result = _gameController.BuyBoosterPack(boosterId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo(failureCase switch
            {
                0 => "Can only buy booster packs in Shop phase.",
                1 => "Booster pack not found in shop.",
                _ => "Not enough money (Costs $5)."
            }));
        });
    }

    /// <summary>
    /// Verifies that SelectBoosterCard returns failure when no pack open.
    /// </summary>
    [Test]
    public void SelectBoosterCard_WhenNoPackOpen_ReturnsFailure()
    {
        var result = _gameController.SelectBoosterCard("missing-card");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("No booster pack is currently opened."));
        });
    }

    /// <summary>
    /// Verifies that SelectBoosterCard returns failure for joker slots full.
    /// </summary>
    [Test]
    public void SelectBoosterCard_JokerSlotsFull_ReturnsFailure()
    {
        _gameController.StartGame();
        for (var i = 0; i < 5; i++)
            _gameController.Deck.JokerCards.Add(new JokerCard($"Joker {i}", JokerEdition.Base,
                JokerRarity.Common, JokerModifierType.AdditionMultiplier, 1, 4));
        var joker = new JokerCard("Booster Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 1, 4);
        var pack = new BoosterPack("Buffoon Pack", 4, 1, 1, BoosterType.Buffoon, PackSize.Normal);
        pack.JokerCards.Add(joker);
        _gameController.Shop.OpenedBoosterPack = pack;

        var result = _gameController.SelectBoosterCard(joker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Joker slots are full."));
            Assert.That(pack.JokerCards, Does.Contain(joker));
        });
    }

    /// <summary>
    /// Verifies that SelectBoosterCard succeeds for negative joker with full slots.
    /// </summary>
    [Test]
    public void SelectBoosterCard_NegativeJokerWithFullSlots_Succeeds()
    {
        _gameController.StartGame();
        for (var i = 0; i < 5; i++)
            _gameController.Deck.JokerCards.Add(new JokerCard($"Joker {i}", JokerEdition.Base,
                JokerRarity.Common, JokerModifierType.AdditionMultiplier, 1, 4));
        var joker = new JokerCard("Negative Joker", JokerEdition.Negative, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 1, 4);
        var pack = new BoosterPack("Buffoon Pack", 4, 1, 1, BoosterType.Buffoon, PackSize.Normal);
        pack.JokerCards.Add(joker);
        _gameController.Shop.OpenedBoosterPack = pack;

        var result = _gameController.SelectBoosterCard(joker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.JokerCards, Does.Contain(joker));
            Assert.That(_gameController.Shop.OpenedBoosterPack, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that selecting a Tarot, Planet, or Spectral card adds it to consumables when slots allow.
    /// </summary>
    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    public void SelectBoosterCard_TarotPlanetOrSpectral_AddsToConsumables(int cardType, bool slotsFull)
    {
        _gameController.StartGame();
        var pack = new BoosterPack("Consumable Pack", 4, 1, 1, BoosterType.Arcana, PackSize.Normal);
        string cardId;
        switch (cardType)
        {
            case 0:
                var tarot = new TarotCard("The Fool", 3, TarotType.TheFool);
                pack.TarotCards.Add(tarot);
                cardId = tarot.Id;
                break;
            case 1:
                var planet = PlanetCard.CreateForHand(PokerHandType.Pair);
                pack.PlanetCards.Add(planet);
                cardId = planet.Id;
                break;
            default:
                var spectral = new SpectralCard("Sigil", 4, SpectralType.Sigil);
                pack.SpectralCards.Add(spectral);
                cardId = spectral.Id;
                break;
        }
        if (slotsFull)
        {
            _gameController.Deck.UsableCards.Add(new TarotCard("Existing 1", 3, TarotType.TheFool));
            _gameController.Deck.UsableCards.Add(new TarotCard("Existing 2", 3, TarotType.TheMagician));
        }
        _gameController.Shop.OpenedBoosterPack = pack;

        var result = _gameController.SelectBoosterCard(cardId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(!slotsFull));
            if (slotsFull)
            {
                Assert.That(result.Message, Is.EqualTo("Consumable slots are full."));
                Assert.That(_gameController.Deck.UsableCards, Has.Count.EqualTo(2));
            }
            else
            {
                Assert.That(_gameController.Deck.UsableCards, Has.Count.EqualTo(1));
                Assert.That(_gameController.Shop.OpenedBoosterPack, Is.Null);
            }
        });
    }

    /// <summary>
    /// Verifies that SelectBoosterCard adds to draw pile for playing card.
    /// </summary>
    [Test]
    public void SelectBoosterCard_PlayingCard_AddsToDrawPile()
    {
        _gameController.StartGame();
        var card = new PlayingCard(Suit.Hearts, Rank.Ace);
        var pack = new BoosterPack("Standard Pack", 4, 1, 1, BoosterType.Standard, PackSize.Normal);
        pack.PlayingCards.Add(card);
        _gameController.Shop.OpenedBoosterPack = pack;

        var result = _gameController.SelectBoosterCard(card.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.DrawPile.PlayingCards, Does.Contain(card));
            Assert.That(_gameController.Shop.OpenedBoosterPack, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that SelectBoosterCard returns failure for unknown ID.
    /// </summary>
    [Test]
    public void SelectBoosterCard_UnknownId_ReturnsFailure()
    {
        _gameController.StartGame();
        var pack = new BoosterPack("Standard Pack", 4, 1, 1, BoosterType.Standard, PackSize.Normal);
        _gameController.Shop.OpenedBoosterPack = pack;

        var result = _gameController.SelectBoosterCard("unknown-card");

        Assert.That(result.Message, Is.EqualTo("Card not found in opened booster pack."));
        Assert.That(result.Success, Is.False);
    }

    /// <summary>
    /// Verifies that SelectBoosterCard keeps pack open for first pick below quota.
    /// </summary>
    [Test]
    public void SelectBoosterCard_FirstPickBelowQuota_KeepsPackOpen()
    {
        _gameController.StartGame();
        var first = new PlayingCard(Suit.Hearts, Rank.Ace);
        var second = new PlayingCard(Suit.Spades, Rank.King);
        var pack = new BoosterPack("Mega Pack", 4, 2, 2, BoosterType.Standard, PackSize.Normal);
        pack.PlayingCards.AddRange(new[] { first, second });
        _gameController.Shop.OpenedBoosterPack = pack;

        var result = _gameController.SelectBoosterCard(first.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(pack.MaxPick, Is.EqualTo(1));
            Assert.That(_gameController.Shop.OpenedBoosterPack, Is.SameAs(pack));
            Assert.That(pack.PlayingCards, Does.Contain(second));
        });
    }

    /// <summary>
    /// Verifies that SkipBoosterPack returns success without mutation when no pack open.
    /// </summary>
    [Test]
    public void SkipBoosterPack_WhenNoPackOpen_ReturnsSuccessWithoutMutation()
    {
        var result = _gameController.SkipBoosterPack();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("No booster pack opened."));
            Assert.That(_gameController.Shop.OpenedBoosterPack, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that SkipBoosterPack closes pack when pack open.
    /// </summary>
    [Test]
    public void SkipBoosterPack_WhenPackOpen_ClosesPack()
    {
        _gameController.StartGame();
        _gameController.Shop.OpenedBoosterPack = new BoosterPack("Standard Pack", 4, 1, 1,
            BoosterType.Standard, PackSize.Normal);

        var result = _gameController.SkipBoosterPack();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Booster pack skipped."));
            Assert.That(_gameController.Shop.OpenedBoosterPack, Is.Null);
        });
    }

    #endregion

    #region Shop Card Purchase Branches

    /// <summary>
    /// Verifies that BuyCardFromShop adds consumable and deducts money for affordable tarot.
    /// </summary>
    [Test]
    public void BuyCardFromShop_AffordableTarot_AddsConsumableAndDeductsMoney()
    {
        PrepareShopForGapTest();
        var tarot = new TarotCard("The Fool", 3, TarotType.TheFool);
        _gameController.Shop.TarotCardOffers.Add(tarot);
        _gameController.Money = 10;

        var result = _gameController.BuyCardFromShop(tarot.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(7));
            Assert.That(_gameController.Deck.UsableCards, Does.Contain(tarot));
            Assert.That(_gameController.Shop.TarotCardOffers, Does.Not.Contain(tarot));
        });
    }

    /// <summary>
    /// Verifies that buying a Tarot card fails with insufficient money or full consumable slots.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    public void BuyCardFromShop_TarotInsufficientMoneyOrFullSlots_ReturnsFailure(int failureCase)
    {
        PrepareShopForGapTest();
        var tarot = new TarotCard("The Fool", 3, TarotType.TheFool);
        _gameController.Shop.TarotCardOffers.Add(tarot);
        _gameController.Money = failureCase == 0 ? 2 : 10;
        if (failureCase == 1)
        {
            _gameController.Deck.UsableCards.Add(new TarotCard("The Magician", 3, TarotType.TheMagician));
            _gameController.Deck.UsableCards.Add(new TarotCard("The Empress", 3, TarotType.TheEmpress));
        }

        var result = _gameController.BuyCardFromShop(tarot.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo(failureCase == 0 ? "Not enough money." : "Consumable slots are full."));
            Assert.That(_gameController.Shop.TarotCardOffers, Does.Contain(tarot));
        });
    }

    /// <summary>
    /// Verifies that BuyCardFromShop adds consumable and deducts money for affordable planet.
    /// </summary>
    [Test]
    public void BuyCardFromShop_AffordablePlanet_AddsConsumableAndDeductsMoney()
    {
        PrepareShopForGapTest();
        var planet = PlanetCard.CreateForHand(PokerHandType.Flush);
        planet.Price = 4;
        _gameController.Shop.PlanetCardOffers.Add(planet);
        _gameController.Money = 10;

        var result = _gameController.BuyCardFromShop(planet.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(6));
            Assert.That(_gameController.Deck.UsableCards, Does.Contain(planet));
            Assert.That(_gameController.Shop.PlanetCardOffers, Does.Not.Contain(planet));
        });
    }

    /// <summary>
    /// Verifies that buying a Planet card fails with insufficient money or full consumable slots.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    public void BuyCardFromShop_PlanetInsufficientMoneyOrFullSlots_ReturnsFailure(int failureCase)
    {
        PrepareShopForGapTest();
        var planet = PlanetCard.CreateForHand(PokerHandType.Flush);
        planet.Price = 4;
        _gameController.Shop.PlanetCardOffers.Add(planet);
        _gameController.Money = failureCase == 0 ? 3 : 10;
        if (failureCase == 1)
        {
            _gameController.Deck.UsableCards.Add(new TarotCard("The Fool", 3, TarotType.TheFool));
            _gameController.Deck.UsableCards.Add(new TarotCard("The Magician", 3, TarotType.TheMagician));
        }

        var result = _gameController.BuyCardFromShop(planet.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo(failureCase == 0 ? "Not enough money." : "Consumable slots are full."));
            Assert.That(_gameController.Shop.PlanetCardOffers, Does.Contain(planet));
        });
    }

    /// <summary>
    /// Verifies that BuyCardFromShop adds consumable and deducts money for affordable spectral.
    /// </summary>
    [Test]
    public void BuyCardFromShop_AffordableSpectral_AddsConsumableAndDeductsMoney()
    {
        PrepareShopForGapTest();
        var spectral = new SpectralCard("Sigil", 4, SpectralType.Sigil);
        _gameController.Shop.SpectralCardOffers.Add(spectral);
        _gameController.Money = 10;

        var result = _gameController.BuyCardFromShop(spectral.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(6));
            Assert.That(_gameController.Deck.UsableCards, Does.Contain(spectral));
            Assert.That(_gameController.Shop.SpectralCardOffers, Does.Not.Contain(spectral));
        });
    }

    /// <summary>
    /// Verifies that buying a Spectral card fails with insufficient money or full consumable slots.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    public void BuyCardFromShop_SpectralInsufficientMoneyOrFullSlots_ReturnsFailure(int failureCase)
    {
        PrepareShopForGapTest();
        var spectral = new SpectralCard("Sigil", 4, SpectralType.Sigil);
        _gameController.Shop.SpectralCardOffers.Add(spectral);
        _gameController.Money = failureCase == 0 ? 3 : 10;
        if (failureCase == 1)
        {
            _gameController.Deck.UsableCards.Add(new TarotCard("The Fool", 3, TarotType.TheFool));
            _gameController.Deck.UsableCards.Add(new TarotCard("The Magician", 3, TarotType.TheMagician));
        }

        var result = _gameController.BuyCardFromShop(spectral.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo(failureCase == 0 ? "Not enough money." : "Consumable slots are full."));
            Assert.That(_gameController.Shop.SpectralCardOffers, Does.Contain(spectral));
        });
    }

    /// <summary>
    /// Verifies that BuyCardFromShop adds to draw pile and raises event for affordable playing card.
    /// </summary>
    [Test]
    public void BuyCardFromShop_AffordablePlayingCard_AddsToDrawPileAndRaisesEvent()
    {
        PrepareShopForGapTest();
        var playingCard = new PlayingCard(Suit.Hearts, Rank.Ace) { Price = 2 };
        _gameController.Shop.PlayingCardOffers.Add(playingCard);
        _gameController.Money = 10;
        PlayingCard? addedCard = null;
        _gameController.OnAddPlayingCard += card => addedCard = card;

        var result = _gameController.BuyCardFromShop(playingCard.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.EqualTo(8));
            Assert.That(_gameController.DrawPile.PlayingCards, Does.Contain(playingCard));
            Assert.That(_gameController.Shop.PlayingCardOffers, Does.Not.Contain(playingCard));
            Assert.That(addedCard, Is.SameAs(playingCard));
        });
    }

    /// <summary>
    /// Verifies that buying a Playing Card fails when the player has insufficient money.
    /// </summary>
    [Test]
    public void BuyCardFromShop_PlayingCardInsufficientMoney_ReturnsFailure()
    {
        PrepareShopForGapTest();
        var playingCard = new PlayingCard(Suit.Hearts, Rank.Ace) { Price = 2 };
        _gameController.Shop.PlayingCardOffers.Add(playingCard);
        _gameController.Money = 1;

        var result = _gameController.BuyCardFromShop(playingCard.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Not enough money."));
            Assert.That(_gameController.Shop.PlayingCardOffers, Does.Contain(playingCard));
            Assert.That(_gameController.DrawPile.PlayingCards, Does.Not.Contain(playingCard));
        });
    }

    /// <summary>
    /// Verifies that BuyCardFromShop returns failure for unknown offer ID.
    /// </summary>
    [Test]
    public void BuyCardFromShop_UnknownOfferId_ReturnsFailure()
    {
        PrepareShopForGapTest();

        var result = _gameController.BuyCardFromShop("unknown-offer");

        Assert.That(result.Message, Is.EqualTo("Card offer not found in shop."));
        Assert.That(result.Success, Is.False);
    }

    /// <summary>
    /// Verifies that RerollShop returns failure when not in shop or insufficient money.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    public void RerollShop_WhenNotInShopOrInsufficientMoney_ReturnsFailure(int failureCase)
    {
        if (failureCase == 0)
        {
            _gameController.StartGame();
        }
        else
        {
            PrepareShopForGapTest();
            _gameController.Money = 4;
        }

        var result = _gameController.RerollShop();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo(failureCase == 0
                ? "Can only reroll shop in Shop phase."
                : "Not enough money to reroll (Costs $5)."));
        });
    }

    /// <summary>
    /// Verifies that RerollShop uses effective cost with clearance and chaos branches.
    /// </summary>
    [Test]
    public void RerollShop_WithClearanceAndChaosBranches_UsesEffectiveCost()
    {
        PrepareShopForGapTest();
        _gameController.PurchasedVouchers.Add(new Voucher("Clearance Sale", VoucherEffect.ClearanceSale, 10));
        _gameController.Deck.JokerCards.Add(new JokerCard(JokerId.ChaosTheClown, "Chaos the Clown",
            JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4));
        _gameController.Money = 5;

        var result = _gameController.RerollShop();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Money, Is.Zero);
            Assert.That(_gameController.Shop.RerollCount, Is.EqualTo(1));
            Assert.That(result.Message, Does.Contain("$5"));
        });
        _mockShopService.Verify(s => s.RerollShop(_gameController.Shop, _gameController.CurrentAnte,
            _gameController.PurchasedVouchers), Times.Once);
    }

    private void PrepareShopForGapTest()
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);
        Assert.That(_gameController.DefeatBlind(), Is.True);
    }

    #endregion

    #region Consumable Dispatch and Arrangement

    /// <summary>
    /// Verifies that UseConsumable keeps card and history for tarot handler failure.
    /// </summary>
    [Test]
    public void UseConsumable_TarotHandlerFailure_KeepsCardAndHistory()
    {
        var previous = new TarotCard("The Magician", 3, TarotType.TheMagician);
        var tarot = new TarotCard("The Empress", 3, TarotType.TheEmpress);
        var targetIds = new List<string>();
        _gameController.LastTarotUsed = previous;
        _gameController.Deck.UsableCards.Add(tarot);
        var message = "Tarot failed.";
        _mockConsumableHandler.Setup(h => h.UseTarot(_gameController, tarot, targetIds, out message))
            .Returns(false);

        var result = _gameController.UseConsumable(tarot.Id, targetIds);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(_gameController.Deck.UsableCards, Does.Contain(tarot));
            Assert.That(_gameController.LastTarotUsed, Is.SameAs(previous));
        });
    }

    /// <summary>
    /// Verifies that UseConsumable does not replace last tarot used for the fool success.
    /// </summary>
    [Test]
    public void UseConsumable_TheFoolSuccess_DoesNotReplaceLastTarotUsed()
    {
        var previous = new TarotCard("The Magician", 3, TarotType.TheMagician);
        var fool = new TarotCard("The Fool", 3, TarotType.TheFool);
        var targetIds = new List<string>();
        _gameController.LastTarotUsed = previous;
        _gameController.Deck.UsableCards.Add(fool);
        var message = "The Fool created The Magician!";
        _mockConsumableHandler.Setup(h => h.UseTarot(_gameController, fool, targetIds, out message))
            .Returns(true);

        var result = _gameController.UseConsumable(fool.Id, targetIds);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.UsableCards, Does.Not.Contain(fool));
            Assert.That(_gameController.LastTarotUsed, Is.SameAs(previous));
        });
    }

    /// <summary>
    /// Verifies that UseConsumable stores history and removes card for planet success.
    /// </summary>
    [Test]
    public void UseConsumable_PlanetSuccess_StoresHistoryAndRemovesCard()
    {
        var planet = PlanetCard.CreateForHand(PokerHandType.Flush);
        _gameController.Deck.UsableCards.Add(planet);
        var message = "Upgraded Flush!";
        _mockConsumableHandler.Setup(h => h.UsePlanet(_gameController, planet, out message))
            .Returns(true);

        var result = _gameController.UseConsumable(planet.Id, new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.UsableCards, Does.Not.Contain(planet));
            Assert.That(_gameController.LastPlanetUsed, Is.SameAs(planet));
        });
    }

    /// <summary>
    /// Verifies that UseConsumable keeps card and history for planet failure.
    /// </summary>
    [Test]
    public void UseConsumable_PlanetFailure_KeepsCardAndHistory()
    {
        var previous = PlanetCard.CreateForHand(PokerHandType.Pair);
        var planet = PlanetCard.CreateForHand(PokerHandType.Flush);
        _gameController.LastPlanetUsed = previous;
        _gameController.Deck.UsableCards.Add(planet);
        var message = "Planet failed.";
        _mockConsumableHandler.Setup(h => h.UsePlanet(_gameController, planet, out message))
            .Returns(false);

        var result = _gameController.UseConsumable(planet.Id, new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(_gameController.Deck.UsableCards, Does.Contain(planet));
            Assert.That(_gameController.LastPlanetUsed, Is.SameAs(previous));
        });
    }

    /// <summary>
    /// Verifies that UseConsumable removes card for spectral success.
    /// </summary>
    [Test]
    public void UseConsumable_SpectralSuccess_RemovesCard()
    {
        var spectral = new SpectralCard("Sigil", 4, SpectralType.Sigil);
        _gameController.Deck.UsableCards.Add(spectral);
        var message = "Converted hand!";
        _mockConsumableHandler.Setup(h => h.UseSpectral(_gameController, spectral, out message))
            .Returns(true);

        var result = _gameController.UseConsumable(spectral.Id, new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.UsableCards, Does.Not.Contain(spectral));
        });
    }

    /// <summary>
    /// Verifies that UseConsumable keeps card for spectral failure.
    /// </summary>
    [Test]
    public void UseConsumable_SpectralFailure_KeepsCard()
    {
        var spectral = new SpectralCard("Sigil", 4, SpectralType.Sigil);
        _gameController.Deck.UsableCards.Add(spectral);
        var message = "Spectral failed.";
        _mockConsumableHandler.Setup(h => h.UseSpectral(_gameController, spectral, out message))
            .Returns(false);

        var result = _gameController.UseConsumable(spectral.Id, new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(_gameController.Deck.UsableCards, Does.Contain(spectral));
        });
    }

    /// <summary>
    /// Verifies that ArrangeConsumables reorders inventory for valid order.
    /// </summary>
    [Test]
    public void ArrangeConsumables_ValidOrder_ReordersInventory()
    {
        var first = new TarotCard("The Fool", 3, TarotType.TheFool);
        var second = PlanetCard.CreateForHand(PokerHandType.Pair);
        var third = new SpectralCard("Sigil", 4, SpectralType.Sigil);
        _gameController.Deck.UsableCards.AddRange(new IUsableCard[] { first, second, third });

        var result = _gameController.ArrangeConsumables(new List<string> { third.Id, first.Id, second.Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Deck.UsableCards,
                Is.EqualTo(new IUsableCard[] { third, first, second }));
        });
    }

    /// <summary>
    /// Verifies that ArrangeConsumables rejects null, count-mismatched, and unknown-ID input without mutation.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void ArrangeConsumables_NullCountMismatchOrUnknownId_ReturnsFailureWithoutMutation(int inputCase)
    {
        var first = new TarotCard("The Fool", 3, TarotType.TheFool);
        var second = PlanetCard.CreateForHand(PokerHandType.Pair);
        _gameController.Deck.UsableCards.AddRange(new IUsableCard[] { first, second });
        var original = _gameController.Deck.UsableCards.ToList();
        List<string>? ids = inputCase switch
        {
            0 => null,
            1 => new List<string> { first.Id },
            _ => new List<string> { first.Id, "unknown-consumable" }
        };

        var result = _gameController.ArrangeConsumables(ids!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(_gameController.Deck.UsableCards, Is.EqualTo(original));
            if (inputCase == 0 || inputCase == 1)
                Assert.That(result.Message, Is.EqualTo("Must provide all existing Consumable IDs in the desired order."));
            else
                Assert.That(result.Message, Is.EqualTo("Consumable with ID unknown-consumable not found."));
        });
    }

    #endregion

    #region Blind, Draw, and Lifecycle Gaps

    /// <summary>
    /// Verifies that GetAvailableBlinds regenerates blinds when ante cache missing.
    /// </summary>
    [Test]
    public void GetAvailableBlinds_WhenAnteCacheMissing_RegeneratesBlinds()
    {
        _gameController.StartGame();
        var ante = _gameController.CurrentAnte;
        _gameController.BlindEnemies.Remove(ante);

        var blinds = _gameController.GetAvailableBlinds();

        Assert.Multiple(() =>
        {
            Assert.That(blinds, Has.Count.EqualTo(3));
            Assert.That(_gameController.BlindEnemies.ContainsKey(ante), Is.True);
        });
    }

    /// <summary>
    /// Verifies that SelectBlind debuffs cards previously played this ante for the pillar.
    /// </summary>
    [Test]
    public void SelectBlind_ThePillar_DebuffsCardsPreviouslyPlayedThisAnte()
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);
        var playedCard = _gameController.Hand[0];
        var unplayedCard = _gameController.Hand[1];
        var scoreResult = new ScoreCalculationResultDto
        {
            HandType = PokerHandType.HighCard,
            FinalScore = 1,
            ScoringCards = new List<PlayingCard> { playedCard },
            UnscoredCards = new List<PlayingCard>()
        };
        _mockScoringService.Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(), It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(scoreResult);

        Assert.That(_gameController.PlayHand(new List<string> { playedCard.Id }).Success, Is.True);
        _gameController.BlindEnemies[_gameController.CurrentAnte][2] =
            new Blind(BlindId.ThePillar, "The Pillar", BlindType.Boss, 100) { Id = 3 };
        SetPrivateProperty(_gameController, "Phase", GameStatePhase.SelectingBlind);

        Assert.That(_gameController.SelectBlind(3), Is.True);
        var allCards = _gameController.Hand.Concat(_gameController.DrawPile.PlayingCards).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(allCards.Single(c => c.Id == playedCard.Id).IsDebuffed, Is.True);
            Assert.That(allCards.Single(c => c.Id == unplayedCard.Id).IsDebuffed, Is.False);
        });
    }

    /// <summary>
    /// Verifies that DrawCards returns empty when requested count is zero or hand full.
    /// </summary>
    [Test]
    public void DrawCards_WhenRequestedCountIsZeroOrHandFull_ReturnsEmpty()
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);

        var zeroCount = _gameController.DrawCards(0);
        var fullHand = _gameController.DrawCards(1);

        Assert.Multiple(() =>
        {
            Assert.That(zeroCount, Is.Empty);
            Assert.That(fullHand, Is.Empty);
            Assert.That(_gameController.Hand, Has.Count.EqualTo(_gameController.MaxHand));
        });
    }

    /// <summary>
    /// Verifies that DrawCards recycles discard pile when draw pile insufficient.
    /// </summary>
    [Test]
    public void DrawCards_WhenDrawPileInsufficient_RecyclesDiscardPile()
    {
        _gameController.StartGame();
        var recycledCard = new PlayingCard(Suit.Hearts, Rank.Ace);
        _gameController.DrawPile.Clear();
        _gameController.DiscardPile.DiscardCards(new[] { recycledCard });

        var drawn = _gameController.DrawCards(1);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Has.Count.EqualTo(1));
            Assert.That(drawn[0], Is.SameAs(recycledCard));
            Assert.That(_gameController.DiscardPile, Has.Property("Count").EqualTo(0));
            Assert.That(_gameController.Hand, Does.Contain(recycledCard));
        });
    }

    /// <summary>
    /// Verifies that DrawCards applies debuff to newly drawn cards while boss active.
    /// </summary>
    [Test]
    public void DrawCards_WhileBossActive_AppliesDebuffToNewlyDrawnCards()
    {
        _gameController.StartGame();
        _gameController.BlindEnemies[1][2] =
            new Blind(BlindId.TheClub, "The Club", BlindType.Boss, 100) { Id = 3 };
        Assert.That(_gameController.SelectBlind(3), Is.True);
        _gameController.Hand.Clear();
        _gameController.DrawPile.Clear();
        var club = new PlayingCard(Suit.Clubs, Rank.Ace);
        _gameController.DrawPile.AddCards(new[] { club });

        var drawn = _gameController.DrawCards(1);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Has.Count.EqualTo(1));
            Assert.That(drawn[0], Is.SameAs(club));
            Assert.That(club.IsDebuffed, Is.True);
        });
    }

    /// <summary>
    /// Verifies that DefeatBlind returns false without mutation when no current blind.
    /// </summary>
    [Test]
    public void DefeatBlind_WhenNoCurrentBlind_ReturnsFalseWithoutMutation()
    {
        var phaseBefore = _gameController.Phase;
        var moneyBefore = _gameController.Money;

        var result = _gameController.DefeatBlind();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(_gameController.Phase, Is.EqualTo(phaseBefore));
            Assert.That(_gameController.Money, Is.EqualTo(moneyBefore));
        });
    }

    /// <summary>
    /// Verifies that Cashout returns zero when no current blind.
    /// </summary>
    [Test]
    public void Cashout_WhenNoCurrentBlind_ReturnsZero()
    {
        Assert.That(_gameController.Cashout(), Is.Zero);
    }

    /// <summary>
    /// Verifies that LeaveShop returns failure when not in shop.
    /// </summary>
    [Test]
    public void LeaveShop_WhenNotInShop_ReturnsFailure()
    {
        var result = _gameController.LeaveShop();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Cannot leave shop when not in Shop phase."));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.SelectingBlind));
        });
    }

    /// <summary>
    /// Verifies that LeaveShop returns to blind selection with null current blind.
    /// </summary>
    [Test]
    public void LeaveShop_WithNullCurrentBlind_ReturnsToBlindSelection()
    {
        _gameController.StartGame();
        SetPrivateProperty(_gameController, "Phase", GameStatePhase.InShop);
        SetPrivateProperty(_gameController, "CurrentBlind", null);

        var result = _gameController.LeaveShop();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.SelectingBlind));
            Assert.That(result.Message, Is.EqualTo("Proceeding to blind selection."));
        });
    }

    /// <summary>
    /// Verifies that LeaveShop advances ante and round after boss before ante eight.
    /// </summary>
    [Test]
    public void LeaveShop_AfterBossBeforeAnteEight_AdvancesAnteAndRound()
    {
        _gameController.StartGame();
        _gameController.BlindEnemies[1][2] =
            new Blind(BlindId.TheClub, "The Club", BlindType.Boss, 1) { Id = 3 };
        Assert.That(_gameController.SelectBlind(3), Is.True);
        Assert.That(_gameController.DefeatBlind(), Is.True);

        var result = _gameController.LeaveShop();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.CurrentAnte, Is.EqualTo(2));
            Assert.That(_gameController.CurrentRound, Is.EqualTo(2));
            Assert.That(_gameController.Phase, Is.EqualTo(GameStatePhase.SelectingBlind));
            Assert.That(_gameController.GetAvailableBlinds(), Has.Count.EqualTo(3));
        });
    }

    private static void SetPrivateProperty(object target, string propertyName, object? value)
    {
        var field = target.GetType().GetField(
            $"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Backing field for {propertyName} was not found.");
        field!.SetValue(target, value);
    }

    #endregion

    #region Joker, Booster, and Remaining Branches

    /// <summary>
    /// Verifies that Ice Cream loses five chips after PlayHand without dropping below zero.
    /// </summary>
    [TestCase(100, 95)]
    [TestCase(3, 0)]
    public void PlayHand_WithIceCream_ReducesChipsValueByFiveWithZeroFloor(int initialChips, int expectedChips)
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);
        var card = _gameController.Hand[0];
        var iceCream = new JokerCard(JokerId.IceCream, "Ice Cream", JokerEdition.Base,
            JokerRarity.Common, JokerModifierType.Chips, initialChips, 5);
        _gameController.Deck.JokerCards.Add(iceCream);
        _mockScoringService.Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(), It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto
            {
                HandType = PokerHandType.HighCard,
                FinalScore = 1,
                ScoringCards = new List<PlayingCard> { card }
            });

        var result = _gameController.PlayHand(new List<string> { card.Id });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(iceCream.ChipsValue, Is.EqualTo(expectedChips));
        });
    }
    
    /// <summary>
    /// Verifies that BuyArcanaOrSpectralBooster draws temporary hand when hand empty.
    /// </summary>
    [TestCase(BoosterType.Arcana)]
    [TestCase(BoosterType.Spectral)]
    public void BuyArcanaOrSpectralBooster_WhenHandEmpty_DrawsTemporaryHand(BoosterType boosterType)
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);
        Assert.That(_gameController.DefeatBlind(), Is.True);
        _gameController.Hand.Clear();
        var pack = new BoosterPack("Consumable Pack", 0, 1, 1, boosterType, PackSize.Normal);
        _gameController.Shop.BoosterPacks.Add(pack);
        _mockShopService.Setup(s => s.OpenBoosterPack(
                pack, It.IsAny<List<Voucher>>(), It.IsAny<PokerHandType>())).Returns(pack);

        var result = _gameController.BuyBoosterPack(pack.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Hand, Has.Count.EqualTo(_gameController.MaxHand));
        });
    }

    /// <summary>
    /// Verifies that BuyNonConsumableBooster does not draw hand when hand empty.
    /// </summary>
    [Test]
    public void BuyNonConsumableBooster_WhenHandEmpty_DoesNotDrawHand()
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);
        Assert.That(_gameController.DefeatBlind(), Is.True);
        _gameController.Hand.Clear();
        var pack = new BoosterPack("Standard Pack", 0, 1, 1, BoosterType.Standard, PackSize.Normal);
        _gameController.Shop.BoosterPacks.Add(pack);
        _mockShopService.Setup(s => s.OpenBoosterPack(
                pack, It.IsAny<List<Voucher>>(), It.IsAny<PokerHandType>())).Returns(pack);

        var result = _gameController.BuyBoosterPack(pack.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_gameController.Hand, Is.Empty);
        });
    }

    /// <summary>
    /// Verifies that PlayHand initializes count to one when poker hand played dictionary missing key.
    /// </summary>
    [Test]
    public void PlayHand_WhenPokerHandPlayedDictionaryMissingKey_InitializesCountToOne()
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);
        var card = _gameController.Hand[0];
        _gameController.PokerHandPlayed.Remove(PokerHandType.HighCard);
        _mockScoringService.Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(), It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new ScoreCalculationResultDto
            {
                HandType = PokerHandType.HighCard,
                FinalScore = 1,
                ScoringCards = new List<PlayingCard> { card }
            });

        Assert.That(_gameController.PlayHand(new List<string> { card.Id }).Success, Is.True);

        Assert.That(_gameController.PokerHandPlayed[PokerHandType.HighCard], Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that DiscardCards rejects null, empty, and selections containing more than five cards.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void DiscardCards_NullEmptyOrMoreThanFive_ReturnsRangeFailure(int inputCase)
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);
        List<string>? cardIds = inputCase switch
        {
            0 => null,
            1 => new List<string>(),
            _ => new List<string> { "1", "2", "3", "4", "5", "6" }
        };

        var result = _gameController.DiscardCards(cardIds!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Must discard between 1 and 5 cards."));
        });
    }

    /// <summary>
    /// Verifies that GetScorePreview passes only resolved cards to scoring for cards not in hand.
    /// </summary>
    [Test]
    public void GetScorePreview_CardsNotInHand_PassesOnlyResolvedCardsToScoring()
    {
        _gameController.StartGame();
        Assert.That(_gameController.SelectBlind(1), Is.True);
        var resultDto = new ScoreCalculationResultDto { HandType = PokerHandType.HighCard, FinalScore = 10 };
        _mockScoringService.Setup(s => s.CalculateScore(
                It.IsAny<List<PlayingCard>>(), It.IsAny<List<PlayingCard>>(),
                It.IsAny<List<JokerCard>>(), It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(resultDto);

        var result = _gameController.GetScorePreview(new List<string> { "missing-card" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.SameAs(resultDto));
        });
        _mockScoringService.Verify(s => s.CalculateScore(
                It.Is<List<PlayingCard>>(cards => cards.Count == 0),
                It.Is<List<PlayingCard>>(cards => cards.Count == _gameController.Hand.Count),
                It.IsAny<List<JokerCard>>(), It.IsAny<Dictionary<PokerHandType, int>>(),
                It.IsAny<BlindId?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    #endregion
}
