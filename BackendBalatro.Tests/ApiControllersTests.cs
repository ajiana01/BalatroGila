using BackendBalatro.Controllers;
using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Sessions;
using BackendBalatro.Services.Shop;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BackendBalatro.Tests;

public class ApiControllersTests
{
    private readonly GameSessionService _sessionService;
    private readonly GameController _gameController;
    private readonly ActionController _actionController;
    private readonly ShopController _shopController;

    public ApiControllersTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();

        _sessionService = new GameSessionService(scoring, shopService, consumableHandler);

        _gameController = new GameController(_sessionService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        _actionController = new ActionController(_sessionService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        _shopController = new ShopController(_sessionService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public void GameController_Start_ReturnsOkWithGameState()
    {
        var result = _gameController.StartGame(new StartGameRequestDto { PlayerName = "Aji", SessionId = "test-session" });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GameStateResponseDto>>(okResult.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(GameStatePhase.SelectingBlind, response.Data.Phase);
        Assert.Equal(52, response.Data.DeckRemainingCount);
    }

    [Fact]
    public void ActionController_PlayHand_RejectsWhenInSelectingBlind()
    {
        // Session starts in SelectingBlind
        _gameController.StartGame(new StartGameRequestDto { SessionId = "session-state-check" });

        _actionController.ControllerContext.HttpContext.Request.Headers["X-Session-Id"] = "session-state-check";
        var result = _actionController.PlayHand(new PlayHandRequestDto { CardIds = new List<string> { "card1" } });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GameStateResponseDto>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("Cannot play hand while in SelectingBlind phase", response.Message);
    }

    [Fact]
    public void ShopController_GetShop_RejectsWhenNotInShop()
    {
        _gameController.StartGame(new StartGameRequestDto { SessionId = "session-shop-check" });

        _shopController.ControllerContext.HttpContext.Request.Headers["X-Session-Id"] = "session-shop-check";
        var result = _shopController.GetShop();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ShopDto>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("Shop is closed", response.Message);
    }
}
