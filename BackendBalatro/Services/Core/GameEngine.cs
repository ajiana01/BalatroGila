using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;
using BackendBalatro.Models.Interfaces;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;

namespace BackendBalatro.Services.Core;

public class GameEngine : IGameEngine
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

    public Dictionary<int, List<Blind>> BlindEnemies { get; } = new();
    public Blind? CurrentBlind { get; private set; }
    public GameStatePhase Phase { get; private set; } = GameStatePhase.SelectingBlind;

    public Dictionary<PokerHandType, int> PokerHandLevels { get; } = new();
    public Dictionary<PokerHandType, int> PokerHandPlayed { get; } = new();

    public TarotCard? LastTarotUsed { get; set; }
    public PlanetCard? LastPlanetUsed { get; set; }

    // Events
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

    public GameEngine(
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

        Deck.JokerCards.Clear();
        Deck.UsableCards.Clear();
        PurchasedVouchers.Clear();
        Hand.Clear();
        DiscardPile.Clear();
        DrawPile.Clear();

        InitializePokerHandLevels();

        // 1. Inisialisasi 52 kartu standar secara hardcoded (4 suits x 13 ranks)
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

        // 2. Generate Blinds for Ante 1
        GenerateBlindsForAnte(CurrentAnte);

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
            new Blind("Small Blind", BlindType.Small, baseScore, 3) { Id = 1 },
            new Blind("Big Blind", BlindType.Big, (int)(baseScore * 1.5), 4) { Id = 2 },
            GenerateBossBlind(ante, baseScore * 2)
        };

        BlindEnemies[ante] = blinds;
    }

    private static Blind GenerateBossBlind(int ante, int score)
    {
        var bossNames = new[]
        {
            ("The Club", "All Club cards are debuffed", "club_debuff"),
            ("The Goad", "All Spade cards are debuffed", "spade_debuff"),
            ("The Window", "All Diamond cards are debuffed", "diamond_debuff"),
            ("The Head", "All Heart cards are debuffed", "heart_debuff"),
            ("The Pillar", "Cards played previously this Ante are debuffed", "pillar"),
            ("The Psychic", "Must play exactly 5 cards", "psychic"),
            ("The Needle", "Play only 1 hand", "needle"),
            ("The Water", "Start with 0 discards", "water"),
            ("The Wall", "Extra large blind (4x base score)", "wall"),
            ("Cerulean Bell", "Forces 1 card to always be selected", "cerulean")
        };

        var random = new Random();
        var selected = bossNames[random.Next(bossNames.Length)];
        int finalScore = selected.Item3 == "wall" ? score * 2 : score;

        return new Blind(selected.Item1, BlindType.Boss, finalScore, 5, selected.Item2)
        {
            Id = 3,
            BossKey = selected.Item3
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

        // Reset Deck & Hand for the round
        RecycleAllCardsToDrawPile();

        // Apply boss debuffs
        ApplyBossBlindEffects(selected);

        RoundScore = 0;

        // Apply voucher effects to hands & discards
        int bonusHands = PurchasedVouchers.Count(v => v.Effect == VoucherEffect.Grabber);
        int bonusDiscards = PurchasedVouchers.Count(v => v.Effect == VoucherEffect.Wasteful);
        if (selected.BossKey == "needle")
        {
            _currentHand = 1;
        }
        else
        {
            _currentHand = MaxHands + bonusHands;
        }

        if (selected.BossKey == "water")
        {
            _currentDiscard = 0;
        }
        else
        {
            _currentDiscard = MaxDiscards + bonusDiscards;
        }

        // Draw initial hand
        DrawCards(MaxHand);

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
            if (boss.BossKey == "club_debuff" && card.Suit == Suit.Clubs) card.IsDebuffed = true;
            if (boss.BossKey == "spade_debuff" && card.Suit == Suit.Spades) card.IsDebuffed = true;
            if (boss.BossKey == "diamond_debuff" && card.Suit == Suit.Diamonds) card.IsDebuffed = true;
            if (boss.BossKey == "heart_debuff" && card.Suit == Suit.Hearts) card.IsDebuffed = true;
        }
    }

    public List<PlayingCard> DrawCards(int count)
    {
        int needed = Math.Min(count, MaxHand - Hand.Count);
        if (needed <= 0) return new List<PlayingCard>();

        if (DrawPile.Count < needed && DiscardPile.Count > 0)
        {
            var recycled = DiscardPile.PullAllCards();
            DrawPile.AddCards(recycled);
            DrawPile.Shuffle();
        }

        var drawn = DrawPile.DrawCards(needed);
        Hand.AddRange(drawn);
        return drawn;
    }

    public (bool Success, string Message, ScoreCalculationResultDto? Result) PlayHand(List<string> cardIds)
    {
        if (Phase != GameStatePhase.Playing)
        {
            return (false, $"Cannot play hand while in {Phase} phase.", null);
        }

        if (cardIds == null || cardIds.Count == 0 || cardIds.Count > 5)
        {
            return (false, "Must play between 1 and 5 cards.", null);
        }

        if (CurrentBlind?.BossKey == "psychic" && cardIds.Count != 5)
        {
            return (false, "The Psychic forces you to play exactly 5 cards!", null);
        }

        var playedCards = Hand.Where(c => cardIds.Contains(c.Id)).ToList();
        if (playedCards.Count != cardIds.Count)
        {
            return (false, "One or more selected cards are not in hand.", null);
        }

        // Deduct 1 hand
        _currentHand--;

        var remainingInHand = Hand.Except(playedCards).ToList();

        // Calculate score
        var result = _scoringService.CalculateScore(playedCards, remainingInHand, Deck.JokerCards, PokerHandLevels);

        // Update statistics
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

        OnPlayHand?.Invoke(playedCards);
        OnScore?.Invoke(result.FinalScore);

        // Remove played cards from Hand and add to DiscardPile
        foreach (var card in playedCards)
        {
            Hand.Remove(card);
        }
        DiscardPile.DiscardCards(playedCards);

        // Check if Blind is defeated
        if (CurrentBlind != null && RoundScore >= CurrentBlind.ScoreToDefeat)
        {
            DefeatBlind();
            return (true, $"Blind Defeated! Scored {result.FinalScore} with {result.HandName}.", result);
        }

        // Check for Game Over
        if (_currentHand <= 0)
        {
            GameOver();
            return (true, $"Game Over! Hands exhausted before reaching target score.", result);
        }

        // Round continues: draw replacement cards
        DrawCards(MaxHand - Hand.Count);

        return (true, $"Played {result.HandName} for {result.FinalScore} points!", result);
    }

    public (bool Success, string Message) DiscardCards(List<string> cardIds)
    {
        if (Phase != GameStatePhase.Playing)
        {
            return (false, $"Cannot discard cards while in {Phase} phase.");
        }

        if (_currentDiscard <= 0)
        {
            return (false, "No discards remaining.");
        }

        if (cardIds == null || cardIds.Count == 0 || cardIds.Count > 5)
        {
            return (false, "Must discard between 1 and 5 cards.");
        }

        var toDiscard = Hand.Where(c => cardIds.Contains(c.Id)).ToList();
        if (toDiscard.Count != cardIds.Count)
        {
            return (false, "One or more selected cards are not in hand.");
        }

        _currentDiscard--;

        foreach (var card in toDiscard)
        {
            Hand.Remove(card);
        }
        DiscardPile.DiscardCards(toDiscard);

        DrawCards(MaxHand - Hand.Count);

        return (true, $"Discarded {toDiscard.Count} card(s).");
    }

    public (bool Success, string Message, ScoreCalculationResultDto? Result) GetScorePreview(List<string> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0 || cardIds.Count > 5)
        {
            return (false, "Select 1 to 5 cards for score preview.", null);
        }

        var playedCards = Hand.Where(c => cardIds.Contains(c.Id)).ToList();
        var remainingInHand = Hand.Except(playedCards).ToList();

        var result = _scoringService.CalculateScore(playedCards, remainingInHand, Deck.JokerCards, PokerHandLevels);
        return (true, "Score preview calculated.", result);
    }

    public bool DefeatBlind()
    {
        if (CurrentBlind == null) return false;

        CurrentBlind.IsDefeated = true;
        OnBlindDefeated?.Invoke(CurrentBlind);

        Cashout();

        // Check for Gold cards held in hand at end of round (+$3 each)
        int goldCardCount = Hand.Count(c => !c.IsDebuffed && c.Enhancement == EnhancePokerCard.GoldCards);
        if (goldCardCount > 0)
        {
            Money += goldCardCount * 3;
        }

        // Open shop
        _shopService.PopulateShop(Shop, CurrentAnte, PurchasedVouchers);
        Phase = GameStatePhase.InShop;
        OnShopOpen?.Invoke();

        return true;
    }

    public int Cashout()
    {
        if (CurrentBlind == null) return 0;

        int reward = CurrentBlind.RewardMoney;
        int remainingHandsMoney = _currentHand * 1;
        int interest = Math.Min(5, Money / 5);

        int totalCashout = reward + remainingHandsMoney + interest;
        Money += totalCashout;

        OnGetCashout?.Invoke();
        return totalCashout;
    }

    public (bool Success, string Message) LeaveShop()
    {
        if (Phase != GameStatePhase.InShop)
        {
            return (false, "Cannot leave shop when not in Shop phase.");
        }

        Shop.OpenedBoosterPack = null;

        if (CurrentBlind == null)
        {
            Phase = GameStatePhase.SelectingBlind;
            return (true, "Proceeding to blind selection.");
        }

        if (CurrentBlind.BlindType == BlindType.Boss)
        {
            // WIN CONDITION STATIS: Boss Blind on Ante 8 defeated!
            if (CurrentAnte >= 8)
            {
                Win();
                return (true, "Victory! You have defeated the Ante 8 Boss Blind!");
            }

            // Advance to next Ante
            AdvanceAnte();
            NextRound();
            Phase = GameStatePhase.SelectingBlind;
            return (true, $"Advancing to Ante {CurrentAnte}!");
        }

        // Previous blind was Small or Big -> Next Blind in current Ante
        NextRound();
        Phase = GameStatePhase.SelectingBlind;
        return (true, "Proceeding to next blind selection.");
    }

    public bool AdvanceAnte()
    {
        CurrentAnte++;
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

    // Consumables & Joker Actions
    public (bool Success, string Message) UseConsumable(string consumableId, List<string> targetCardIds)
    {
        var card = Deck.UsableCards.FirstOrDefault(c => c.Id == consumableId);
        if (card == null)
        {
            return (false, "Consumable card not found in inventory.");
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

        return (success, msg);
    }

    public (bool Success, string Message) SellCard(string cardId)
    {
        var joker = Deck.JokerCards.FirstOrDefault(j => j.Id == cardId);
        if (joker != null)
        {
            Deck.JokerCards.Remove(joker);
            Money += joker.SellValue;
            return (true, $"Sold {joker.Name} for ${joker.SellValue}.");
        }

        var consumable = Deck.UsableCards.FirstOrDefault(c => c.Id == cardId);
        if (consumable != null)
        {
            Deck.UsableCards.Remove(consumable);
            int sellVal = Math.Max(1, consumable.Price / 2);
            Money += sellVal;
            return (true, $"Sold {consumable.Name} for ${sellVal}.");
        }

        return (false, "Card not found in Jokers or Consumables.");
    }

    public (bool Success, string Message) ArrangeJokers(List<string> jokerIds)
    {
        if (jokerIds == null || jokerIds.Count != Deck.JokerCards.Count)
        {
            return (false, "Must provide all existing Joker IDs in the desired order.");
        }

        var reordered = new List<JokerCard>();
        foreach (var id in jokerIds)
        {
            var j = Deck.JokerCards.FirstOrDefault(x => x.Id == id);
            if (j == null) return (false, $"Joker with ID {id} not found.");
            reordered.Add(j);
        }

        Deck.JokerCards.Clear();
        Deck.JokerCards.AddRange(reordered);
        return (true, "Jokers reordered successfully.");
    }

    public (bool Success, string Message) ArrangeConsumables(List<string> consumableIds)
    {
        if (consumableIds == null || consumableIds.Count != Deck.UsableCards.Count)
        {
            return (false, "Must provide all existing Consumable IDs in the desired order.");
        }

        var reordered = new List<IUsableCard>();
        foreach (var id in consumableIds)
        {
            var c = Deck.UsableCards.FirstOrDefault(x => x.Id == id);
            if (c == null) return (false, $"Consumable with ID {id} not found.");
            reordered.Add(c);
        }

        Deck.UsableCards.Clear();
        Deck.UsableCards.AddRange(reordered);
        return (true, "Consumables reordered successfully.");
    }

    // Shop Actions
    public (bool Success, string Message) BuyCardFromShop(string cardId)
    {
        if (Phase != GameStatePhase.InShop)
        {
            return (false, "Can only buy cards while in Shop phase.");
        }

        var joker = Shop.JokerCardOffers.FirstOrDefault(j => j.Id == cardId);
        if (joker != null)
        {
            if (Money < joker.Price) return (false, "Not enough money.");
            if (Deck.IsJokerContainerFull() && joker.Edition != JokerEdition.Negative) return (false, "Joker slots are full.");

            Money -= joker.Price;
            Shop.JokerCardOffers.Remove(joker);
            Deck.JokerCards.Add(joker);
            return (true, $"Purchased {joker.Name} for ${joker.Price}!");
        }

        var tarot = Shop.TarotCardOffers.FirstOrDefault(t => t.Id == cardId);
        if (tarot != null)
        {
            if (Money < tarot.Price) return (false, "Not enough money.");
            if (Deck.IsConsumableContainerFull()) return (false, "Consumable slots are full.");

            Money -= tarot.Price;
            Shop.TarotCardOffers.Remove(tarot);
            Deck.UsableCards.Add(tarot);
            return (true, $"Purchased {tarot.Name} for ${tarot.Price}!");
        }

        var planet = Shop.PlanetCardOffers.FirstOrDefault(p => p.Id == cardId);
        if (planet != null)
        {
            if (Money < planet.Price) return (false, "Not enough money.");
            if (Deck.IsConsumableContainerFull()) return (false, "Consumable slots are full.");

            Money -= planet.Price;
            Shop.PlanetCardOffers.Remove(planet);
            Deck.UsableCards.Add(planet);
            return (true, $"Purchased {planet.Name} for ${planet.Price}!");
        }

        var spectral = Shop.SpectralCardOffers.FirstOrDefault(s => s.Id == cardId);
        if (spectral != null)
        {
            if (Money < spectral.Price) return (false, "Not enough money.");
            if (Deck.IsConsumableContainerFull()) return (false, "Consumable slots are full.");

            Money -= spectral.Price;
            Shop.SpectralCardOffers.Remove(spectral);
            Deck.UsableCards.Add(spectral);
            return (true, $"Purchased {spectral.Name} for ${spectral.Price}!");
        }

        var playingCard = Shop.PlayingCardOffers.FirstOrDefault(p => p.Id == cardId);
        if (playingCard != null)
        {
            if (Money < playingCard.Price) return (false, "Not enough money.");

            Money -= playingCard.Price;
            Shop.PlayingCardOffers.Remove(playingCard);
            DrawPile.PlayingCards.Add(playingCard);
            OnAddPlayingCard?.Invoke(playingCard);
            return (true, $"Purchased {playingCard.Name} and added to deck!");
        }

        return (false, "Card offer not found in shop.");
    }

    public (bool Success, string Message) RerollShop()
    {
        if (Phase != GameStatePhase.InShop)
        {
            return (false, "Can only reroll shop in Shop phase.");
        }

        int cost = Shop.RerollCost;
        if (PurchasedVouchers.Any(v => v.Effect == VoucherEffect.RerollSurplus))
        {
            cost = Math.Max(0, cost - 2);
        }

        if (Money < cost)
        {
            return (false, $"Not enough money to reroll (Costs ${cost}).");
        }

        Money -= cost;
        Shop.RerollCount++;
        _shopService.RerollShop(Shop, CurrentAnte, PurchasedVouchers);

        return (true, $"Shop rerolled for ${cost}. Next reroll costs ${Shop.RerollCost}.");
    }

    public (bool Success, string Message, BoosterPack? Pack) BuyBoosterPack(string boosterId)
    {
        if (Phase != GameStatePhase.InShop)
        {
            return (false, "Can only buy booster packs in Shop phase.", null);
        }

        var pack = Shop.BoosterPacks.FirstOrDefault(b => b.Id == boosterId);
        if (pack == null)
        {
            return (false, "Booster pack not found in shop.", null);
        }

        if (Money < pack.Price)
        {
            return (false, $"Not enough money (Costs ${pack.Price}).", null);
        }

        Money -= pack.Price;
        Shop.BoosterPacks.Remove(pack);
        _shopService.OpenBoosterPack(pack);
        Shop.OpenedBoosterPack = pack;

        return (true, $"Opened {pack.Name}! Pick {pack.MaxPick} card(s).", pack);
    }

    public (bool Success, string Message) SelectBoosterCard(string cardId)
    {
        if (Shop.OpenedBoosterPack == null)
        {
            return (false, "No booster pack is currently opened.");
        }

        var pack = Shop.OpenedBoosterPack;
        bool picked = false;
        string resultMessage = string.Empty;

        // Check in each category of opened pack
        var joker = pack.JokerCards.FirstOrDefault(j => j.Id == cardId);
        if (joker != null)
        {
            if (Deck.IsJokerContainerFull() && joker.Edition != JokerEdition.Negative) return (false, "Joker slots are full.");
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
                if (Deck.IsConsumableContainerFull()) return (false, "Consumable slots are full.");
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
                if (Deck.IsConsumableContainerFull()) return (false, "Consumable slots are full.");
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
                if (Deck.IsConsumableContainerFull()) return (false, "Consumable slots are full.");
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
            return (false, "Card not found in opened booster pack.");
        }

        pack.MaxPick--;
        int totalRemainingCards = pack.PlayingCards.Count + pack.TarotCards.Count + pack.PlanetCards.Count + pack.SpectralCards.Count + pack.JokerCards.Count;
        if (pack.MaxPick <= 0 || totalRemainingCards == 0)
        {
            Shop.OpenedBoosterPack = null;
        }

        return (true, resultMessage);
    }

    public (bool Success, string Message) SkipBoosterPack()
    {
        if (Shop.OpenedBoosterPack == null)
        {
            return (true, "No booster pack opened.");
        }

        Shop.OpenedBoosterPack = null;
        return (true, "Booster pack skipped.");
    }

    public (bool Success, string Message) BuyVoucher(string voucherId)
    {
        if (Phase != GameStatePhase.InShop)
        {
            return (false, "Can only buy vouchers in Shop phase.");
        }

        if (Shop.Voucher == null || Shop.Voucher.Id != voucherId)
        {
            return (false, "Voucher not available in shop.");
        }

        var voucher = Shop.Voucher;
        if (Money < voucher.Price)
        {
            return (false, $"Not enough money for voucher (Costs ${voucher.Price}).");
        }

        Money -= voucher.Price;
        voucher.IsPurchased = true;
        PurchasedVouchers.Add(voucher);
        Shop.Voucher = null;

        // Apply permanent voucher effects
        if (voucher.Effect == VoucherEffect.Overstock)
        {
            Shop.MaxItemCardOffers++;
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
        else if (voucher.Effect == VoucherEffect.Hieroglyph)
        {
            CurrentAnte = Math.Max(1, CurrentAnte - 1);
            MaxHands = Math.Max(1, MaxHands - 1);
        }

        return (true, $"Purchased {voucher.Name} voucher!");
    }

    public GameStateResponseDto GetGameState(string? message = null, ScoreCalculationResultDto? lastScore = null)
    {
        ShopDto? shopDto = null;
        if (Phase == GameStatePhase.InShop)
        {
            shopDto = new ShopDto
            {
                JokerCards = Shop.JokerCardOffers,
                PlayingCards = Shop.PlayingCardOffers,
                TarotCards = Shop.TarotCardOffers,
                PlanetCards = Shop.PlanetCardOffers,
                SpectralCards = Shop.SpectralCardOffers,
                BoosterPacks = Shop.BoosterPacks,
                Voucher = Shop.Voucher,
                OpenedBoosterPack = Shop.OpenedBoosterPack,
                RerollCost = Shop.RerollCost,
                RerollCount = Shop.RerollCount
            };
        }

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
