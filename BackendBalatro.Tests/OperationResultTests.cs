using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class OperationResultTests
{
    private readonly IGameController _controller;

    public OperationResultTests()
    {
        var evaluator = new PokerHandEvaluator();
        IScoringService scoringService = new ScoringService(evaluator);
        IShopService shopService = new ShopService();
        IConsumableEffectHandler consumableHandler = new ConsumableEffectHandler();

        _controller = new GameController(scoringService, shopService, consumableHandler);
        _controller.StartGame();
    }

    [Fact]
    public void OperationResult_OkAndFail_SetPropertiesCorrectly()
    {
        var okResult = OperationResult.Ok("Action succeeded");
        Assert.True(okResult.Success);
        Assert.Equal("Action succeeded", okResult.Message);

        var failResult = OperationResult.Fail("Action failed");
        Assert.False(failResult.Success);
        Assert.Equal("Action failed", failResult.Message);
    }

    [Fact]
    public void OperationResultGeneric_OkAndFail_SetPropertiesCorrectly()
    {
        var dummyScore = new ScoreCalculationResultDto { FinalScore = 150 };
        var okResult = OperationResult<ScoreCalculationResultDto>.Ok(dummyScore, "Score calculated");
        
        Assert.True(okResult.Success);
        Assert.Equal("Score calculated", okResult.Message);
        Assert.NotNull(okResult.Data);
        Assert.Equal(150, okResult.Data.FinalScore);
        Assert.Equal(okResult.Data, okResult.Result);
        Assert.Equal(okResult.Data, okResult.Value);

        var failResult = OperationResult<ScoreCalculationResultDto>.Fail("Calculation error");
        Assert.False(failResult.Success);
        Assert.Equal("Calculation error", failResult.Message);
        Assert.Null(failResult.Data);
    }

    [Fact]
    public void OperationResult_Deconstruct_WorksSeamlessly()
    {
        var result = OperationResult.Ok("Custom message");
        var (success, msg) = result;

        Assert.True(success);
        Assert.Equal("Custom message", msg);

        var genericResult = OperationResult<string>.Ok("Payload", "Payload message");
        var (gSuccess, gMsg, gData) = genericResult;

        Assert.True(gSuccess);
        Assert.Equal("Payload message", gMsg);
        Assert.Equal("Payload", gData);
    }

    [Fact]
    public void GameController_MethodsReturnOperationResult_Instances()
    {
        // Select Blind to allow playing
        _controller.SelectBlind(1);

        // Play Hand returns OperationResult<ScoreCalculationResultDto>
        var cardIds = _controller.Hand.Take(5).Select(c => c.Id).ToList();
        OperationResult<ScoreCalculationResultDto> playRes = _controller.PlayHand(cardIds);
        
        Assert.True(playRes.Success);
        Assert.NotNull(playRes.Data);
        Assert.NotNull(playRes.Result);
        Assert.True(playRes.Data.FinalScore > 0);

        // DiscardCards returns OperationResult
        var discardCards = _controller.Hand.Take(1).Select(c => c.Id).ToList();
        OperationResult discardRes = _controller.DiscardCards(discardCards);
        Assert.True(discardRes.Success);
        Assert.False(string.IsNullOrWhiteSpace(discardRes.Message));

        // GetScorePreview returns OperationResult<ScoreCalculationResultDto>
        var previewCards = _controller.Hand.Take(2).Select(c => c.Id).ToList();
        OperationResult<ScoreCalculationResultDto> previewRes = _controller.GetScorePreview(previewCards);
        Assert.True(previewRes.Success);
        Assert.NotNull(previewRes.Data);
    }
}
