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
        Dictionary<PokerHandType, int> handLevels,
        BlindId? activeBlindId = null)
    {
        var (handType, scoringCards, unscoredCards) = _pokerHandEvaluator.Evaluate(playedCards);

        var stoneCards = playedCards.Where(c => c.Enhancement == EnhancePokerCard.StoneCards && !scoringCards.Contains(c)).ToList();
        if (stoneCards.Count > 0)
        {
            scoringCards.AddRange(stoneCards);
            unscoredCards.RemoveAll(c => stoneCards.Contains(c));
        }

        int level = handLevels.TryGetValue(handType, out int lvl) ? lvl : 1;
        var (baseChips, baseMult) = GetBaseChipsAndMult(handType, level);

        if (activeBlindId == BlindId.TheFlint)
        {
            baseChips = Math.Max(1, (int)Math.Floor(baseChips / 2.0));
            baseMult = Math.Max(1f, (float)Math.Floor(baseMult / 2.0));
        }

        int cardChips = 0;
        float cardMult = 0f;
        float cardXMult = 1.0f;
        int luckyMoney = 0;
        var triggerMessages = new List<string>();

        foreach (var card in scoringCards)
        {
            cardChips += card.GetEffectiveChips();
            cardMult += card.GetEffectiveMult();
            cardXMult *= card.GetEffectiveXMult();

            if (!card.IsDebuffed && card.Enhancement == EnhancePokerCard.LuckyCards)
            {
                var rng = new Random();
                if (rng.Next(5) == 0)
                {
                    cardMult += 20f;
                    triggerMessages.Add($"{card.Name} (Lucky): +20 Mult!");
                }
                if (rng.Next(15) == 0)
                {
                    luckyMoney += 20;
                    triggerMessages.Add($"{card.Name} (Lucky): +$20!");
                }
            }
        }

        foreach (var card in handCardsRemaining)
        {
            if (!card.IsDebuffed && card.Enhancement == EnhancePokerCard.SteelCards)
            {
                cardXMult *= 1.5f;
            }
        }

        int totalChips = baseChips + cardChips;
        float totalMult = (baseMult + cardMult) * cardXMult;

        int jokerChips = 0;
        float jokerMult = 0f;
        float jokerXMult = 1.0f;
        var jokerTriggers = new List<JokerTriggerEffectDto>();

        for (int i = 0; i < jokers.Count; i++)
        {
            var joker = jokers[i];
            int thisJokerChips = 0;
            float thisJokerMult = 0f;
            float thisJokerXMult = 1.0f;
            var thisJokerMessages = new List<string>();

            if (joker.Edition == JokerEdition.Foil)
            {
                thisJokerChips += 50;
                thisJokerMessages.Add("+50 Chips");
            }
            else if (joker.Edition == JokerEdition.Holographic)
            {
                thisJokerMult += 10f;
                thisJokerMessages.Add("+10 Mult");
            }
            else if (joker.Edition == JokerEdition.Polychrome)
            {
                thisJokerXMult *= 1.5f;
                thisJokerMessages.Add("X1.5 Mult");
            }

            if (joker.ChipsValue > 0)
            {
                thisJokerChips += joker.ChipsValue;
                thisJokerMessages.Add($"+{joker.ChipsValue} Chips");
            }
            if (joker.MultValue > 0)
            {
                thisJokerMult += joker.MultValue;
                thisJokerMessages.Add($"+{joker.MultValue} Mult");
            }
            if (joker.XMultValue > 1.0f)
            {
                thisJokerXMult *= joker.XMultValue;
                thisJokerMessages.Add($"X{joker.XMultValue} Mult");
            }

            ApplySpecificJokerLogic(joker, handType, playedCards, scoringCards, handCardsRemaining, ref thisJokerChips, ref thisJokerMult, ref thisJokerXMult, thisJokerMessages);

            jokerChips += thisJokerChips;
            jokerMult += thisJokerMult;
            jokerXMult *= thisJokerXMult;

            if (thisJokerMessages.Count > 0)
            {
                string combinedMsg = string.Join(", ", thisJokerMessages);
                jokerTriggers.Add(new JokerTriggerEffectDto
                {
                    JokerId = joker.Id,
                    JokerIndex = i,
                    Message = combinedMsg,
                    ChipsAdded = thisJokerChips,
                    MultAdded = thisJokerMult,
                    XMultMultiplied = thisJokerXMult
                });
                triggerMessages.Add($"{joker.Name}: {combinedMsg}");
            }
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
            JokerTriggerMessages = triggerMessages,
            JokerTriggers = jokerTriggers,
            LuckyMoneyWon = luckyMoney
        };
    }

    private static void ApplySpecificJokerLogic(
        JokerCard joker,
        PokerHandType handType,
        List<PlayingCard> playedCards,
        List<PlayingCard> scoringCards,
        List<PlayingCard> handCardsRemaining,
        ref int jokerChips,
        ref float jokerMult,
        ref float jokerXMult,
        List<string> triggerMessages)
    {
        switch (joker.JokerId)
        {
            case JokerId.ScaryFace:
                int faceCount = scoringCards.Count(c => !c.IsDebuffed && (c.Rank == Rank.Jack || c.Rank == Rank.Queen || c.Rank == Rank.King));
                if (faceCount > 0)
                {
                    jokerChips += faceCount * 30;
                    triggerMessages.Add($"Scary Face: +{faceCount * 30} Chips for {faceCount} face cards");
                }
                break;

            case JokerId.SmileyFace:
                int smileyFaceCount = scoringCards.Count(c => !c.IsDebuffed && (c.Rank == Rank.Jack || c.Rank == Rank.Queen || c.Rank == Rank.King));
                if (smileyFaceCount > 0)
                {
                    jokerMult += smileyFaceCount * 5;
                    triggerMessages.Add($"Smiley Face: +{smileyFaceCount * 5} Mult for {smileyFaceCount} face cards");
                }
                break;

            case JokerId.Photograph:
                var firstFace = scoringCards.FirstOrDefault(c => !c.IsDebuffed && (c.Rank == Rank.Jack || c.Rank == Rank.Queen || c.Rank == Rank.King));
                if (firstFace != null)
                {
                    jokerXMult *= 2.0f;
                    triggerMessages.Add($"Photograph: X2 Mult for first face card ({firstFace.Rank})");
                }
                break;

            case JokerId.HalfJoker:
                if (playedCards.Count <= 3)
                {
                    jokerMult += 20;
                    triggerMessages.Add("Half Joker: +20 Mult for <=3 cards played");
                }
                break;

            case JokerId.RaisedFist:
                if (handCardsRemaining.Count > 0)
                {
                    var lowestRank = handCardsRemaining.Min(c => (int)c.Rank);
                    jokerMult += lowestRank * 2;
                    triggerMessages.Add($"Raised Fist: +{lowestRank * 2} Mult from lowest card in hand");
                }
                break;

            case JokerId.Baron:
                int kingHeldCount = handCardsRemaining.Count(c => !c.IsDebuffed && c.Rank == Rank.King);
                if (kingHeldCount > 0)
                {
                    float baronMult = (float)Math.Pow(1.5, kingHeldCount);
                    jokerXMult *= baronMult;
                    triggerMessages.Add($"Baron: X{baronMult:0.##} Mult from {kingHeldCount} King(s) in hand");
                }
                break;

            case JokerId.Blackboard:
                if (handCardsRemaining.Count > 0 && handCardsRemaining.All(c => c.Suit == Suit.Spades || c.Suit == Suit.Clubs))
                {
                    jokerXMult *= 3.0f;
                    triggerMessages.Add("Blackboard: X3 Mult for Spades & Clubs only held in hand");
                }
                break;

            case JokerId.GreedyJoker:
                int diamondCount = scoringCards.Count(c => !c.IsDebuffed && (c.Suit == Suit.Diamonds || c.Enhancement == EnhancePokerCard.WildCards));
                if (diamondCount > 0)
                {
                    jokerMult += diamondCount * 4;
                    triggerMessages.Add($"Greedy Joker: +{diamondCount * 4} Mult from Diamonds");
                }
                break;

            case JokerId.LustyJoker:
                int heartCount = scoringCards.Count(c => !c.IsDebuffed && (c.Suit == Suit.Hearts || c.Enhancement == EnhancePokerCard.WildCards));
                if (heartCount > 0)
                {
                    jokerMult += heartCount * 4;
                    triggerMessages.Add($"Lusty Joker: +{heartCount * 4} Mult from Hearts");
                }
                break;

            case JokerId.WrathfulJoker:
                int spadeCount = scoringCards.Count(c => !c.IsDebuffed && (c.Suit == Suit.Spades || c.Enhancement == EnhancePokerCard.WildCards));
                if (spadeCount > 0)
                {
                    jokerMult += spadeCount * 4;
                    triggerMessages.Add($"Wrathful Joker: +{spadeCount * 4} Mult from Spades");
                }
                break;

            case JokerId.GluttonousJoker:
                int clubCount = scoringCards.Count(c => !c.IsDebuffed && (c.Suit == Suit.Clubs || c.Enhancement == EnhancePokerCard.WildCards));
                if (clubCount > 0)
                {
                    jokerMult += clubCount * 4;
                    triggerMessages.Add($"Gluttonous Joker: +{clubCount * 4} Mult from Clubs");
                }
                break;

            case JokerId.Fibonacci:
                int fibCount = scoringCards.Count(c => !c.IsDebuffed && (c.Rank == Rank.Ace || c.Rank == Rank.Two || c.Rank == Rank.Three || c.Rank == Rank.Five || c.Rank == Rank.Eight));
                if (fibCount > 0)
                {
                    jokerMult += fibCount * 8;
                    triggerMessages.Add($"Fibonacci: +{fibCount * 8} Mult from Fibonacci cards");
                }
                break;

            case JokerId.EvenSteven:
                int evenCount = scoringCards.Count(c => !c.IsDebuffed && ((int)c.Rank % 2 == 0) && (int)c.Rank <= 10);
                if (evenCount > 0)
                {
                    jokerMult += evenCount * 4;
                    triggerMessages.Add($"Even Steven: +{evenCount * 4} Mult from Even cards");
                }
                break;

            case JokerId.OddTodd:
                int oddCount = scoringCards.Count(c => !c.IsDebuffed && (c.Rank == Rank.Ace || ((int)c.Rank % 2 == 1 && (int)c.Rank <= 9)));
                if (oddCount > 0)
                {
                    jokerChips += oddCount * 31;
                    triggerMessages.Add($"Odd Todd: +{oddCount * 31} Chips from Odd cards");
                }
                break;

            case JokerId.Scholar:
                int aceCount = scoringCards.Count(c => !c.IsDebuffed && c.Rank == Rank.Ace);
                if (aceCount > 0)
                {
                    jokerChips += aceCount * 20;
                    jokerMult += aceCount * 4;
                    triggerMessages.Add($"Scholar: +{aceCount * 20} Chips and +{aceCount * 4} Mult from Aces");
                }
                break;

            case JokerId.WalkieTalkie:
                int wtCount = scoringCards.Count(c => !c.IsDebuffed && (c.Rank == Rank.Ten || c.Rank == Rank.Four));
                if (wtCount > 0)
                {
                    jokerChips += wtCount * 10;
                    jokerMult += wtCount * 4;
                    triggerMessages.Add($"Walkie Talkie: +{wtCount * 10} Chips and +{wtCount * 4} Mult from 10s and 4s");
                }
                break;

            case JokerId.JollyJoker:
                if (handType == PokerHandType.Pair || handType == PokerHandType.TwoPair || handType == PokerHandType.FullHouse)
                {
                    jokerMult += 8;
                    triggerMessages.Add("Jolly Joker: +8 Mult for Pair");
                }
                break;

            case JokerId.ZanyJoker:
                if (handType == PokerHandType.ThreeOfAKind || handType == PokerHandType.FullHouse)
                {
                    jokerMult += 12;
                    triggerMessages.Add("Zany Joker: +12 Mult for Three of a Kind");
                }
                break;

            case JokerId.MadJoker:
                if (handType == PokerHandType.TwoPair)
                {
                    jokerMult += 10;
                    triggerMessages.Add("Mad Joker: +10 Mult for Two Pair");
                }
                break;

            case JokerId.CrazyJoker:
                if (handType == PokerHandType.Straight || handType == PokerHandType.StraightFlush)
                {
                    jokerMult += 12;
                    triggerMessages.Add("Crazy Joker: +12 Mult for Straight");
                }
                break;

            case JokerId.DrollJoker:
                if (handType == PokerHandType.Flush || handType == PokerHandType.StraightFlush)
                {
                    jokerMult += 10;
                    triggerMessages.Add("Droll Joker: +10 Mult for Flush");
                }
                break;

            case JokerId.SlyJoker:
                if (handType == PokerHandType.Pair || handType == PokerHandType.TwoPair || handType == PokerHandType.FullHouse)
                {
                    jokerChips += 50;
                    triggerMessages.Add("Sly Joker: +50 Chips for Pair");
                }
                break;

            case JokerId.WilyJoker:
                if (handType == PokerHandType.ThreeOfAKind || handType == PokerHandType.FullHouse)
                {
                    jokerChips += 100;
                    triggerMessages.Add("Wily Joker: +100 Chips for Three of a Kind");
                }
                break;

            case JokerId.CleverJoker:
                if (handType == PokerHandType.TwoPair)
                {
                    jokerChips += 80;
                    triggerMessages.Add("Clever Joker: +80 Chips for Two Pair");
                }
                break;

            case JokerId.DeviousJoker:
                if (handType == PokerHandType.Straight || handType == PokerHandType.StraightFlush)
                {
                    jokerChips += 100;
                    triggerMessages.Add("Devious Joker: +100 Chips for Straight");
                }
                break;

            case JokerId.CraftyJoker:
                if (handType == PokerHandType.Flush || handType == PokerHandType.StraightFlush)
                {
                    jokerChips += 80;
                    triggerMessages.Add("Crafty Joker: +80 Chips for Flush");
                }
                break;

            case JokerId.Misprint:
                var rnd = new Random();
                int misprintMult = rnd.Next(0, 24);
                jokerMult += misprintMult;
                triggerMessages.Add($"Misprint: +{misprintMult} Mult");
                break;
        }
    }
}
