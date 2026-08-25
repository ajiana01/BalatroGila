using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Services.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace BackendBalatro.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameSessionService _sessionService;

    public GameController(IGameSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    private string GetSessionId(string? requestedSessionId)
    {
        if (Request.Headers.TryGetValue("X-Session-Id", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            return headerVal.ToString();
        }
        return !string.IsNullOrWhiteSpace(requestedSessionId) ? requestedSessionId : "default";
    }

    [HttpPost("start")]
    public ActionResult<ApiResponse<GameStateResponseDto>> StartGame([FromBody] StartGameRequestDto? request)
    {
        string sessionId = GetSessionId(request?.SessionId);
        var engine = _sessionService.GetOrCreateSession(sessionId, request?.PlayerName);
        engine.StartGame();

        var state = engine.GetGameState("Game started successfully!");
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, "Game initialized"));
    }

    [HttpGet("state")]
    public ActionResult<ApiResponse<GameStateResponseDto>> GetState([FromQuery] string? sessionId)
    {
        string resolvedSessionId = GetSessionId(sessionId);
        var engine = _sessionService.GetOrCreateSession(resolvedSessionId);
        var state = engine.GetGameState();
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state));
    }

    [HttpGet("blinds")]
    public ActionResult<ApiResponse<object>> GetBlinds([FromQuery] string? sessionId)
    {
        string resolvedSessionId = GetSessionId(sessionId);
        var engine = _sessionService.GetOrCreateSession(resolvedSessionId);
        var blinds = engine.GetAvailableBlinds();
        return Ok(ApiResponse<object>.Ok(new { CurrentAnte = engine.CurrentAnte, Blinds = blinds }));
    }

    [HttpPost("blinds/select")]
    public ActionResult<ApiResponse<GameStateResponseDto>> SelectBlind(
        [FromBody] SelectBlindRequestDto request,
        [FromQuery] string? sessionId)
    {
        string resolvedSessionId = GetSessionId(sessionId);
        var engine = _sessionService.GetOrCreateSession(resolvedSessionId);

        if (engine.Phase != GameStatePhase.SelectingBlind)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail($"Cannot select blind while in {engine.Phase} phase."));
        }

        bool success = engine.SelectBlind(request.BlindId);
        if (!success)
        {
            return BadRequest(ApiResponse<GameStateResponseDto>.Fail("Failed to select blind. Blind may already be defeated or invalid ID."));
        }

        var state = engine.GetGameState($"Selected {engine.CurrentBlind?.Name}. Good luck!");
        return Ok(ApiResponse<GameStateResponseDto>.Ok(state, "Blind selected"));
    }
}
