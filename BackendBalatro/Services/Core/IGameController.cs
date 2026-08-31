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

    bool StartGame();
    GameStateResponseDto GetGameState(string? message = null, ScoreCalculationResultDto? lastScore = null);
    bool GameOver();
    bool Win();
    bool AdvanceAnte();
    bool NextRound();

    List<Blind> GetAvailableBlinds();
    bool SelectBlind(int blindId);
    Blind? GetCurrentBlind();
    bool DefeatBlind();
    OperationResult RerollBossBlind();

    List<PlayingCard> DrawCards(int count);
    OperationResult<ScoreCalculationResultDto> PlayHand(List<string> cardIds);
    OperationResult DiscardCards(List<string> cardIds);
    OperationResult<ScoreCalculationResultDto> GetScorePreview(List<string> cardIds);

    OperationResult UseConsumable(string consumableId, List<string> targetCardIds);
    OperationResult SellCard(string cardId);
    OperationResult ArrangeJokers(List<string> jokerIds);
    OperationResult ArrangeConsumables(List<string> consumableIds);

    OperationResult BuyCardFromShop(string cardId);
    OperationResult RerollShop();
    OperationResult<BoosterPack> BuyBoosterPack(string boosterId);
    OperationResult SelectBoosterCard(string cardId);
    OperationResult BuyVoucher(string voucherId);
    OperationResult SkipBoosterPack();
    OperationResult LeaveShop();
}
