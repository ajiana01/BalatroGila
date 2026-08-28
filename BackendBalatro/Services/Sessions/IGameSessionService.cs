using BackendBalatro.Services.Core;

namespace BackendBalatro.Services.Sessions;

public interface IGameSessionService
{
    IGameController GetOrCreateSession(string? sessionId, string? playerName = null);
    IGameController? GetSession(string sessionId);
    bool RemoveSession(string sessionId);
    string CreateNewSession(string? playerName = null);
}
