using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Moq;

namespace BackendBalatro.Tests;

[TestFixture]
public class GameControllerTests
{
    private GameController _gameController;
    private Mock<IScoringService> _mockScoringService;
    private Mock<IShopService> _mockShopService;
    private Mock<IConsumableEffectHandler> _mockConsumableHandler;

    [SetUp]
    public void Setup()
    {
        _mockScoringService = new Mock<IScoringService>();
        _mockShopService = new Mock<IShopService>();
        _mockConsumableHandler = new Mock<IConsumableEffectHandler>();

        _gameController = new GameController(
            _mockScoringService.Object,
            _mockShopService.Object,
            _mockConsumableHandler.Object);
    }

    #region Game Lifecycle Tests

    [Test]
    [Description("TC-1.1: Memulai permainan baru dan memverifikasi default state serta 52 kartu di DrawPile")]
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

    [Test]
    [Description("TC-1.2: Memverifikasi seluruh tipe poker hand memiliki level awal 1 dan jumlah dimainkan 0")]
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

    [Test]
    [Description("TC-1.3: Game di-restart saat ada sisa kartu/voucher dari sesi sebelumnya dan memastikan seluruh state dibersihkan")]
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

    [Test]
    [Description("TC-1.4: Memanggil Win() dan memverifikasi perubahan phase ke Victory serta event OnWinGame terpicu")]
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

    [Test]
    [Description("TC-1.5: Memanggil GameOver() dan memverifikasi perubahan phase ke GameOver serta event OnGameOver terpicu")]
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

    [Test]
    [Description("TC-1.6: Pindah ke Ante berikutnya, memverifikasi kenaikan Ante, reset voucher & reroll flags, pembuatan blind baru, dan trigger event OnAnteAdvance")]
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

    [Test]
    [Description("TC-1.7: Memanggil GetGameState() saat fase Shop dan memverifikasi DTO berisi data session, uang, deck, serta shop offers")]
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

    [Test]
    [Description("TC-1.8: Memverifikasi saat ShopService mengembalikan null untuk voucher, StartGame tetap berjalan sukses dan CurrentAnteVoucher bernilai null")]
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

    [Test]
    [Description("TC-1.9: Memverifikasi skor blind pada Endless mode menggunakan formula eksponensial setelah Ante 8")]
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

    [Test]
    [Description("TC-1.10: Memverifikasi GetGameState tetap menghasilkan DTO valid saat seluruh koleksi shop bernilai null")]
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

    [Test]
    [Description("TC-1.11: Memverifikasi state GameOver bersifat terminal dan tidak dapat berubah menjadi Victory")]
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

    [Test]
    [Description("TC-2.1: Memverifikasi pemilihan Small Blind mengubah phase, mengisi CurrentBlind, membagikan 8 kartu, dan memicu event")]
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

    [Test]
    [Description("TC-2.2: Memverifikasi kartu dari ronde sebelumnya dikembalikan ke DrawPile dan seluruh debuff dihapus")]
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

    [TestCase(999)]
    [TestCase(-1)]
    [Description("TC-2.3: Memverifikasi ID blind yang tidak valid ditolak tanpa mengubah state game")]
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
    [Test]
    [Description("TC-2.4: Memverifikasi blind yang sudah dikalahkan tidak dapat dipilih kembali")]
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

    [TestCase(GameStatePhase.Playing)]
    [TestCase(GameStatePhase.InShop)]
    [Description("TC-2.5: Memverifikasi SelectBlind ditolak saat game tidak berada pada fase SelectingBlind")]
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

    [Test]
    [Description("TC-2.6: Memverifikasi DirectorsCut dan uang yang cukup memungkinkan reroll Boss Blind")]
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

    [Test]
    [Description("TC-2.7: Memverifikasi reroll boss ditolak tanpa voucher DirectorsCut")]
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

    [Test]
    [Description("TC-2.8: Memverifikasi reroll boss kedua dalam ante yang sama ditolak")]
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

    [Test]
    [Description("TC-2.9: Memverifikasi reroll boss ditolak saat uang kurang dari $10")]
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

    [Test]
    [Description("TC-3.1: Memverifikasi PlayHand menghitung score, mengurangi hand, dan menarik kartu pengganti")]
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

    [Test]
    [Description("TC-3.2: Memverifikasi score yang mencapai target mengalahkan blind dan membuka shop")]
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

    [Test]
    [Description("TC-3.3: Memverifikasi bonus uang dari Lucky Card ditambahkan ke saldo pemain")]
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

