using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Moq;

namespace BackendBalatro.Tests;

[TestFixture]
public class GameController_LifecycleTests
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
        // Arrange
        var fakeVoucher = new Voucher("Overstock", VoucherEffect.Overstock, 10, "Extra shop slot");
        _mockShopService
            .Setup(s => s.GenerateVoucherForAnte(1, It.IsAny<List<Voucher>>()))
            .Returns(fakeVoucher);

        // Act
        var result = _gameController.StartGame();

        // Assert
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
        // Arrange
        var allHandTypes = Enum.GetValues<PokerHandType>();

        // Act
        _gameController.StartGame();

        // Assert
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
}