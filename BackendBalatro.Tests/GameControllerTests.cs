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
}