    [Test]
    [Description("TC-3.4: Memverifikasi kartu Glass yang shatter dihapus dan kartu yang selamat masuk DiscardPile")]
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
                _mockConsumableHandler.Object);
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

    [TestCase(GameStatePhase.SelectingBlind)]
    [TestCase(GameStatePhase.InShop)]
    [Description("TC-3.5: Memverifikasi PlayHand ditolak saat game tidak berada pada fase Playing")]
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

    [Test]
    [Description("TC-3.6: Memverifikasi PlayHand menolak parameter null maupun list kartu kosong")]
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

    [Test]
    [Description("TC-3.7: Memverifikasi PlayHand menolak enam kartu atau lebih")]
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

    [Test]
    [Description("TC-3.8: Memverifikasi PlayHand menolak ID kartu yang tidak terdapat di tangan")]
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

    [Test]
    [Description("TC-3.9: Memverifikasi hand terakhir yang tidak mencapai target memicu GameOver")]
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

    [Test]
    [Description("TC-4.1: Memverifikasi The Psychic menolak permainan kurang dari 5 kartu")]
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

    [Test]
    [Description("TC-4.2: Memverifikasi The Eye menolak tipe poker hand yang sama pada ronde yang sama")]
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

    [Test]
    [Description("TC-4.3: Memverifikasi The Mouth menolak tipe poker hand berbeda pada ronde yang sama")]
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

    [Test]
    [Description("TC-4.4: Memverifikasi The Needle membatasi kesempatan bermain menjadi satu")]
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

    [Test]
    [Description("TC-4.5: Memverifikasi The Water mengatur jumlah discard awal menjadi nol")]
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

    [Test]
    [Description("TC-4.6: Memverifikasi The Manacle mengurangi ukuran hand efektif sebanyak satu kartu")]
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

    [Test]
    [Description("TC-4.7: Memverifikasi The Arm menurunkan level poker hand yang dimainkan sebanyak satu")]
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

    [Test]
    [Description("TC-4.8: Memverifikasi The Tooth memotong satu dolar untuk setiap kartu yang dimainkan")]
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

    [Test]
    [Description("TC-4.9: Memverifikasi The Ox mereset uang saat memainkan poker hand yang paling sering dimainkan")]
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

    [Test]
    [Description("TC-4.10: Memverifikasi The Hook membuang dua kartu acak dari sisa hand")]
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

    [TestCase(BlindId.TheClub, Suit.Clubs, Rank.Ace)]
    [TestCase(BlindId.TheGoad, Suit.Spades, Rank.Ace)]
    [TestCase(BlindId.TheWindow, Suit.Diamonds, Rank.Ace)]
    [TestCase(BlindId.TheHead, Suit.Hearts, Rank.Ace)]
    [TestCase(BlindId.ThePlant, Suit.Hearts, Rank.Jack)]
    [Description("TC-4.11: Memverifikasi boss suit/face debuff diterapkan pada kartu yang sesuai")]
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

    [Test]
    [Description("TC-4.12: Memverifikasi penjualan Joker pada Verdant Leaf mencabut seluruh debuff kartu")]
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

    [Test]
    [Description("TC-5.1: Memverifikasi discard kartu valid mengurangi discard dan menarik kartu pengganti")]
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

    [Test]
    [Description("TC-5.2: Memverifikasi discard ditolak saat seluruh kesempatan discard telah habis")]
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

    [TestCase(GameStatePhase.SelectingBlind)]
    [TestCase(GameStatePhase.InShop)]
    [Description("TC-5.3: Memverifikasi discard ditolak saat game tidak berada pada fase Playing")]
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

    [Test]
    [Description("TC-5.4: Memverifikasi discard ditolak untuk ID kartu yang tidak ada di tangan")]
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

    [Test]
    [Description("TC-5.5: Memverifikasi preview score valid tidak mengubah state permainan")]
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

    [Test]
    [Description("TC-5.6: Memverifikasi preview score menolak input kosong dan lebih dari lima kartu")]
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

    [Test]
    [Description("TC-6.1: Memverifikasi cashout menghitung reward, sisa hand, dan interest")]
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

    [Test]
    [Description("TC-6.2: Memverifikasi voucher SeedMoney menaikkan batas interest menjadi sepuluh dolar")]
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

    [Test]
    [Description("TC-6.3: Memverifikasi dua Gold Card yang tidak didebuff memberikan bonus enam dolar")]
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

    [Test]
    [Description("TC-6.4: Memverifikasi efek akhir ronde GoldenJoker dan Popcorn")]
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
    

    
    #endregion

    #region Consumables & Inventory Management Tests

    

    #endregion
}
