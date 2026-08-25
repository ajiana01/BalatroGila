using BackendBalatro.Services.Core;

namespace BackendBalatro.Services.Sessions;

public interface IGameSessionService
{
    IGameEngine GetOrCreateSession(string? sessionId, string? playerName = null);
    IGameEngine? GetSession(string sessionId);
    bool RemoveSession(string sessionId);
    string CreateNewSession(string? playerName = null);
}
