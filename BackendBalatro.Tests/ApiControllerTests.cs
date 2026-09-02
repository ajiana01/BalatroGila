using BackendBalatro.Controllers;
using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ApiGameController = BackendBalatro.Controllers.GameController;

namespace BackendBalatro.Tests;

[TestFixture]
public class ApiControllerTests
{
    private Mock<IGameSessionService> _sessions;
    private Mock<IGameController> _engine;
    private GameStateResponseDto _state;

    [SetUp]
    public void SetUp()
    {
        _sessions = new Mock<IGameSessionService>();
        _engine = new Mock<IGameController>();
        _state = new GameStateResponseDto { SessionId = "state" };
        _engine.Setup(e => e.GetGameState(It.IsAny<string?>(), It.IsAny<ScoreCalculationResultDto?>())).Returns(_state);
        _engine.Setup(e => e.GetGameState(It.IsAny<string?>(), It.IsAny<ScoreCalculationResultDto?>())).Returns(_state);
        _sessions.Setup(s => s.GetOrCreateSession(It.IsAny<string?>(), It.IsAny<string?>())).Returns(_engine.Object);
        _sessions.Setup(s => s.GetOrCreateSession(It.IsAny<string?>(), It.IsAny<string?>())).Returns(_engine.Object);
    }

    [TestCase("header-id", "query-id")]
    public void ActionController_SessionIdHeaderPresent_UsesHeaderValue(string header, string query)
    {
        var controller = ActionController();
        controller.Request.QueryString = new QueryString($"?sessionId={query}");
        controller.Request.Headers["X-Session-Id"] = header;
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(false, "preview"));

        controller.GetScorePreview(new ScorePreviewRequestDto());

