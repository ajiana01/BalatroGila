using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Evaluators;

public class ScoringService : IScoringService
{
    private readonly IPokerHandEvaluator _pokerHandEvaluator;

    public ScoringService(IPokerHandEvaluator pokerHandEvaluator)
    {
        _pokerHandEvaluator = pokerHandEvaluator;
    }

    public (int BaseChips, float BaseMult) GetBaseChipsAndMult(PokerHandType handType, int level)
    {
        level = Math.Max(1, level);
        var (baseChips, baseMult) = GetDefaultBaseValues(handType);
        var (lvlChips, lvlMult) = GetLevelUpBonus(handType);

        int totalChips = baseChips + (level - 1) * lvlChips;
        float totalMult = baseMult + (level - 1) * lvlMult;

        return (totalChips, totalMult);
    }

    private static (int BaseChips, float BaseMult) GetDefaultBaseValues(PokerHandType handType)
    {
        return handType switch
        {
            PokerHandType.HighCard => (5, 1f),
            PokerHandType.Pair => (10, 2f),
            PokerHandType.TwoPair => (20, 2f),
            PokerHandType.ThreeOfAKind => (30, 3f),
            PokerHandType.Straight => (30, 4f),
            PokerHandType.Flush => (35, 4f),
            PokerHandType.FullHouse => (40, 4f),
            PokerHandType.FourOfAKind => (60, 7f),
            PokerHandType.StraightFlush => (100, 8f),
            _ => (5, 1f)
        };
    }

    public (int LevelUpChips, float LevelUpMult) GetLevelUpBonus(PokerHandType handType)
    {
        return handType switch
        {
            PokerHandType.HighCard => (10, 1f),
            PokerHandType.Pair => (15, 1f),
            PokerHandType.TwoPair => (20, 1f),
            PokerHandType.ThreeOfAKind => (20, 2f),
            PokerHandType.Straight => (30, 3f),
            PokerHandType.Flush => (15, 2f),
            PokerHandType.FullHouse => (25, 2f),
            PokerHandType.FourOfAKind => (30, 3f),
            PokerHandType.StraightFlush => (40, 4f),
            _ => (10, 1f)
        };
    }

    public ScoreCalculationResultDto CalculateScore(
        List<PlayingCard> playedCards,
        List<PlayingCard> handCardsRemaining,
        List<JokerCard> jokers,
        Dictionary<PokerHandType, int> handLevels)
    {
        var (handType, scoringCards, unscoredCards) = _pokerHandEvaluator.Evaluate(playedCards);

        // Also add any Stone cards from playedCards to scoring cards if not already included
        var stoneCards = playedCards.Where(c => c.Enhancement == EnhancePokerCard.StoneCards && !scoringCards.Contains(c)).ToList();
        if (stoneCards.Count > 0)
        {
            scoringCards.AddRange(stoneCards);
            unscoredCards.RemoveAll(c => stoneCards.Contains(c));
        }

        int level = handLevels.TryGetValue(handType, out int lvl) ? lvl : 1;
        var (baseChips, baseMult) = GetBaseChipsAndMult(handType, level);

        int cardChips = 0;
        float cardMult = 0f;
        float cardXMult = 1.0f;

        // 1. Scoring Cards Evaluation
        foreach (var card in scoringCards)
        {
            cardChips += card.GetEffectiveChips();
            cardMult += card.GetEffectiveMult();
            cardXMult *= card.GetEffectiveXMult();
        }

        // 2. Held in Hand Cards Evaluation (e.g. Steel cards)
        foreach (var card in handCardsRemaining)
        {
            if (!card.IsDebuffed && card.Enhancement == EnhancePokerCard.SteelCards)
            {
                cardXMult *= 1.5f;
            }
        }

        int totalChips = baseChips + cardChips;
        float totalMult = (baseMult + cardMult) * cardXMult;

        // 3. Jokers Evaluation
        int jokerChips = 0;
        float jokerMult = 0f;
        float jokerXMult = 1.0f;
        var triggerMessages = new List<string>();

        foreach (var joker in jokers)
        {
            // Joker Editions
            if (joker.Edition == JokerEdition.Foil)
            {
                jokerChips += 50;
                triggerMessages.Add($"{joker.Name} (Foil): +50 Chips");
            }
            else if (joker.Edition == JokerEdition.Holographic)
            {
                jokerMult += 10f;
                triggerMessages.Add($"{joker.Name} (Holo): +10 Mult");
            }
            else if (joker.Edition == JokerEdition.Polychrome)
            {
                jokerXMult *= 1.5f;
                triggerMessages.Add($"{joker.Name} (Poly): X1.5 Mult");
            }

            // Joker Modifier Types
            if (joker.ChipsValue > 0)
            {
                jokerChips += joker.ChipsValue;
                triggerMessages.Add($"{joker.Name}: +{joker.ChipsValue} Chips");
            }
            if (joker.MultValue > 0)
            {
                jokerMult += joker.MultValue;
                triggerMessages.Add($"{joker.Name}: +{joker.MultValue} Mult");
            }
            if (joker.XMultValue > 1.0f)
            {
                jokerXMult *= joker.XMultValue;
                triggerMessages.Add($"{joker.Name}: X{joker.XMultValue} Mult");
            }

            // Specific Joker Key Logic
            ApplySpecificJokerLogic(joker, playedCards, scoringCards, handCardsRemaining, ref jokerChips, ref jokerMult, ref jokerXMult, triggerMessages);
        }

        totalChips += jokerChips;
        totalMult += jokerMult;
        totalMult *= jokerXMult;

        int finalScore = (int)Math.Floor(totalChips * totalMult);

        return new ScoreCalculationResultDto
        {
            HandType = handType,
            HandLevel = level,
            BaseChips = baseChips,
            BaseMult = baseMult,
            CardChips = cardChips,
            CardMult = cardMult,
            CardXMult = cardXMult,
            JokerChips = jokerChips,
            JokerMult = jokerMult,
            JokerXMult = jokerXMult,
            TotalChips = totalChips,
            TotalMult = totalMult,
            FinalScore = finalScore,
            ScoringCards = scoringCards,
            UnscoredCards = unscoredCards,
            JokerTriggerMessages = triggerMessages
        };
    }

