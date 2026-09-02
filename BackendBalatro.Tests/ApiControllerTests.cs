/*
 * ApiControllerTests.cs - Unit Tests for HTTP API Controllers
 *
 * This file documents the controller-layer contract: session resolution,
 * phase validation, mapping game-engine results to HTTP 200/400 responses,
 * and response envelopes. Gameplay rules are intentionally tested in the
 * GameController and service test fixtures instead.
 *
 * Key testing practices demonstrated:
 * - Arrange-Act-Assert (AAA)
 * - Dependency mocking with Moq
 * - Parameterized tests with [TestCase]
 * - Test names following [Endpoint]_[Scenario]_[ExpectedResult]
 *
 */

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

/// <summary>
/// Test fixture for <see cref="ActionController"/>, the HTTP GameController,
/// and <see cref="ShopController"/>.
///
/// Each test uses a mocked session service and game engine to isolate
/// controller behavior from gameplay, databases, HTTP hosting, and other tests.
/// </summary>
[TestFixture]
public class ApiControllerTests
{
    // Shared dependency used by all API controllers to resolve a game session.
    private Mock<IGameSessionService> _sessions;

    // Indirect system under test: controllers retrieve this engine through the session service.
    private Mock<IGameController> _engine;

    // Default state returned when a successful endpoint requests the latest game state.
    private GameStateResponseDto _state;

    /// <summary>
    /// Runs before every test to create fresh mocks and state.
    /// This isolation prevents one test's setup or invocations from affecting another.
    /// </summary>
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

    #region Session Resolution

    // The X-Session-Id header takes precedence over query-string and request-body values.
    /// <summary>
    /// Verifies that the session ID supplied through the HTTP header overrides
    /// the session ID supplied through the query string.
    /// </summary>
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

    /// <summary>
    /// Verifies that ActionController falls back to the query-string session ID
    /// when the X-Session-Id header is not present.
    /// </summary>
    [Test]
    public void ActionController_HeaderMissing_UsesQuerySessionId()
    {
        var controller = ActionController();
        controller.Request.QueryString = new QueryString("?sessionId=query-id");
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(false, "preview"));

        controller.GetScorePreview(new ScorePreviewRequestDto());