        _sessions.Verify(s => s.GetOrCreateSession(header, null), Times.Once);
    }

    [Test]
    public void ActionController_HeaderMissing_UsesQuerySessionId()
    {
        var controller = ActionController();
        controller.Request.QueryString = new QueryString("?sessionId=query-id");
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(false, "preview"));

        controller.GetScorePreview(new ScorePreviewRequestDto());

        _sessions.Verify(s => s.GetOrCreateSession("query-id", null), Times.Once);
    }

    [Test]
    public void ActionController_BlankHeaderAndQuery_UsesDefaultSession()
    {
        var controller = ActionController();
        controller.Request.Headers["X-Session-Id"] = "  ";
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(false, "preview"));

        controller.GetScorePreview(new ScorePreviewRequestDto());

        _sessions.Verify(s => s.GetOrCreateSession("default", null), Times.Once);
    }

    [TestCase("header-id", "query-id", "header-id")]
    [TestCase("", "query-id", "query-id")]
    [TestCase("", "", "default")]
    public void ShopController_SessionResolution_HeaderQueryAndDefault(string header, string query, string expected)
    {
        var controller = ShopController();
        controller.Request.QueryString = string.IsNullOrEmpty(query) ? new QueryString() : new QueryString($"?sessionId={query}");
        if (!string.IsNullOrEmpty(header)) controller.Request.Headers["X-Session-Id"] = header;
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.InShop);
        _engine.Setup(e => e.GetGameState(It.IsAny<string?>(), It.IsAny<ScoreCalculationResultDto?>())).Returns(_state);

        controller.GetShop();

        _sessions.Verify(s => s.GetOrCreateSession(expected, null), Times.Once);
    }

    [TestCase("header-id", "request-id", "header-id")]
    [TestCase("", "request-id", "request-id")]
    [TestCase("", "", "default")]
    public void GameController_SessionResolution_HeaderRequestAndDefault(string header, string requestId, string expected)
    {
        var controller = GameController();
        if (!string.IsNullOrEmpty(header)) controller.Request.Headers["X-Session-Id"] = header;

        controller.StartGame(new StartGameRequestDto { SessionId = requestId });

        _sessions.Verify(s => s.GetOrCreateSession(expected, It.IsAny<string?>()), Times.Once);
    }

    [Test]
    public void PlayHand_WhenPhaseNotPlaying_ReturnsBadRequest()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.InShop);

        var result = ActionController().PlayHand(new PlayHandRequestDto());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        _engine.Verify(e => e.PlayHand(It.IsAny<List<string>>()), Times.Never);
    }

    [Test]
    public void PlayHand_EngineFailure_ReturnsBadRequest()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.Playing);
        _engine.Setup(e => e.PlayHand(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(false, "invalid hand"));

        var result = ActionController().PlayHand(new PlayHandRequestDto());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        Assert.That(((ApiResponse<GameStateResponseDto>)((BadRequestObjectResult)result.Result!).Value!).Message, Is.EqualTo("invalid hand"));
    }

    [Test]
    public void PlayHand_EngineSuccess_ReturnsOkWithStateAndScore()
    {
        var score = new ScoreCalculationResultDto { FinalScore = 10 };
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.Playing);
        _engine.Setup(e => e.PlayHand(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(true, "played", score));

        var result = ActionController().PlayHand(new PlayHandRequestDto { CardIds = new() { "card" } });

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        _engine.Verify(e => e.GetGameState("played", score), Times.Once);
    }

    [TestCase(GameStatePhase.InShop, false)]
    [TestCase(GameStatePhase.Playing, false)]
    [TestCase(GameStatePhase.Playing, true)]
    public void Discard_PhaseFailureEngineFailureAndSuccess_ReturnExpectedResponses(GameStatePhase phase, bool success)
    {
        _engine.SetupGet(e => e.Phase).Returns(phase);
        _engine.Setup(e => e.DiscardCards(It.IsAny<List<string>>())).Returns(new OperationResult(success, success ? "discarded" : "discard failed"));

        var result = ActionController().Discard(new DiscardRequestDto { CardIds = new() { "card" } });

        if (phase != GameStatePhase.Playing || !success) Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        else Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void GetScorePreview_FailureOrNullResult_ReturnsBadRequest(bool success, bool nullResult)
    {
        var data = nullResult ? null : new ScoreCalculationResultDto();
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(success, "preview result", data));

        var result = ActionController().GetScorePreview(new ScorePreviewRequestDto());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetScorePreview_Success_ReturnsScoreDto()
    {
        var score = new ScoreCalculationResultDto { FinalScore = 42 };
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(true, "ok", score));

        var result = ActionController().GetScorePreview(new ScorePreviewRequestDto());

        var response = (ApiResponse<ScoreCalculationResultDto>)((OkObjectResult)result.Result!).Value!;
        Assert.Multiple(() => { Assert.That(result.Result, Is.TypeOf<OkObjectResult>()); Assert.That(response.Data, Is.SameAs(score)); });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UseConsumable_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.UseConsumable(It.IsAny<string>(), It.IsAny<List<string>>())).Returns(new OperationResult(success, "consumable"));
        var result = ActionController().UseConsumable(new UseConsumableRequestDto { ConsumableId = "c" });
        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SellCard_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.SellCard(It.IsAny<string>())).Returns(new OperationResult(success, "sell"));
        var result = ActionController().SellCard(new SellCardRequestDto { CardId = "c" });
        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReorderJokers_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.ArrangeJokers(It.IsAny<List<string>>())).Returns(new OperationResult(success, "reorder"));
        var result = ActionController().ReorderJokers(new ReorderJokersRequestDto { JokerIds = new() { "j" } });
        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReorderConsumables_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.ArrangeConsumables(It.IsAny<List<string>>())).Returns(new OperationResult(success, "reorder"));
        var result = ActionController().ReorderConsumables(new ReorderConsumablesRequestDto { ConsumableIds = new() { "c" } });
        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
    }

    [Test]
    public void StartGame_NullRequest_UsesDefaultPlayerAndSession()
    {
        var result = GameController().StartGame(null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
            _engine.Verify(e => e.StartGame(), Times.Once);
            _engine.Verify(e => e.GetGameState("Game started successfully!", null), Times.Once);
        });
        _sessions.Verify(s => s.GetOrCreateSession("default", null), Times.Once);
    }

    [Test]
    public void StartGame_RequestAndHeader_ResolveSessionAndPlayerCorrectly()
    {
        var controller = GameController();
        controller.Request.Headers["X-Session-Id"] = "header-session";

        controller.StartGame(new StartGameRequestDto
        {
            SessionId = "request-session",
            PlayerName = "Aji"
        });

        _sessions.Verify(s => s.GetOrCreateSession("header-session", "Aji"), Times.Once);
    }

    [Test]
    public void GetState_ReturnsCurrentSessionState()
    {
        var result = GameController().GetState("state-session");

        var response = (ApiResponse<GameStateResponseDto>)((OkObjectResult)result.Result!).Value!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
            Assert.That(response.Data, Is.SameAs(_state));
        });
        _sessions.Verify(s => s.GetOrCreateSession("state-session", null), Times.Once);
        _engine.Verify(e => e.GetGameState(null, null), Times.Once);
    }

    [Test]
    public void GetBlinds_ReturnsAnteAndAvailableBlinds()
    {
        var blinds = new List<Blind> { new() { Id = 1, Name = "Small Blind" } };
        _engine.SetupGet(e => e.CurrentAnte).Returns(2);
        _engine.Setup(e => e.GetAvailableBlinds()).Returns(blinds);

        var result = GameController().GetBlinds("blind-session");

        var response = (ApiResponse<object>)((OkObjectResult)result.Result!).Value!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("Success"));
            Assert.That(response.Data, Is.Not.Null);
        });
        _engine.Verify(e => e.GetAvailableBlinds(), Times.Once);
    }

    [Test]
    public void SelectBlind_WhenWrongPhase_ReturnsBadRequestWithoutSelecting()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.Playing);

        var result = GameController().SelectBlind(new SelectBlindRequestDto { BlindId = 1 }, "session");

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        _engine.Verify(e => e.SelectBlind(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void SelectBlind_InvalidOrDefeated_ReturnsBadRequest()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.SelectingBlind);
        _engine.Setup(e => e.SelectBlind(999)).Returns(false);

        var result = GameController().SelectBlind(new SelectBlindRequestDto { BlindId = 999 }, "session");

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
            Assert.That(((ApiResponse<GameStateResponseDto>)((BadRequestObjectResult)result.Result!).Value!).Message,
                Is.EqualTo("Failed to select blind. Blind may already be defeated or invalid ID."));
        });
    }

    [Test]
    public void SelectBlind_Valid_ReturnsOkWithSelectedBlindState()
    {
        var blind = new Blind { Id = 1, Name = "Small Blind" };
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.SelectingBlind);
        _engine.SetupGet(e => e.CurrentBlind).Returns(blind);
        _engine.Setup(e => e.SelectBlind(1)).Returns(true);

        var result = GameController().SelectBlind(new SelectBlindRequestDto { BlindId = 1 }, "session");

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
            _engine.Verify(e => e.SelectBlind(1), Times.Once);
            _engine.Verify(e => e.GetGameState("Selected Small Blind. Good luck!", null), Times.Once);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RerollBossBlind_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.RerollBossBlind()).Returns(new OperationResult(success, success ? "rerolled" : "not allowed"));

        var result = GameController().RerollBossBlind("session");

        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
        if (success)
        {
            _engine.Verify(e => e.GetGameState("rerolled", null), Times.Once);
        }
        else
        {
            _engine.Verify(e => e.GetGameState(It.IsAny<string?>(), It.IsAny<ScoreCalculationResultDto?>()), Times.Never);
        }
    }

    [Test]
    public void GetShop_WhenClosed_ReturnsBadRequest()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.Playing);

        var result = ShopController().GetShop();

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        _engine.Verify(e => e.GetGameState(It.IsAny<string?>(), It.IsAny<ScoreCalculationResultDto?>()), Times.Never);
    }

    [Test]
    public void GetShop_WhenOpen_ReturnsShopDto()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.InShop);
        _state.Shop = new ShopDto();
        _engine.Setup(e => e.GetGameState(null, null)).Returns(_state);

        var result = ShopController().GetShop();

        var response = (ApiResponse<ShopDto>)((OkObjectResult)result.Result!).Value!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
            Assert.That(response.Data, Is.SameAs(_state.Shop));
            Assert.That(response.Message, Is.EqualTo("Shop retrieved"));
        });
    }

    [TestCase(GameStatePhase.Playing, false)]
    [TestCase(GameStatePhase.InShop, false)]
    [TestCase(GameStatePhase.InShop, true)]
    public void BuyCard_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses(GameStatePhase phase, bool success)
    {
        _engine.SetupGet(e => e.Phase).Returns(phase);
        _engine.Setup(e => e.BuyCardFromShop(It.IsAny<string>())).Returns(new OperationResult(success, "buy-card"));

        var result = ShopController().BuyCard(new BuyCardRequestDto { CardId = "card" });

        Assert.That(result.Result, Is.TypeOf(phase != GameStatePhase.InShop || !success
            ? typeof(BadRequestObjectResult)
            : typeof(OkObjectResult)));
        if (phase != GameStatePhase.InShop) _engine.Verify(e => e.BuyCardFromShop(It.IsAny<string>()), Times.Never);
    }

    [TestCase(GameStatePhase.Playing, false)]
    [TestCase(GameStatePhase.InShop, false)]
    [TestCase(GameStatePhase.InShop, true)]
    public void Reroll_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses(GameStatePhase phase, bool success)
    {
        _engine.SetupGet(e => e.Phase).Returns(phase);
        _engine.Setup(e => e.RerollShop()).Returns(new OperationResult(success, "reroll"));

        var result = ShopController().Reroll();

        Assert.That(result.Result, Is.TypeOf(phase != GameStatePhase.InShop || !success
            ? typeof(BadRequestObjectResult)
            : typeof(OkObjectResult)));
        if (phase != GameStatePhase.InShop) _engine.Verify(e => e.RerollShop(), Times.Never);
    }

    [TestCase(GameStatePhase.Playing, false)]
    [TestCase(GameStatePhase.InShop, false)]
    [TestCase(GameStatePhase.InShop, true)]
    public void BuyBooster_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses(GameStatePhase phase, bool success)
    {
        _engine.SetupGet(e => e.Phase).Returns(phase);
        _engine.Setup(e => e.BuyBoosterPack(It.IsAny<string>())).Returns(
            new OperationResult<BoosterPack>(success, "booster", new BoosterPack()));

        var result = ShopController().BuyBooster(new BuyBoosterRequestDto { BoosterId = "booster" });

        Assert.That(result.Result, Is.TypeOf(phase != GameStatePhase.InShop || !success
            ? typeof(BadRequestObjectResult)
            : typeof(OkObjectResult)));
        if (phase != GameStatePhase.InShop) _engine.Verify(e => e.BuyBoosterPack(It.IsAny<string>()), Times.Never);
    }

    [TestCase(GameStatePhase.Playing, false)]
    [TestCase(GameStatePhase.InShop, false)]
    [TestCase(GameStatePhase.InShop, true)]
    public void SelectBoosterCard_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses(GameStatePhase phase, bool success)
    {
        _engine.SetupGet(e => e.Phase).Returns(phase);
        _engine.Setup(e => e.SelectBoosterCard(It.IsAny<string>())).Returns(new OperationResult(success, "pick"));

        var result = ShopController().SelectBoosterCard(new SelectBoosterCardRequestDto { CardId = "card" });

        Assert.That(result.Result, Is.TypeOf(phase != GameStatePhase.InShop || !success
            ? typeof(BadRequestObjectResult)
            : typeof(OkObjectResult)));
        if (phase != GameStatePhase.InShop) _engine.Verify(e => e.SelectBoosterCard(It.IsAny<string>()), Times.Never);
    }

    [TestCase(GameStatePhase.Playing, false)]
    [TestCase(GameStatePhase.InShop, true)]
    public void SkipBooster_WrongPhaseAndSuccess_ReturnExpectedResponses(GameStatePhase phase, bool success)
    {
        _engine.SetupGet(e => e.Phase).Returns(phase);
        _engine.Setup(e => e.SkipBoosterPack()).Returns(new OperationResult(success, "skip"));

        var result = ShopController().SkipBooster();

        Assert.That(result.Result, Is.TypeOf(phase != GameStatePhase.InShop
            ? typeof(BadRequestObjectResult)
            : typeof(OkObjectResult)));
        if (phase != GameStatePhase.InShop) _engine.Verify(e => e.SkipBoosterPack(), Times.Never);
    }

    [TestCase(GameStatePhase.Playing, false)]
    [TestCase(GameStatePhase.InShop, false)]
    [TestCase(GameStatePhase.InShop, true)]
    public void BuyVoucher_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses(GameStatePhase phase, bool success)
    {
        _engine.SetupGet(e => e.Phase).Returns(phase);
        _engine.Setup(e => e.BuyVoucher(It.IsAny<string>())).Returns(new OperationResult(success, "voucher"));

        var result = ShopController().BuyVoucher(new BuyVoucherRequestDto { VoucherId = "voucher" });

        Assert.That(result.Result, Is.TypeOf(phase != GameStatePhase.InShop || !success
            ? typeof(BadRequestObjectResult)
            : typeof(OkObjectResult)));
        if (phase != GameStatePhase.InShop) _engine.Verify(e => e.BuyVoucher(It.IsAny<string>()), Times.Never);
    }

    [TestCase(GameStatePhase.Playing, false)]
    [TestCase(GameStatePhase.InShop, false)]
    [TestCase(GameStatePhase.InShop, true)]
    public void LeaveShop_WrongPhaseEngineFailureAndSuccess_ReturnExpectedResponses(GameStatePhase phase, bool success)
    {
        _engine.SetupGet(e => e.Phase).Returns(phase);
        _engine.Setup(e => e.LeaveShop()).Returns(new OperationResult(success, "leave"));

        var result = ShopController().LeaveShop();

        Assert.That(result.Result, Is.TypeOf(phase != GameStatePhase.InShop || !success
            ? typeof(BadRequestObjectResult)
            : typeof(OkObjectResult)));
        if (phase != GameStatePhase.InShop) _engine.Verify(e => e.LeaveShop(), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ShopController_RerollBossBlind_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.RerollBossBlind()).Returns(new OperationResult(success, "boss reroll"));

        var result = ShopController().RerollBossBlind();

        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
        if (success) _engine.Verify(e => e.GetGameState("boss reroll", null), Times.Once);
        else _engine.Verify(e => e.GetGameState(It.IsAny<string?>(), It.IsAny<ScoreCalculationResultDto?>()), Times.Never);
    }

    private ActionController ActionController()
    {
        var controller = new ActionController(_sessions.Object);
        Configure(controller);
        return controller;
    }

    private ApiGameController GameController()
    {
        var controller = new ApiGameController(_sessions.Object);
        Configure(controller);
        return controller;
    }

    private ShopController ShopController()
    {
        var controller = new ShopController(_sessions.Object);
        Configure(controller);
        return controller;
    }

    private static void Configure(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }
}
