using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.DTOs;

public class GameStateResponseDto
{
    public string SessionId { get; set; } = string.Empty;
    public Player Player { get; set; } = new();
    public GameStatePhase Phase { get; set; }
    public string PhaseName => Phase.ToString();

    public int CurrentAnte { get; set; }
    public int MaxAnte { get; set; } = 8;
    public int CurrentRound { get; set; }

    public Blind? CurrentBlind { get; set; }
    public List<Blind> AvailableBlinds { get; set; } = new();

    public int CurrentScore { get; set; }
    public int TargetScore { get; set; }
    public int Money { get; set; }

    public int HandsRemaining { get; set; }
    public int MaxHands { get; set; }
    public int DiscardsRemaining { get; set; }
    public int MaxDiscards { get; set; }

    public List<PlayingCard> Hand { get; set; } = new();
    public List<PlayingCard> FullDeck { get; set; } = new();
    public List<PlayingCard> RemainingCards { get; set; } = new();
    public int DeckRemainingCount { get; set; }
    public int DrawPileCount { get; set; }
    public int DiscardPileCount { get; set; }

    public List<JokerCard> Jokers { get; set; } = new();
    public int MaxJokers { get; set; }
    public List<IUsableCard> Consumables { get; set; } = new();
    public int MaxConsumables { get; set; }

    public List<Voucher> PurchasedVouchers { get; set; } = new();
    public Dictionary<PokerHandType, int> PokerHandLevels { get; set; } = new();
    public Dictionary<PokerHandType, int> PokerHandPlayed { get; set; } = new();

    public ShopDto? Shop { get; set; }
    public ScoreCalculationResultDto? LastScoreResult { get; set; }
    public string? LastMessage { get; set; }
}

public class ShopDto
{
    public List<JokerCard> JokerCards { get; set; } = new();
    public List<PlayingCard> PlayingCards { get; set; } = new();
    public List<TarotCard> TarotCards { get; set; } = new();
    public List<PlanetCard> PlanetCards { get; set; } = new();
    public List<SpectralCard> SpectralCards { get; set; } = new();
    public List<BoosterPack> BoosterPacks { get; set; } = new();
    public Voucher? Voucher { get; set; }
    public BoosterPack? OpenedBoosterPack { get; set; }
    public int RerollCost { get; set; }
    public int RerollCount { get; set; }
}
