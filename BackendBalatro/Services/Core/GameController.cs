using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;
using BackendBalatro.Models.Interfaces;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;

namespace BackendBalatro.Services.Core;

//TODO SERILOG
public class GameController : IGameController
{
    private readonly IScoringService _scoringService;
    private readonly IShopService _shopService;
    private readonly IConsumableEffectHandler _consumableHandler;

    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public IPlayer Player { get; set; } = new Player(1, "Player 1");

    public int MaxHand { get; set; } = 8;
    private int _currentHand = 4;
    public int HandsRemaining => _currentHand;
    public int MaxHands { get; set; } = 4;

    private int _currentDiscard = 4;
    public int DiscardsRemaining => _currentDiscard;
    public int MaxDiscards { get; set; } = 4;

    public int Money { get; set; } = 4;
    public int CurrentRound { get; private set; } = 1;
    public int CurrentAnte { get; private set; } = 1;
    public int MaxAnte => 8;

    public int RoundScore { get; private set; } = 0;
    public int CurrentScore { get; private set; } = 0;

    public IDrawPile DrawPile { get; } = new DrawPile();
    public IDiscardPile DiscardPile { get; } = new DiscardPile();
    public List<PlayingCard> Hand { get; } = new();
    public Deck Deck { get; } = new(5, 2);
    public BackendBalatro.Models.Entities.Shop Shop { get; } = new();
    public List<Voucher> PurchasedVouchers { get; } = new();
    public Voucher? CurrentAnteVoucher { get; private set; }
    public bool IsAnteVoucherPurchased { get; private set; } = false;
    public bool IsBossBlindRerolledThisAnte { get; private set; } = false;

    public Dictionary<int, List<Blind>> BlindEnemies { get; } = new();
    public Blind? CurrentBlind { get; private set; }
    public GameStatePhase Phase { get; private set; } = GameStatePhase.SelectingBlind;

    public Dictionary<PokerHandType, int> PokerHandLevels { get; } = new();
    public Dictionary<PokerHandType, int> PokerHandPlayed { get; } = new();

    public TarotCard? LastTarotUsed { get; set; }
    public PlanetCard? LastPlanetUsed { get; set; }

    private readonly HashSet<string> _playedCardIdsThisAnte = new();
    private readonly HashSet<PokerHandType> _playedHandTypesThisRound = new();
    private PokerHandType? _allowedHandTypeThisRound = null;

    private record BossBlindDef(
        BlindId BlindId,
        string Name,
        string Description,
        int MinAnte,
        float Multiplier,
        int Reward = 5,
        bool IsShowdown = false);

    private static readonly List<BossBlindDef> SupportedBossBlinds = new()
    {
        new(BlindId.TheClub, "The Club", "All Club cards are debuffed", 1, 2.0f, 5),
        new(BlindId.TheGoad, "The Goad", "All Spade cards are debuffed", 1, 2.0f, 5),
        new(BlindId.TheWindow, "The Window", "All Diamond cards are debuffed", 1, 2.0f, 5),
        new(BlindId.TheHead, "The Head", "All Heart cards are debuffed", 1, 2.0f, 5),
        new(BlindId.ThePlant, "The Plant", "All face cards are debuffed", 4, 2.0f, 5),

        new(BlindId.ThePsychic, "The Psychic", "Must play exactly 5 cards", 1, 2.0f, 5),
        new(BlindId.TheNeedle, "The Needle", "Play only 1 hand", 2, 1.0f, 5),
        new(BlindId.TheWater, "The Water", "Start with 0 discards", 2, 2.0f, 5),
        new(BlindId.TheManacle, "The Manacle", "-1 Hand Size", 1, 2.0f, 5),
        new(BlindId.TheWall, "The Wall", "Extra large blind (4x base score)", 2, 4.0f, 5),
        new(BlindId.TheArm, "The Arm", "Decrease level of played poker hand by 1", 2, 2.0f, 5),
        new(BlindId.TheTooth, "The Tooth", "Lose $1 per card played", 3, 2.0f, 5),
        new(BlindId.TheFlint, "The Flint", "Base Chips and Mult are halved", 2, 2.0f, 5),
        new(BlindId.TheEye, "The Eye", "No repeat hand types this round", 3, 2.0f, 5),
        new(BlindId.TheMouth, "The Mouth", "Only 1 hand type allowed this round", 2, 2.0f, 5),
        new(BlindId.TheHook, "The Hook", "Discards 2 random cards in hand after each hand played", 1, 2.0f, 5),
        new(BlindId.TheOx, "The Ox", "Playing the most played poker hand sets money to $0", 6, 2.0f, 5),
        new(BlindId.ThePillar, "The Pillar", "Cards played previously this Ante are debuffed", 1, 2.0f, 5),

        new(BlindId.VioletVessel, "Violet Vessel", "Very large blind (6x base score)", 8, 6.0f, 8, true),
        new(BlindId.VerdantLeaf, "Verdant Leaf", "All cards are debuffed until 1 Joker is sold", 8, 2.0f, 8, true)
    };

    public event Action<Blind>? OnBlindSelected;
    public event Action<List<PlayingCard>>? OnPlayHand;
    public event Action<int>? OnScore;
    public event Action<Blind>? OnBlindDefeated;
    public event Action? OnGetCashout;
    public event Action? OnShopOpen;
    public event Action<int>? OnNextRound;
    public event Action<int>? OnAnteAdvance;
    public event Action<PlayingCard>? OnAddPlayingCard;
    public event Action? OnWinGame;
    public event Action? OnGameOver;