    private static void ApplySpecificJokerLogic(
        JokerCard joker,
        List<PlayingCard> playedCards,
        List<PlayingCard> scoringCards,
        List<PlayingCard> handCardsRemaining,
        ref int jokerChips,
        ref float jokerMult,
        ref float jokerXMult,
        List<string> triggerMessages)
    {
        string key = joker.JokerKey.ToLowerInvariant();
        if (string.IsNullOrEmpty(key))
        {
            key = joker.Name.Replace(" ", "").ToLowerInvariant();
        }

        switch (key)
        {
            case "scaryface":
                // Face cards give +30 Chips when scored
                int faceCount = scoringCards.Count(c => !c.IsDebuffed && (c.Rank == Rank.Jack || c.Rank == Rank.Queen || c.Rank == Rank.King));
                if (faceCount > 0)
                {
                    jokerChips += faceCount * 30;
                    triggerMessages.Add($"Scary Face: +{faceCount * 30} Chips for {faceCount} face cards");
                }
                break;

            case "halfjoker":
                // +20 Mult if played hand contains 3 or fewer cards
                if (playedCards.Count <= 3)
                {
                    jokerMult += 20;
                    triggerMessages.Add("Half Joker: +20 Mult for <=3 cards played");
                }
                break;

            case "raisedfist":
                // Adds double the rank of lowest card held in hand to Mult
                if (handCardsRemaining.Count > 0)
                {
                    var lowestRank = handCardsRemaining.Min(c => (int)c.Rank);
                    jokerMult += lowestRank * 2;
                    triggerMessages.Add($"Raised Fist: +{lowestRank * 2} Mult from lowest card in hand");
                }
                break;

            case "abstractjoker":
                // +3 Mult for each Joker card
                // Handled in caller if needed or default Mult
                break;

            case "greedyjoker":
                int diamondCount = scoringCards.Count(c => !c.IsDebuffed && (c.Suit == Suit.Diamonds || c.Enhancement == EnhancePokerCard.WildCards));
                if (diamondCount > 0)
                {
                    jokerMult += diamondCount * 4;
                    triggerMessages.Add($"Greedy Joker: +{diamondCount * 4} Mult from Diamonds");
                }
                break;

            case "lustyjoker":
                int heartCount = scoringCards.Count(c => !c.IsDebuffed && (c.Suit == Suit.Hearts || c.Enhancement == EnhancePokerCard.WildCards));
                if (heartCount > 0)
                {
                    jokerMult += heartCount * 4;
                    triggerMessages.Add($"Lusty Joker: +{heartCount * 4} Mult from Hearts");
                }
                break;

            case "wrathfuljoker":
                int spadeCount = scoringCards.Count(c => !c.IsDebuffed && (c.Suit == Suit.Spades || c.Enhancement == EnhancePokerCard.WildCards));
                if (spadeCount > 0)
                {
                    jokerMult += spadeCount * 4;
                    triggerMessages.Add($"Wrathful Joker: +{spadeCount * 4} Mult from Spades");
                }
                break;

            case "gluttonousjoker":
                int clubCount = scoringCards.Count(c => !c.IsDebuffed && (c.Suit == Suit.Clubs || c.Enhancement == EnhancePokerCard.WildCards));
                if (clubCount > 0)
                {
                    jokerMult += clubCount * 4;
                    triggerMessages.Add($"Gluttonous Joker: +{clubCount * 4} Mult from Clubs");
                }
                break;

            case "fibonacci":
                // Each played Ace, 2, 3, 5, or 8 gives +8 Mult when scored
                int fibCount = scoringCards.Count(c => !c.IsDebuffed && (c.Rank == Rank.Ace || c.Rank == Rank.Two || c.Rank == Rank.Three || c.Rank == Rank.Five || c.Rank == Rank.Eight));
                if (fibCount > 0)
                {
                    jokerMult += fibCount * 8;
                    triggerMessages.Add($"Fibonacci: +{fibCount * 8} Mult from Fibonacci cards");
                }
                break;
        }
    }
}
