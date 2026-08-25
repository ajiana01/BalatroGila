using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Services.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace BackendBalatro.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActionController : ControllerBase
{
    private readonly IGameSessionService _sessionService;

    public ActionController(IGameSessionService sessionService)
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

    [HttpPost("play-hand")]
    public ActionResult<ApiResponse<GameStateResponseDto>> PlayHand([FromBody] PlayHandRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.Playing)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot play hand while in {engine.Phase} phase."));
        }

        var (success, message, result) = engine.PlayHand(request.CardIds);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message, result);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("discard")]
    public ActionResult<ApiResponse<GameStateResponseDto>> Discard([FromBody] DiscardRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        if (engine.Phase != GameStatePhase.Playing)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot discard cards while in {engine.Phase} phase."));
        }

        var (success, message) = engine.DiscardCards(request.CardIds);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("score-preview")]
    public ActionResult<ApiResponse<ScoreCalculationResultDto>> GetScorePreview([FromBody] ScorePreviewRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        var (success, message, result) = engine.GetScorePreview(request.CardIds);
        if (!success || result == null)
        {
            return BadRequest(ApiResponse<ScoreCalculationResultDto>.Fail(message));
        }

        return Ok(ApiResponse<ScoreCalculationResultDto>.Ok(result, message));
    }

    [HttpPost("use-consumable")]
    public ActionResult<ApiResponse<GameStateResponseDto>> UseConsumable([FromBody] UseConsumableRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        var (success, message) = engine.UseConsumable(request.ConsumableId, request.TargetCardIds);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("sell-card")]
    public ActionResult<ApiResponse<GameStateResponseDto>> SellCard([FromBody] SellCardRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        var (success, message) = engine.SellCard(request.CardId);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }

    [HttpPost("reorder-jokers")]
    public ActionResult<ApiResponse<GameStateResponseDto>> ReorderJokers([FromBody] ReorderJokersRequestDto request)
    {
        string sessionId = GetSessionId();
        var engine = _sessionService.GetOrCreateSession(sessionId);

        var (success, message) = engine.ArrangeJokers(request.JokerIds);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail(message));
        }

        var state = engine.GetGameState(message);
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, message));
    }
}
