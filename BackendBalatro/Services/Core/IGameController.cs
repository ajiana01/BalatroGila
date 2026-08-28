using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;
using BackendBalatro.Models.Interfaces;
using ShopEntity = BackendBalatro.Models.Entities.Shop;

namespace BackendBalatro.Services.Core;

public interface IGameController
{
    string SessionId { get; }
    IPlayer Player { get; }
    int MaxHand { get; set; }
    int HandsRemaining { get; }
    int MaxHands { get; set; }
    int DiscardsRemaining { get; }
    int MaxDiscards { get; set; }
    int Money { get; set; }
    int CurrentRound { get; }
    int CurrentAnte { get; }
    int MaxAnte { get; }
    int RoundScore { get; }
    int CurrentScore { get; }

    IDrawPile DrawPile { get; }
    IDiscardPile DiscardPile { get; }
    List<PlayingCard> Hand { get; }
    Deck Deck { get; }
    ShopEntity Shop { get; }
    List<Voucher> PurchasedVouchers { get; }
    Voucher? CurrentAnteVoucher { get; }
    bool IsAnteVoucherPurchased { get; }

    Blind? CurrentBlind { get; }
    GameStatePhase Phase { get; }

    Dictionary<PokerHandType, int> PokerHandLevels { get; }
    Dictionary<PokerHandType, int> PokerHandPlayed { get; }

    TarotCard? LastTarotUsed { get; set; }
    PlanetCard? LastPlanetUsed { get; set; }

    // Actions
    event Action<Blind>? OnBlindSelected;
    event Action<List<PlayingCard>>? OnPlayHand;
    event Action<int>? OnScore;
    event Action<Blind>? OnBlindDefeated;
    event Action? OnGetCashout;
    event Action? OnShopOpen;
    event Action<int>? OnNextRound;
    event Action<int>? OnAnteAdvance;
    event Action<PlayingCard>? OnAddPlayingCard;
    event Action? OnWinGame;
    event Action? OnGameOver;

    // Game lifecycle
    bool StartGame();
    GameStateResponseDto GetGameState(string? message = null, ScoreCalculationResultDto? lastScore = null);
    bool GameOver();
    bool Win();
    bool AdvanceAnte();
    bool NextRound();

    // Blinds
    List<Blind> GetAvailableBlinds();
    bool SelectBlind(int blindId);
    Blind? GetCurrentBlind();
    bool DefeatBlind();
    (bool Success, string Message) RerollBossBlind();

    // Hand Actions
    List<PlayingCard> DrawCards(int count);
    (bool Success, string Message, ScoreCalculationResultDto? Result) PlayHand(List<string> cardIds);
    (bool Success, string Message) DiscardCards(List<string> cardIds);
    (bool Success, string Message, ScoreCalculationResultDto? Result) GetScorePreview(List<string> cardIds);

    // Consumables & Jokers
    (bool Success, string Message) UseConsumable(string consumableId, List<string> targetCardIds);
    (bool Success, string Message) SellCard(string cardId);
    (bool Success, string Message) ArrangeJokers(List<string> jokerIds);
    (bool Success, string Message) ArrangeConsumables(List<string> consumableIds);

    // Shop
    (bool Success, string Message) BuyCardFromShop(string cardId);
    (bool Success, string Message) RerollShop();
    (bool Success, string Message, BoosterPack? Pack) BuyBoosterPack(string boosterId);
    (bool Success, string Message) SelectBoosterCard(string cardId);
    (bool Success, string Message) BuyVoucher(string voucherId);
    (bool Success, string Message) SkipBoosterPack();
    (bool Success, string Message) LeaveShop();
}
