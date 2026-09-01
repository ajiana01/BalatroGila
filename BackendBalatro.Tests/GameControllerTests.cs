using BackendBalatro.Models.Interfaces;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Moq;
using NUnit.Framework;

namespace BackendBalatro.Tests;

public class GameControllerTests
{
    private Mock<IScoringService> _mockScoringService;
    private Mock<IShopService> _mockShopService;
    private Mock<IConsumableEffectHandler> _mockConsumableHandler;

    [SetUp]
    public void Setup()
    {
        _mockScoringService = new Mock<IScoringService>();
        _mockShopService = new Mock<IShopService>();
        _mockConsumableHandler = new Mock<IConsumableEffectHandler>();
    }

    [Test]
    public void PlayHand_ValidCardsSelected_ReturnsSuccessResult()
    {
        var controller = new GameController(
            _mockScoringService.Object,
            _mockShopService.Object,
            _mockConsumableHandler.Object);
        controller.StartGame();
        controller.SelectBlind(controller.BlindEnemies[1][0].Id);
        var cardIdsToPlay = controller.Hand.Take(5).Select(c => c.Id).ToList();

        var result = controller.PlayHand(cardIdsToPlay);

        Assert.That(result.Success, Is.True);
    }
}