using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Services.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace BackendBalatro.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopController : ControllerBase
{
    private readonly IGameSessionService _sessionService;

    public ShopController(IGameSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    private string GetSessionId()
    {
        if (Request.Headers.TryGetValue("X-Session-Id", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            return headerVal.ToString();
        }
        if (Request.Query.TryGetValue("sessionId", out var queryVal) && !string.IsNullOrWhiteSpace(queryVal))
        {
            return queryVal.ToString();
        }
        return "default";
    }

    [HttpGet]
    public ActionResult<ApiResponse<ShopDto>> GetShop()
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.InShop)
        {
            return BadRequest(ApiResponse<ShopDto>.Fail($"Shop is closed. Current phase is {engine.Phase}."));
        }

        var state = engine.GetGameState();
        return Ok(ApiResponse<ShopDto>.Ok(state.Shop!, "Shop retrieved"));
    }

    [HttpPost("buy-card")]
    public ActionResult<ApiResponse<GameStateResponseDto>> BuyCard([FromBody] BuyCardRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.InShop)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot buy from shop while in {engine.Phase} phase."));
        }

        var (success, message) = engine.BuyCardFromShop(request.CardId);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("reroll")]
    public ActionResult<ApiResponse<GameStateResponseDto>> Reroll()
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.InShop)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot reroll shop while in {engine.Phase} phase."));
        }

        var (success, message) = engine.RerollShop();
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("buy-booster")]
    public ActionResult<ApiResponse<GameStateResponseDto>> BuyBooster([FromBody] BuyBoosterRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.InShop)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot buy booster pack while in {engine.Phase} phase."));
        }

        var (success, message, pack) = engine.BuyBoosterPack(request.BoosterId);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("select-booster-card")]
    public ActionResult<ApiResponse<GameStateResponseDto>> SelectBoosterCard([FromBody] SelectBoosterCardRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.InShop)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot select booster card while in {engine.Phase} phase."));
        }

        var (success, message) = engine.SelectBoosterCard(request.CardId);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("skip-booster")]
    public ActionResult<ApiResponse<GameStateResponseDto>> SkipBooster()
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.InShop)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot skip booster pack while in {engine.Phase} phase."));
        }

        var (success, message) = engine.SkipBoosterPack();
        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("buy-voucher")]
    public ActionResult<ApiResponse<GameStateResponseDto>> BuyVoucher([FromBody] BuyVoucherRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.InShop)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot buy voucher while in {engine.Phase} phase."));
        }

        var (success, message) = engine.BuyVoucher(request.VoucherId);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("leave")]
    public ActionResult<ApiResponse<GameStateResponseDto>> LeaveShop()
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.InShop)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot leave shop while in {engine.Phase} phase."));
        }

        var (success, message) = engine.LeaveShop();
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }
}