        _sessions.Verify(s => s.GetOrCreateSession("query-id", null), Times.Once);
    }

    /// <summary>
    /// Verifies that blank session input resolves to the default session.
    /// </summary>
    [Test]
    public void ActionController_BlankHeaderAndQuery_UsesDefaultSession()
    {
        var controller = ActionController();
        controller.Request.Headers["X-Session-Id"] = "  ";
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(false, "preview"));

        controller.GetScorePreview(new ScorePreviewRequestDto());

        _sessions.Verify(s => s.GetOrCreateSession("default", null), Times.Once);
    }

    /// <summary>
    /// Verifies the ShopController session precedence order: header, query
    /// string, then the default session.
    /// </summary>
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

    /// <summary>
    /// Verifies the GameController session precedence order: header, request
    /// body, then the default session.
    /// </summary>
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

    #endregion

    #region ActionController Endpoints

    // These tests verify that ActionController maps engine results to the correct
    // HTTP responses without implementing gameplay rules itself.
    /// <summary>
    /// Verifies that PlayHand rejects requests outside the Playing phase before
    /// delegating to the game engine.
    /// </summary>
    [Test]
    public void PlayHand_WhenPhaseNotPlaying_ReturnsBadRequest()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.InShop);

        var result = ActionController().PlayHand(new PlayHandRequestDto());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        _engine.Verify(e => e.PlayHand(It.IsAny<List<string>>()), Times.Never);
    }

    /// <summary>
    /// Verifies that a failed PlayHand result from the engine is returned as a
    /// bad-request response with the engine message.
    /// </summary>
    [Test]
    public void PlayHand_EngineFailure_ReturnsBadRequest()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.Playing);
        _engine.Setup(e => e.PlayHand(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(false, "invalid hand"));

        var result = ActionController().PlayHand(new PlayHandRequestDto());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        Assert.That(((ApiResponse<GameStateResponseDto>)((BadRequestObjectResult)result.Result!).Value!).Message, Is.EqualTo("invalid hand"));
    }

    /// <summary>
    /// Verifies that a successful hand play returns updated game state together
    /// with the score result produced by the engine.
    /// </summary>
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

    /// <summary>
    /// Verifies that Discard returns the correct HTTP response for an invalid
    /// phase, an engine failure, and an engine success.
    /// </summary>
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

    /// <summary>
    /// Verifies that score preview fails when the engine reports failure or
    /// returns no score payload.
    /// </summary>
    [TestCase(false, false)]
    [TestCase(true, true)]
    public void GetScorePreview_FailureOrNullResult_ReturnsBadRequest(bool success, bool nullResult)
    {
        var data = nullResult ? null : new ScoreCalculationResultDto();
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(success, "preview result", data));

        var result = ActionController().GetScorePreview(new ScorePreviewRequestDto());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    /// <summary>
    /// Verifies that a valid score preview is returned in a successful response
    /// without being replaced by another DTO instance.
    /// </summary>
    [Test]
    public void GetScorePreview_Success_ReturnsScoreDto()
    {
        var score = new ScoreCalculationResultDto { FinalScore = 42 };
        _engine.Setup(e => e.GetScorePreview(It.IsAny<List<string>>())).Returns(new OperationResult<ScoreCalculationResultDto>(true, "ok", score));

        var result = ActionController().GetScorePreview(new ScorePreviewRequestDto());

        var response = (ApiResponse<ScoreCalculationResultDto>)((OkObjectResult)result.Result!).Value!;
        Assert.Multiple(() => { Assert.That(result.Result, Is.TypeOf<OkObjectResult>()); Assert.That(response.Data, Is.SameAs(score)); });
    }

    /// <summary>
    /// Verifies that UseConsumable maps the engine success flag to either a
    /// successful or bad-request HTTP response.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void UseConsumable_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.UseConsumable(It.IsAny<string>(), It.IsAny<List<string>>())).Returns(new OperationResult(success, "consumable"));
        var result = ActionController().UseConsumable(new UseConsumableRequestDto { ConsumableId = "c" });
        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
    }

    /// <summary>
    /// Verifies that SellCard maps engine failure and success to the matching
    /// HTTP response type.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void SellCard_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.SellCard(It.IsAny<string>())).Returns(new OperationResult(success, "sell"));
        var result = ActionController().SellCard(new SellCardRequestDto { CardId = "c" });
        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
    }

    /// <summary>
    /// Verifies that Joker reordering maps engine failure and success to the
    /// matching HTTP response type.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ReorderJokers_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.ArrangeJokers(It.IsAny<List<string>>())).Returns(new OperationResult(success, "reorder"));
        var result = ActionController().ReorderJokers(new ReorderJokersRequestDto { JokerIds = new() { "j" } });
        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
    }

    /// <summary>
    /// Verifies that consumable reordering maps engine failure and success to
    /// the matching HTTP response type.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ReorderConsumables_FailureAndSuccess_ReturnExpectedResponses(bool success)
    {
        _engine.Setup(e => e.ArrangeConsumables(It.IsAny<List<string>>())).Returns(new OperationResult(success, "reorder"));
        var result = ActionController().ReorderConsumables(new ReorderConsumablesRequestDto { ConsumableIds = new() { "c" } });
        Assert.That(result.Result, Is.TypeOf(success ? typeof(OkObjectResult) : typeof(BadRequestObjectResult)));
    }

    #endregion

    #region GameController HTTP Endpoints

    // This region tests the HTTP controller, not Services.Core.GameController.
    // The ApiGameController alias keeps the two types unambiguous.
    /// <summary>
    /// Verifies that a null start-game request uses the default session and
    /// player while starting the engine and returning its state.
    /// </summary>
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

    /// <summary>
    /// Verifies that the start-game header session ID overrides the request ID
    /// while the requested player name is preserved.
    /// </summary>
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

    /// <summary>
    /// Verifies that GetState retrieves the requested session and returns its
    /// current game-state DTO in a successful envelope.
    /// </summary>
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

    /// <summary>
    /// Verifies that GetBlinds returns the engine's current ante and available
    /// blinds in a successful response.
    /// </summary>
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

    /// <summary>
    /// Verifies that SelectBlind rejects a request outside SelectingBlind and
    /// does not invoke the engine selection operation.
    /// </summary>
    [Test]
    public void SelectBlind_WhenWrongPhase_ReturnsBadRequestWithoutSelecting()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.Playing);

        var result = GameController().SelectBlind(new SelectBlindRequestDto { BlindId = 1 }, "session");

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        _engine.Verify(e => e.SelectBlind(It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// Verifies that an invalid or already-defeated blind is reported as a
    /// bad request with the controller's explanatory message.
    /// </summary>
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

    /// <summary>
    /// Verifies that selecting a valid blind succeeds and returns state with a
    /// message that identifies the selected blind.
    /// </summary>
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

    /// <summary>
    /// Verifies that boss-blind rerolls return a bad request on failure and
    /// refreshed game state on success.
    /// </summary>
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

    #endregion

    #region ShopController HTTP Endpoints

    // Shop endpoints may delegate operations to the engine only during the InShop phase.
    /// <summary>
    /// Verifies that the shop endpoint rejects requests when the game is not
    /// currently in the InShop phase.
    /// </summary>
    [Test]
    public void GetShop_WhenClosed_ReturnsBadRequest()
    {
        _engine.SetupGet(e => e.Phase).Returns(GameStatePhase.Playing);

        var result = ShopController().GetShop();

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        _engine.Verify(e => e.GetGameState(It.IsAny<string?>(), It.IsAny<ScoreCalculationResultDto?>()), Times.Never);
    }

    /// <summary>
    /// Verifies that the shop endpoint returns the Shop DTO from current game
    /// state when the shop is open.
    /// </summary>
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

    /// <summary>
    /// Verifies that card purchases are phase-guarded and map engine failures
    /// and successes to the appropriate HTTP response.
    /// </summary>
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

    /// <summary>
    /// Verifies that shop rerolls are phase-guarded and map engine failures
    /// and successes to the appropriate HTTP response.
    /// </summary>
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

    /// <summary>
    /// Verifies that booster purchases are phase-guarded and map engine
    /// failures and successes to the appropriate HTTP response.
    /// </summary>
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

    /// <summary>
    /// Verifies that booster-card selection is phase-guarded and maps engine
    /// failures and successes to the appropriate HTTP response.
    /// </summary>
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

    /// <summary>
    /// Verifies that skipping a booster is rejected outside the shop and
    /// succeeds when the engine accepts the request in the shop.
    /// </summary>
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

    /// <summary>
    /// Verifies that voucher purchases are phase-guarded and map engine
    /// failures and successes to the appropriate HTTP response.
    /// </summary>
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

    /// <summary>
    /// Verifies that leaving the shop is phase-guarded and maps engine
    /// failures and successes to the appropriate HTTP response.
    /// </summary>
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

    /// <summary>
    /// Verifies that the ShopController boss-reroll endpoint returns a bad
    /// request on engine failure and refreshed game state on success.
    /// </summary>
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

    #endregion

    #region Controller Factories

    // These factories give every controller a fresh HttpContext so tests can set
    // headers and query strings as they would on a real HTTP request.
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

    #endregion
}
