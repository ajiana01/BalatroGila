using BackendBalatro.Enums;

namespace BackendBalatro.Models.DTOs;

public class StartGameRequestDto
{
    public string? PlayerName { get; set; } = "Player 1";
    public string? SessionId { get; set; }
}

public class SelectBlindRequestDto
{
    public int BlindId { get; set; }
    public BlindType? BlindType { get; set; }
}

public class PlayHandRequestDto
{
    public List<string> CardIds { get; set; } = new();
}

public class DiscardRequestDto
{
    public List<string> CardIds { get; set; } = new();
}

public class ScorePreviewRequestDto
{
    public List<string> CardIds { get; set; } = new();
}

public class UseConsumableRequestDto
{
    public string ConsumableId { get; set; } = string.Empty;
    public List<string> TargetCardIds { get; set; } = new();
}

public class SellCardRequestDto
{
    public string CardId { get; set; } = string.Empty;
}

public class ReorderJokersRequestDto
{
    public List<string> JokerIds { get; set; } = new();
}

public class BuyCardRequestDto
{
    public string CardId { get; set; } = string.Empty;
}

public class BuyBoosterRequestDto
{
    public string BoosterId { get; set; } = string.Empty;
}

public class SelectBoosterCardRequestDto
{
    public string CardId { get; set; } = string.Empty;
}

public class BuyVoucherRequestDto
{
    public string VoucherId { get; set; } = string.Empty;
}