    public GameController(
        IScoringService scoringService,
        IShopService shopService,
        IConsumableEffectHandler consumableHandler)
    {
        _scoringService = scoringService;
        _shopService = shopService;
        _consumableHandler = consumableHandler;

        InitializePokerHandLevels();
    }

    private void InitializePokerHandLevels()
    {
        PokerHandLevels.Clear();
        PokerHandPlayed.Clear();
        foreach (PokerHandType type in Enum.GetValues<PokerHandType>())
        {
            PokerHandLevels[type] = 1;
            PokerHandPlayed[type] = 0;
        }
    }

    public bool StartGame()
    {
        CurrentAnte = 1;
        CurrentRound = 1;
        Money = 4;
        RoundScore = 0;
        CurrentScore = 0;
        MaxHands = 4;
        MaxDiscards = 4;
        _currentHand = 4;
        _currentDiscard = 4;
        MaxHand = 8;
        Phase = GameStatePhase.SelectingBlind;

        LastTarotUsed = null;
        LastPlanetUsed = null;

        _playedCardIdsThisAnte.Clear();
        _playedHandTypesThisRound.Clear();
        _allowedHandTypeThisRound = null;

        Deck.JokerCards.Clear();
        Deck.UsableCards.Clear();
        PurchasedVouchers.Clear();
        IsAnteVoucherPurchased = false;
        IsBossBlindRerolledThisAnte = false;
        Hand.Clear();
        DiscardPile.Clear();
        DrawPile.Clear();

        InitializePokerHandLevels();

        var standardCards = new List<PlayingCard>();
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                standardCards.Add(new PlayingCard(suit, rank, EnhancePokerCard.None, 1));
            }
        }
        DrawPile.AddCards(standardCards);
        DrawPile.Shuffle();

        GenerateBlindsForAnte(CurrentAnte);
        CurrentAnteVoucher = _shopService.GenerateVoucherForAnte(CurrentAnte, PurchasedVouchers);

        return true;
    }

    private void GenerateBlindsForAnte(int ante)
    {
        int baseScore = ante switch
        {
            1 => 300,
            2 => 800,
            3 => 2000,
            4 => 5000,
            5 => 11000,
            6 => 20000,
            7 => 35000,
            8 => 50000,
            _ => (int)(50000 * Math.Pow(1.5, ante - 8))
        };

        var blinds = new List<Blind>
        {
            new Blind(BlindId.SmallBlind, "Small Blind", BlindType.Small, baseScore, 3, "No special effect - can be skipped for Tag.") { Id = 1 },
            new Blind(BlindId.BigBlind, "Big Blind", BlindType.Big, (int)(baseScore * 1.5), 4, "No special effect - can be skipped for Tag.") { Id = 2 },
            GenerateBossBlind(ante, baseScore)
        };

        BlindEnemies[ante] = blinds;
    }

    private static Blind GenerateBossBlind(int ante, int baseScore)
    {
        var random = new Random();
        List<BossBlindDef> pool;
        if (ante >= 8)
        {
            pool = SupportedBossBlinds.Where(b => b.IsShowdown).ToList();
            if (pool.Count == 0) pool = SupportedBossBlinds.Where(b => b.MinAnte <= ante).ToList();
        }
        else
        {
            pool = SupportedBossBlinds.Where(b => !b.IsShowdown && b.MinAnte <= ante).ToList();
        }

        var selected = pool[random.Next(pool.Count)];
        int finalScore = (int)(baseScore * selected.Multiplier);

        return new Blind(selected.BlindId, selected.Name, BlindType.Boss, finalScore, selected.Reward, selected.Description)
        {
            Id = 3
        };
    }

    public List<Blind> GetAvailableBlinds()
    {
        if (BlindEnemies.TryGetValue(CurrentAnte, out var blinds))
        {
            return blinds;
        }
        GenerateBlindsForAnte(CurrentAnte);
        return BlindEnemies[CurrentAnte];
    }

    public Blind? GetCurrentBlind() => CurrentBlind;

    public bool SelectBlind(int blindId)
    {
        if (Phase != GameStatePhase.SelectingBlind)
        {
            return false;
        }

        var blinds = GetAvailableBlinds();
        var selected = blinds.FirstOrDefault(b => b.Id == blindId && !b.IsDefeated);
        if (selected == null)
        {
            return false;
        }

        CurrentBlind = selected;

        RecycleAllCardsToDrawPile();

        _playedHandTypesThisRound.Clear();
        _allowedHandTypeThisRound = null;

        ApplyBossBlindEffects(selected);

        RoundScore = 0;

        _currentHand = GetInitialHandsForBlind(selected);
        _currentDiscard = GetInitialDiscardsForBlind(selected);

        DrawCards(GetEffectiveMaxHandSize(selected));

        Phase = GameStatePhase.Playing;
        OnBlindSelected?.Invoke(selected);
        return true;
    }

    private void RecycleAllCardsToDrawPile()
    {
        var allCards = new List<PlayingCard>();
        allCards.AddRange(Hand);
        allCards.AddRange(DiscardPile.PullAllCards());
        allCards.AddRange(DrawPile.DrawCards(DrawPile.Count));

        foreach (var card in allCards)
        {
            card.IsDebuffed = false;
        }

        Hand.Clear();
        DiscardPile.Clear();
        DrawPile.Clear();
        DrawPile.AddCards(allCards);
        DrawPile.Shuffle();
    }

    private void ApplyBossBlindEffects(Blind boss)
    {
        if (boss.BlindType != BlindType.Boss) return;

        foreach (var card in DrawPile.PlayingCards)
        {
            ApplyBossDebuffToCard(card, boss);
        }
        foreach (var card in Hand)
        {
            ApplyBossDebuffToCard(card, boss);
        }
    }

    private void ApplyBossDebuffToCard(PlayingCard card, Blind? boss)
    {
        if (boss == null || boss.BlindType != BlindType.Boss) return;

        if (boss.BlindId == BlindId.TheClub && card.Suit == Suit.Clubs) card.IsDebuffed = true;
        if (boss.BlindId == BlindId.TheGoad && card.Suit == Suit.Spades) card.IsDebuffed = true;
        if (boss.BlindId == BlindId.TheWindow && card.Suit == Suit.Diamonds) card.IsDebuffed = true;
        if (boss.BlindId == BlindId.TheHead && card.Suit == Suit.Hearts) card.IsDebuffed = true;
        if (boss.BlindId == BlindId.ThePlant && (card.Rank == Rank.Jack || card.Rank == Rank.Queen || card.Rank == Rank.King)) card.IsDebuffed = true;
        if (boss.BlindId == BlindId.ThePillar && _playedCardIdsThisAnte.Contains(card.Id)) card.IsDebuffed = true;
        if (boss.BlindId == BlindId.VerdantLeaf) card.IsDebuffed = true;
    }

    private int GetEffectiveMaxHandSize(Blind? blind = null)
    {
        var activeBlind = blind ?? CurrentBlind;
        return activeBlind?.BlindId == BlindId.TheManacle
            ? Math.Max(1, MaxHand - 1)
            : MaxHand;
    }

    private int GetInitialHandsForBlind(Blind blind)
    {
        return blind.BlindId == BlindId.TheNeedle ? 1 : MaxHands;
    }

    private int GetInitialDiscardsForBlind(Blind blind)
    {
        return blind.BlindId == BlindId.TheWater ? 0 : MaxDiscards;
    }

    private bool IsHandPlayAllowedByPsychic(int cardCount, out string? errorMessage)
    {
        if (CurrentBlind?.BlindId == BlindId.ThePsychic && cardCount != 5)
        {
            errorMessage = "The Psychic forces you to play exactly 5 cards!";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private bool ValidateBossHandTypeRestrictions(PokerHandType handType, string handName, out string? errorMessage)
    {
        if (CurrentBlind?.BlindId == BlindId.TheEye && _playedHandTypesThisRound.Contains(handType))
        {
            errorMessage = $"The Eye does not allow repeating {handName} in this round!";
            return false;
        }

        if (CurrentBlind?.BlindId == BlindId.TheMouth)
        {
            if (_allowedHandTypeThisRound == null)
            {
                _allowedHandTypeThisRound = handType;
            }
            else if (_allowedHandTypeThisRound != handType)
            {
                errorMessage = $"The Mouth only allows playing {_allowedHandTypeThisRound} this round!";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    private void ApplyBossHandPlayedPenalties(PokerHandType playedHandType, int playedCardCount)
    {
        if (CurrentBlind == null || CurrentBlind.BlindType != BlindType.Boss) return;

        if (CurrentBlind.BlindId == BlindId.TheArm && PokerHandLevels.TryGetValue(playedHandType, out int lvl) && lvl > 1)
        {
            PokerHandLevels[playedHandType] = lvl - 1;
        }

        if (CurrentBlind.BlindId == BlindId.TheTooth)
        {
            Money = Math.Max(0, Money - playedCardCount);
        }

        if (CurrentBlind.BlindId == BlindId.TheOx && PokerHandPlayed.Values.Any(v => v > 0))
        {
            var mostPlayedType = PokerHandPlayed.OrderByDescending(kv => kv.Value).First().Key;
            if (playedHandType == mostPlayedType)
            {
                Money = 0;
            }
        }
    }

    private List<PlayingCard> ProcessGlassCardDestruction(IEnumerable<PlayingCard> scoringCards, List<string> triggerMessages)
    {
        var destroyedCards = new List<PlayingCard>();
        var rng = new Random();
        foreach (var card in scoringCards)
        {
            if (!card.IsDebuffed && card.Enhancement == EnhancePokerCard.GlassCards && rng.Next(4) == 0)
            {
                destroyedCards.Add(card);
                triggerMessages.Add($"{card.Name} (Glass): Shattered!");
            }
        }
        return destroyedCards;
    }

    private void ApplyTheHookPenalty()
    {
        if (CurrentBlind?.BlindId != BlindId.TheHook || Hand.Count == 0) return;

        var rng = new Random();
        int discardCount = Math.Min(2, Hand.Count);
        var discarded = Hand.OrderBy(_ => rng.Next()).Take(discardCount).ToList();
        foreach (var card in discarded)
        {
            Hand.Remove(card);
        }
        DiscardPile.DiscardCards(discarded);
    }

    private void ApplyGoldCardEndOfRoundReward()
    {
        int goldCardCount = Hand.Count(c => !c.IsDebuffed && c.Enhancement == EnhancePokerCard.GoldCards);
        if (goldCardCount > 0)
        {
            Money += goldCardCount * 3;
        }
    }

    private void ApplyJokerEndOfRoundEffects()
    {
        foreach (var joker in Deck.JokerCards.ToList())
        {
            switch (joker.JokerId)
            {
                case JokerId.GoldenJoker:
                    Money += 4;
                    break;
                case JokerId.Popcorn:
                    joker.MultValue = Math.Max(0, joker.MultValue - 4);
                    break;
                case JokerId.GrosMichel when Random.Shared.Next(6) == 0:
                    Deck.JokerCards.Remove(joker);
                    break;
                case JokerId.Cavendish when Random.Shared.Next(1000) == 0:
                    Deck.JokerCards.Remove(joker);
                    break;
            }
        }
    }

    private void ApplyJokerAfterHandEffects()
    {
        foreach (var joker in Deck.JokerCards)
        {
            if (joker.JokerId == JokerId.IceCream)
            {
                joker.ChipsValue = Math.Max(0, joker.ChipsValue - 5);
            }
        }
    }

    private void LiftVerdantLeafDebuffs()
    {
        if (CurrentBlind?.BlindId != BlindId.VerdantLeaf) return;

        foreach (var card in Hand) card.IsDebuffed = false;
        foreach (var card in DrawPile.PlayingCards) card.IsDebuffed = false;
        foreach (var card in DiscardPile.PlayingCards) card.IsDebuffed = false;
    }

    private void EnsureHandAvailableForConsumableBooster(BoosterPack pack)
    {
        if ((pack.BoosterPackType == BoosterType.Arcana || pack.BoosterPackType == BoosterType.Spectral) && Hand.Count == 0)
        {
            DrawCards(MaxHand);
        }
    }

    public List<PlayingCard> DrawCards(int count)
    {
        int effectiveMaxHand = GetEffectiveMaxHandSize();
        int needed = Math.Min(count, effectiveMaxHand - Hand.Count);
        if (needed <= 0) return new List<PlayingCard>();

        if (DrawPile.Count < needed && DiscardPile.Count > 0)
        {
            var recycled = DiscardPile.PullAllCards();
            DrawPile.AddCards(recycled);
            DrawPile.Shuffle();
        }

        var drawn = DrawPile.DrawCards(needed);
        foreach (var card in drawn)
        {
            ApplyBossDebuffToCard(card, CurrentBlind);
        }
        Hand.AddRange(drawn);
        return drawn;
    }

    public OperationResult<ScoreCalculationResultDto> PlayHand(List<string> cardIds)
    {
        if (Phase != GameStatePhase.Playing)
        {
            return OperationResult<ScoreCalculationResultDto>.Fail($"Cannot play hand while in {Phase} phase.");
        }

        if (cardIds == null || cardIds.Count == 0 || cardIds.Count > 5)
        {
            return OperationResult<ScoreCalculationResultDto>.Fail("Must play between 1 and 5 cards.");
        }

        if (!IsHandPlayAllowedByPsychic(cardIds.Count, out var psychicError))
        {
            return OperationResult<ScoreCalculationResultDto>.Fail(psychicError!);
        }

        var playedCards = Hand.Where(c => cardIds.Contains(c.Id)).ToList();
        if (playedCards.Count != cardIds.Count)
        {
            return OperationResult<ScoreCalculationResultDto>.Fail("One or more selected cards are not in hand.");
        }

        var remainingInHand = Hand.Except(playedCards).ToList();

        var result = _scoringService.CalculateScore(
            playedCards,
            remainingInHand,
            Deck.JokerCards,
            PokerHandLevels,
            CurrentBlind?.BlindId,
            Money,
            DiscardsRemaining,
            DrawPile.Count + DiscardPile.Count);

        if (!ValidateBossHandTypeRestrictions(result.HandType, result.HandName, out var bossHandError))
        {
            return OperationResult<ScoreCalculationResultDto>.Fail(bossHandError!);
        }

        _currentHand--;

        ApplyBossHandPlayedPenalties(result.HandType, playedCards.Count);

        _playedHandTypesThisRound.Add(result.HandType);
        foreach (var card in playedCards)
        {
            _playedCardIdsThisAnte.Add(card.Id);
        }

        if (PokerHandPlayed.ContainsKey(result.HandType))
        {
            PokerHandPlayed[result.HandType]++;
        }
        else
        {
            PokerHandPlayed[result.HandType] = 1;
        }

        RoundScore += result.FinalScore;
        CurrentScore += result.FinalScore;

        ApplyJokerAfterHandEffects();

        OnPlayHand?.Invoke(playedCards);
        OnScore?.Invoke(result.FinalScore);

        if (result.LuckyMoneyWon > 0)
        {
            Money += result.LuckyMoneyWon;
        }

        var destroyedGlassCards = ProcessGlassCardDestruction(result.ScoringCards, result.JokerTriggerMessages);

        foreach (var card in playedCards)
        {
            Hand.Remove(card);
        }
        var survivingPlayedCards = playedCards.Except(destroyedGlassCards).ToList();
        DiscardPile.DiscardCards(survivingPlayedCards);

        ApplyTheHookPenalty();

        if (CurrentBlind != null && RoundScore >= CurrentBlind.ScoreToDefeat)
        {
            DefeatBlind();
            return OperationResult<ScoreCalculationResultDto>.Ok(result, $"Blind Defeated! Scored {result.FinalScore} with {result.HandName}.");
        }

        if (_currentHand <= 0)
        {
            GameOver();
            return OperationResult<ScoreCalculationResultDto>.Ok(result, $"Game Over! Hands exhausted before reaching target score.");
        }

        DrawCards(GetEffectiveMaxHandSize() - Hand.Count);

        return OperationResult<ScoreCalculationResultDto>.Ok(result, $"Played {result.HandName} for {result.FinalScore} points!");
    }

    public OperationResult DiscardCards(List<string> cardIds)
    {
        if (Phase != GameStatePhase.Playing)
        {
            return OperationResult.Fail($"Cannot discard cards while in {Phase} phase.");
        }

        if (_currentDiscard <= 0)
        {
            return OperationResult.Fail("No discards remaining.");
        }

        if (cardIds == null || cardIds.Count == 0 || cardIds.Count > 5)
        {
            return OperationResult.Fail("Must discard between 1 and 5 cards.");
        }

        var toDiscard = Hand.Where(c => cardIds.Contains(c.Id)).ToList();
        if (toDiscard.Count != cardIds.Count)
        {
            return OperationResult.Fail("One or more selected cards are not in hand.");
        }

        _currentDiscard--;

        foreach (var card in toDiscard)
        {
            Hand.Remove(card);
        }
        DiscardPile.DiscardCards(toDiscard);

        DrawCards(GetEffectiveMaxHandSize() - Hand.Count);

        return OperationResult.Ok($"Discarded {toDiscard.Count} card(s).");
    }

    public OperationResult<ScoreCalculationResultDto> GetScorePreview(List<string> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0 || cardIds.Count > 5)
        {
            return OperationResult<ScoreCalculationResultDto>.Fail("Select 1 to 5 cards for score preview.");
        }

        var playedCards = Hand.Where(c => cardIds.Contains(c.Id)).ToList();
        var remainingInHand = Hand.Except(playedCards).ToList();

        var result = _scoringService.CalculateScore(
            playedCards,
            remainingInHand,
            Deck.JokerCards,
            PokerHandLevels,
            CurrentBlind?.BlindId,
            Money,
            DiscardsRemaining,
            DrawPile.Count + DiscardPile.Count);
        return OperationResult<ScoreCalculationResultDto>.Ok(result, "Score preview calculated.");
    }

    public bool DefeatBlind()
    {
        if (CurrentBlind == null) return false;

        CurrentBlind.IsDefeated = true;
        OnBlindDefeated?.Invoke(CurrentBlind);

        Cashout();

        ApplyGoldCardEndOfRoundReward();
        ApplyJokerEndOfRoundEffects();

        _shopService.PopulateShop(Shop, CurrentAnte, PurchasedVouchers, CurrentAnteVoucher, IsAnteVoucherPurchased);
        Phase = GameStatePhase.InShop;
        OnShopOpen?.Invoke();

        return true;
    }

    public int Cashout()
    {
        if (CurrentBlind == null) return 0;

        int reward = CurrentBlind.RewardMoney;
        int remainingHandsMoney = _currentHand * 1;
        int maxInterest = PurchasedVouchers.Any(v => v.Effect == VoucherEffect.SeedMoney) ? 10 : 5;
        int interest = Math.Min(maxInterest, Money / 5);

        int totalCashout = reward + remainingHandsMoney + interest;
        Money += totalCashout;

        OnGetCashout?.Invoke();
        return totalCashout;
    }

    public OperationResult LeaveShop()
    {
        if (Phase != GameStatePhase.InShop)
        {
            return OperationResult.Fail("Cannot leave shop when not in Shop phase.");
        }

        Shop.OpenedBoosterPack = null;

        if (CurrentBlind == null)
        {
            Phase = GameStatePhase.SelectingBlind;
            return OperationResult.Ok("Proceeding to blind selection.");
        }

        if (CurrentBlind.BlindType == BlindType.Boss)
        {
            if (CurrentAnte >= 8)
            {
                Win();
                return OperationResult.Ok("Victory! You have defeated the Ante 8 Boss Blind!");
            }

            AdvanceAnte();
            NextRound();
            Phase = GameStatePhase.SelectingBlind;
            return OperationResult.Ok($"Advancing to Ante {CurrentAnte}!");
        }

        NextRound();
        Phase = GameStatePhase.SelectingBlind;
        return OperationResult.Ok("Proceeding to next blind selection.");
    }

    public bool AdvanceAnte()
    {
        CurrentAnte++;
        _playedCardIdsThisAnte.Clear();
        IsAnteVoucherPurchased = false;
        IsBossBlindRerolledThisAnte = false;
        CurrentAnteVoucher = _shopService.GenerateVoucherForAnte(CurrentAnte, PurchasedVouchers);
        GenerateBlindsForAnte(CurrentAnte);
        OnAnteAdvance?.Invoke(CurrentAnte);
        return true;
    }

    public bool NextRound()
    {
        CurrentRound++;
        OnNextRound?.Invoke(CurrentRound);
        return true;
    }

    public bool Win()
    {
        if (Phase == GameStatePhase.GameOver)
        {
            return false;
        }

        Phase = GameStatePhase.Victory;
        OnWinGame?.Invoke();
        return true;
    }

    public bool GameOver()
    {
        Phase = GameStatePhase.GameOver;
        OnGameOver?.Invoke();
        return true;
    }

    public OperationResult UseConsumable(string consumableId, List<string> targetCardIds)
    {
        var card = Deck.UsableCards.FirstOrDefault(c => c.Id == consumableId);
        if (card == null)
        {
            return OperationResult.Fail("Consumable card not found in inventory.");
        }

        bool success = false;
        string msg = string.Empty;

        if (card is TarotCard tarot)
        {
            success = _consumableHandler.UseTarot(this, tarot, targetCardIds, out msg);
            if (success)
            {
                if (tarot.Type != TarotType.TheFool) LastTarotUsed = tarot;
                Deck.UsableCards.Remove(card);
            }
        }
        else if (card is PlanetCard planet)
        {
            success = _consumableHandler.UsePlanet(this, planet, out msg);
            if (success)
            {
                LastPlanetUsed = planet;
                Deck.UsableCards.Remove(card);
            }
        }
        else if (card is SpectralCard spectral)
        {
            success = _consumableHandler.UseSpectral(this, spectral, out msg);
            if (success)
            {
                Deck.UsableCards.Remove(card);
            }
        }

        return new OperationResult(success, msg);
    }

    public OperationResult SellCard(string cardId)
    {
        var joker = Deck.JokerCards.FirstOrDefault(j => j.Id == cardId);
        if (joker != null)
        {
            Deck.JokerCards.Remove(joker);
            Money += joker.SellValue;

            LiftVerdantLeafDebuffs();

            return OperationResult.Ok($"Sold {joker.Name} for ${joker.SellValue}.");
        }

        var consumable = Deck.UsableCards.FirstOrDefault(c => c.Id == cardId);
        if (consumable != null)
        {
            Deck.UsableCards.Remove(consumable);
            int sellVal = Math.Max(1, consumable.Price / 2);
            Money += sellVal;
            return OperationResult.Ok($"Sold {consumable.Name} for ${sellVal}.");
        }

        return OperationResult.Fail("Card not found in Jokers or Consumables.");
    }

    public OperationResult ArrangeJokers(List<string> jokerIds)
    {
        if (jokerIds == null || jokerIds.Count != Deck.JokerCards.Count)
        {
            return OperationResult.Fail("Must provide all existing Joker IDs in the desired order.");
        }

        var reordered = new List<JokerCard>();
        foreach (var id in jokerIds)
        {
            var j = Deck.JokerCards.FirstOrDefault(x => x.Id == id);
            if (j == null) return OperationResult.Fail($"Joker with ID {id} not found.");
            reordered.Add(j);
        }

        Deck.JokerCards.Clear();
        Deck.JokerCards.AddRange(reordered);
        return OperationResult.Ok("Jokers reordered successfully.");
    }

    public OperationResult ArrangeConsumables(List<string> consumableIds)
    {
        if (consumableIds == null || consumableIds.Count != Deck.UsableCards.Count)
        {
            return OperationResult.Fail("Must provide all existing Consumable IDs in the desired order.");
        }

        var reordered = new List<IUsableCard>();
        foreach (var id in consumableIds)
        {
            var c = Deck.UsableCards.FirstOrDefault(x => x.Id == id);
            if (c == null) return OperationResult.Fail($"Consumable with ID {id} not found.");
            reordered.Add(c);
        }

        Deck.UsableCards.Clear();
        Deck.UsableCards.AddRange(reordered);
        return OperationResult.Ok("Consumables reordered successfully.");
    }

    public OperationResult BuyCardFromShop(string cardId)
    {
        if (Phase != GameStatePhase.InShop)
        {
            return OperationResult.Fail("Can only buy cards while in Shop phase.");
        }

        var joker = Shop.JokerCardOffers.FirstOrDefault(j => j.Id == cardId);
        if (joker != null)
        {
            if (Money < joker.Price) return OperationResult.Fail("Not enough money.");
            if (Deck.IsJokerContainerFull() && joker.Edition != JokerEdition.Negative) return OperationResult.Fail("Joker slots are full.");

            Money -= joker.Price;
            Shop.JokerCardOffers.Remove(joker);
            Deck.JokerCards.Add(joker);
            return OperationResult.Ok($"Purchased {joker.Name} for ${joker.Price}!");
        }

        var tarot = Shop.TarotCardOffers.FirstOrDefault(t => t.Id == cardId);
        if (tarot != null)
        {
            if (Money < tarot.Price) return OperationResult.Fail("Not enough money.");
            if (Deck.IsConsumableContainerFull()) return OperationResult.Fail("Consumable slots are full.");

            Money -= tarot.Price;
            Shop.TarotCardOffers.Remove(tarot);
            Deck.UsableCards.Add(tarot);
            return OperationResult.Ok($"Purchased {tarot.Name} for ${tarot.Price}!");
        }

        var planet = Shop.PlanetCardOffers.FirstOrDefault(p => p.Id == cardId);
        if (planet != null)
        {
            if (Money < planet.Price) return OperationResult.Fail("Not enough money.");
            if (Deck.IsConsumableContainerFull()) return OperationResult.Fail("Consumable slots are full.");

            Money -= planet.Price;
            Shop.PlanetCardOffers.Remove(planet);
            Deck.UsableCards.Add(planet);
            return OperationResult.Ok($"Purchased {planet.Name} for ${planet.Price}!");
        }

        var spectral = Shop.SpectralCardOffers.FirstOrDefault(s => s.Id == cardId);
        if (spectral != null)
        {
            if (Money < spectral.Price) return OperationResult.Fail("Not enough money.");
            if (Deck.IsConsumableContainerFull()) return OperationResult.Fail("Consumable slots are full.");

            Money -= spectral.Price;
            Shop.SpectralCardOffers.Remove(spectral);
            Deck.UsableCards.Add(spectral);
            return OperationResult.Ok($"Purchased {spectral.Name} for ${spectral.Price}!");
        }

        var playingCard = Shop.PlayingCardOffers.FirstOrDefault(p => p.Id == cardId);
        if (playingCard != null)
        {
            if (Money < playingCard.Price) return OperationResult.Fail("Not enough money.");

            Money -= playingCard.Price;
            Shop.PlayingCardOffers.Remove(playingCard);
            DrawPile.PlayingCards.Add(playingCard);
            OnAddPlayingCard?.Invoke(playingCard);
            return OperationResult.Ok($"Purchased {playingCard.Name} and added to deck!");
        }

        return OperationResult.Fail("Card offer not found in shop.");
    }

    public OperationResult RerollShop()
    {
        if (Phase != GameStatePhase.InShop)
        {
            return OperationResult.Fail("Can only reroll shop in Shop phase.");
        }

        int cost = Shop.RerollCost;
        if (PurchasedVouchers.Any(v => v.Effect == VoucherEffect.RerollSurplus))
        {
            cost = Math.Max(0, cost - 2);
        }

        if (Money < cost)
        {
            return OperationResult.Fail($"Not enough money to reroll (Costs ${cost}).");
        }

        Money -= cost;
        Shop.RerollCount++;
        _shopService.RerollShop(Shop, CurrentAnte, PurchasedVouchers);

        return OperationResult.Ok($"Shop rerolled for ${cost}. Next reroll costs ${Shop.RerollCost}.");
    }

    public OperationResult<BoosterPack> BuyBoosterPack(string boosterId)
    {
        if (Phase != GameStatePhase.InShop)
        {
            return OperationResult<BoosterPack>.Fail("Can only buy booster packs in Shop phase.");
        }

        var pack = Shop.BoosterPacks.FirstOrDefault(b => b.Id == boosterId);
        if (pack == null)
        {
            return OperationResult<BoosterPack>.Fail("Booster pack not found in shop.");
        }

        if (Money < pack.Price)
        {
            return OperationResult<BoosterPack>.Fail($"Not enough money (Costs ${pack.Price}).");
        }

        Money -= pack.Price;
        Shop.BoosterPacks.Remove(pack);

        var mostPlayedHand = PokerHandPlayed.Values.Any(v => v > 0)
            ? PokerHandPlayed.OrderByDescending(kv => kv.Value).First().Key
            : PokerHandType.HighCard;

        _shopService.OpenBoosterPack(pack, PurchasedVouchers, mostPlayedHand);
        Shop.OpenedBoosterPack = pack;

        EnsureHandAvailableForConsumableBooster(pack);

        return OperationResult<BoosterPack>.Ok(pack, $"Opened {pack.Name}! Pick {pack.MaxPick} card(s).");
    }

    public OperationResult SelectBoosterCard(string cardId)
    {
        if (Shop.OpenedBoosterPack == null)
        {
            return OperationResult.Fail("No booster pack is currently opened.");
        }

        var pack = Shop.OpenedBoosterPack;
        bool picked = false;
        string resultMessage = string.Empty;

        var joker = pack.JokerCards.FirstOrDefault(j => j.Id == cardId);
        if (joker != null)
        {
            if (Deck.IsJokerContainerFull() && joker.Edition != JokerEdition.Negative) return OperationResult.Fail("Joker slots are full.");
            Deck.JokerCards.Add(joker);
            pack.JokerCards.Remove(joker);
            picked = true;
            resultMessage = $"Added {joker.Name} to Jokers!";
        }

        if (!picked)
        {
            var tarot = pack.TarotCards.FirstOrDefault(t => t.Id == cardId);
            if (tarot != null)
            {
                if (Deck.IsConsumableContainerFull()) return OperationResult.Fail("Consumable slots are full.");
                Deck.UsableCards.Add(tarot);
                pack.TarotCards.Remove(tarot);
                picked = true;
                resultMessage = $"Added {tarot.Name} to Consumables!";
            }
        }

        if (!picked)
        {
            var planet = pack.PlanetCards.FirstOrDefault(p => p.Id == cardId);
            if (planet != null)
            {
                if (Deck.IsConsumableContainerFull()) return OperationResult.Fail("Consumable slots are full.");
                Deck.UsableCards.Add(planet);
                pack.PlanetCards.Remove(planet);
                picked = true;
                resultMessage = $"Added {planet.Name} to Consumables!";
            }
        }

        if (!picked)
        {
            var spectral = pack.SpectralCards.FirstOrDefault(s => s.Id == cardId);
            if (spectral != null)
            {
                if (Deck.IsConsumableContainerFull()) return OperationResult.Fail("Consumable slots are full.");
                Deck.UsableCards.Add(spectral);
                pack.SpectralCards.Remove(spectral);
                picked = true;
                resultMessage = $"Added {spectral.Name} to Consumables!";
            }
        }

        if (!picked)
        {
            var playingCard = pack.PlayingCards.FirstOrDefault(p => p.Id == cardId);
            if (playingCard != null)
            {
                DrawPile.PlayingCards.Add(playingCard);
                pack.PlayingCards.Remove(playingCard);
                picked = true;
                resultMessage = $"Added {playingCard.Name} to Deck!";
            }
        }

        if (!picked)
        {
            return OperationResult.Fail("Card not found in opened booster pack.");
        }

        pack.MaxPick--;
        int totalRemainingCards = pack.PlayingCards.Count + pack.TarotCards.Count + pack.PlanetCards.Count + pack.SpectralCards.Count + pack.JokerCards.Count;
        if (pack.MaxPick <= 0 || totalRemainingCards == 0)
        {
            Shop.OpenedBoosterPack = null;
        }

        return OperationResult.Ok(resultMessage);
    }

    public OperationResult SkipBoosterPack()
    {
        if (Shop.OpenedBoosterPack == null)
        {
            return OperationResult.Ok("No booster pack opened.");
        }

        Shop.OpenedBoosterPack = null;
        return OperationResult.Ok("Booster pack skipped.");
    }

    public OperationResult BuyVoucher(string voucherId)
    {
        if (Phase != GameStatePhase.InShop)
        {
            return OperationResult.Fail("Can only buy vouchers in Shop phase.");
        }

        if (Shop.Voucher == null || Shop.Voucher.Id != voucherId)
        {
            return OperationResult.Fail("Voucher not available in shop.");
        }

        var voucher = Shop.Voucher;
        if (Money < voucher.Price)
        {
            return OperationResult.Fail($"Not enough money for voucher (Costs ${voucher.Price}).");
        }

        Money -= voucher.Price;
        voucher.IsPurchased = true;
        PurchasedVouchers.Add(voucher);
        IsAnteVoucherPurchased = true;
        CurrentAnteVoucher = null;
        Shop.Voucher = null;

        if (voucher.Effect == VoucherEffect.Overstock)
        {
            Shop.MaxItemCardOffers = 3;
        }
        else if (voucher.Effect == VoucherEffect.CrystalBall)
        {
            Deck.MaxConsumableContainer++;
        }
        else if (voucher.Effect == VoucherEffect.Grabber)
        {
            MaxHands++;
        }
        else if (voucher.Effect == VoucherEffect.Wasteful)
        {
            MaxDiscards++;
        }
        else if (voucher.Effect == VoucherEffect.PaintBrush)
        {
            MaxHand++;
        }
        else if (voucher.Effect == VoucherEffect.Hieroglyph)
        {
            CurrentAnte = Math.Max(1, CurrentAnte - 1);
            MaxHands = Math.Max(1, MaxHands - 1);
        }

        return OperationResult.Ok($"Purchased {voucher.Name} voucher!");
    }

    public OperationResult RerollBossBlind()
    {
        if (!PurchasedVouchers.Any(v => v.Effect == VoucherEffect.DirectorsCut))
        {
            return OperationResult.Fail("Director's Cut voucher required to reroll Boss Blind.");
        }
        if (IsBossBlindRerolledThisAnte)
        {
            return OperationResult.Fail("Boss Blind can only be rerolled once per Ante.");
        }
        if (Money < 10)
        {
            return OperationResult.Fail("Not enough money to reroll Boss Blind (Costs $10).");
        }

        Money -= 10;
        IsBossBlindRerolledThisAnte = true;

        int baseScore = CurrentAnte switch
        {
            1 => 300,
            2 => 800,
            3 => 2000,
            4 => 5000,
            5 => 11000,
            6 => 20000,
            7 => 35000,
            8 => 50000,
            _ => (int)(50000 * Math.Pow(1.5, CurrentAnte - 8))
        };

        var newBoss = GenerateBossBlind(CurrentAnte, baseScore);
        if (BlindEnemies.TryGetValue(CurrentAnte, out var blinds))
        {
            var bossIdx = blinds.FindIndex(b => b.BlindType == BlindType.Boss);
            if (bossIdx >= 0)
            {
                blinds[bossIdx] = newBoss;
            }
        }

        if (CurrentBlind?.BlindType == BlindType.Boss)
        {
            CurrentBlind = newBoss;
        }

        return OperationResult.Ok($"Boss Blind rerolled to {newBoss.Name} for $10.");
    }

    public GameStateResponseDto GetGameState(string? message = null, ScoreCalculationResultDto? lastScore = null)
    {
        ShopDto? shopDto = null;
        if (Phase == GameStatePhase.InShop)
        {
            shopDto = new ShopDto
            {
                JokerCards = Shop.JokerCardOffers ?? new List<JokerCard>(),
                PlayingCards = Shop.PlayingCardOffers ?? new List<PlayingCard>(),
                TarotCards = Shop.TarotCardOffers ?? new List<TarotCard>(),
                PlanetCards = Shop.PlanetCardOffers ?? new List<PlanetCard>(),
                SpectralCards = Shop.SpectralCardOffers ?? new List<SpectralCard>(),
                BoosterPacks = Shop.BoosterPacks ?? new List<BoosterPack>(),
                Voucher = Shop.Voucher,
                OpenedBoosterPack = Shop.OpenedBoosterPack,
                RerollCost = Shop.RerollCost,
                RerollCount = Shop.RerollCount
            };
        }

        var allDeckCards = Hand.Concat(DrawPile.PlayingCards).Concat(DiscardPile.PlayingCards).ToList();
        var remainingCards = DrawPile.PlayingCards.Concat(DiscardPile.PlayingCards).ToList();

        return new GameStateResponseDto
        {
            SessionId = SessionId,
            Player = (Player)Player,
            Phase = Phase,
            CurrentAnte = CurrentAnte,
            MaxAnte = MaxAnte,
            CurrentRound = CurrentRound,
            CurrentBlind = CurrentBlind,
            AvailableBlinds = GetAvailableBlinds(),
            CurrentScore = RoundScore,
            TargetScore = CurrentBlind?.ScoreToDefeat ?? 0,
            Money = Money,
            HandsRemaining = _currentHand,
            MaxHands = MaxHands,
            DiscardsRemaining = _currentDiscard,
            MaxDiscards = MaxDiscards,
            Hand = Hand.ToList(),
            FullDeck = allDeckCards,
            RemainingCards = remainingCards,
            DeckRemainingCount = DrawPile.Count + DiscardPile.Count,
            DrawPileCount = DrawPile.Count,
            DiscardPileCount = DiscardPile.Count,
            Jokers = Deck.JokerCards.ToList(),
            MaxJokers = Deck.MaxJokerContainer,
            Consumables = Deck.UsableCards.ToList(),
            MaxConsumables = Deck.MaxConsumableContainer,
            PurchasedVouchers = PurchasedVouchers.ToList(),
            PokerHandLevels = new Dictionary<PokerHandType, int>(PokerHandLevels),
            PokerHandPlayed = new Dictionary<PokerHandType, int>(PokerHandPlayed),
            Shop = shopDto,
            LastScoreResult = lastScore,
            LastMessage = message
        };
    }
}
