using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Evaluators;

public class PokerHandEvaluator : IPokerHandEvaluator
{
    public (PokerHandType HandType, List<PlayingCard> ScoringCards, List<PlayingCard> UnscoredCards) Evaluate(List<PlayingCard> playedCards)
    {
        if (playedCards == null || playedCards.Count == 0)
        {
            return (PokerHandType.HighCard, new List<PlayingCard>(), new List<PlayingCard>());
        }

        var standardCards = GetStandardCards(playedCards);

        if (standardCards.Count == 0)
        {
            return (PokerHandType.HighCard, playedCards, new List<PlayingCard>());
        }

        if (playedCards.Count >= 5 && standardCards.Count >= 5)
        {
            if (IsFlush(standardCards) && TryGetStraight(standardCards, out var straightFlushCards))
            {
                return (PokerHandType.StraightFlush, straightFlushCards, playedCards.Except(straightFlushCards).ToList());
            }
        }

        var rankGroups = GetRankGroupsDescending(standardCards);

        if (rankGroups.Any(g => g.Count() >= 4))
        {
            var fourGroup = rankGroups.First(g => g.Count() >= 4).Take(4).ToList();
            var unscored = playedCards.Except(fourGroup).ToList();
            return (PokerHandType.FourOfAKind, fourGroup, unscored);
        }

        if (rankGroups.Count >= 2 && rankGroups[0].Count() >= 3 && rankGroups[1].Count() >= 2)
        {
            var fullHouseCards = rankGroups[0].Take(3).Concat(rankGroups[1].Take(2)).ToList();
            var unscored = playedCards.Except(fullHouseCards).ToList();
            return (PokerHandType.FullHouse, fullHouseCards, unscored);
        }

        if (playedCards.Count >= 5 && standardCards.Count >= 5 && IsFlush(standardCards))
        {
            var flushCards = standardCards.Take(5).ToList();
            var unscored = playedCards.Except(flushCards).ToList();
            return (PokerHandType.Flush, flushCards, unscored);
        }

        if (playedCards.Count >= 5 && standardCards.Count >= 5 && TryGetStraight(standardCards, out var straightCardsResult))
        {
            var unscored = playedCards.Except(straightCardsResult).ToList();
            return (PokerHandType.Straight, straightCardsResult, unscored);
        }

        if (rankGroups.Any(g => g.Count() >= 3))
        {
            var threeGroup = rankGroups.First(g => g.Count() >= 3).Take(3).ToList();
            var unscored = playedCards.Except(threeGroup).ToList();
            return (PokerHandType.ThreeOfAKind, threeGroup, unscored);
        }

        if (rankGroups.Count >= 2 && rankGroups[0].Count() >= 2 && rankGroups[1].Count() >= 2)
        {
            var twoPairCards = rankGroups[0].Take(2).Concat(rankGroups[1].Take(2)).ToList();
            var unscored = playedCards.Except(twoPairCards).ToList();
            return (PokerHandType.TwoPair, twoPairCards, unscored);
        }

        if (rankGroups.Any(g => g.Count() >= 2))
        {
            var pairGroup = rankGroups.First(g => g.Count() >= 2).Take(2).ToList();
            var unscored = playedCards.Except(pairGroup).ToList();
            return (PokerHandType.Pair, pairGroup, unscored);
        }

        var highestCard = standardCards.OrderByDescending(c => (int)c.Rank).First();
        var highCardList = new List<PlayingCard> { highestCard };
        var unscoredList = playedCards.Except(highCardList).ToList();
        return (PokerHandType.HighCard, highCardList, unscoredList);
    }

    private static List<PlayingCard> GetStandardCards(List<PlayingCard> cards)
    {
        return cards.Where(c => c.Enhancement != EnhancePokerCard.StoneCards).ToList();
    }

    private static List<IGrouping<Rank, PlayingCard>> GetRankGroupsDescending(List<PlayingCard> cards)
    {
        return cards
            .GroupBy(c => c.Rank)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => (int)g.Key)
            .ToList();
    }

    private static bool IsFlush(List<PlayingCard> cards)
    {
        if (cards.Count < 5) return false;

        return Enum.GetValues<Suit>().Any(suit => IsSuitFlush(cards, suit));
    }

    private static bool IsSuitFlush(List<PlayingCard> cards, Suit suit)
    {
        return cards.All(c => c.Enhancement == EnhancePokerCard.WildCards || c.Suit == suit);
    }

    private static bool TryGetStraight(List<PlayingCard> cards, out List<PlayingCard> straightCards)
    {
        straightCards = new List<PlayingCard>();
        if (cards.Count < 5) return false;

        var distinctCards = GetDistinctRankCardsDescending(cards);
        if (distinctCards.Count < 5) return false;

        if (TryGetSequentialStraight(distinctCards, out straightCards))
        {
            return true;
        }

        return TryGetAceLowStraight(distinctCards, out straightCards);
    }

    private static List<PlayingCard> GetDistinctRankCardsDescending(List<PlayingCard> cards)
    {
        return cards
            .GroupBy(c => (int)c.Rank)
            .Select(g => g.First())
            .OrderByDescending(c => (int)c.Rank)
            .ToList();
    }

    private static bool TryGetSequentialStraight(List<PlayingCard> distinctCards, out List<PlayingCard> straightCards)
    {
        straightCards = new List<PlayingCard>();

        for (int i = 0; i <= distinctCards.Count - 5; i++)
        {
            var subset = distinctCards.Skip(i).Take(5).ToList();
            if (IsSequentialFiveCards(subset))
            {
                straightCards = subset;
                return true;
            }
        }

        return false;
    }

    private static bool IsSequentialFiveCards(List<PlayingCard> cards)
    {
        for (int j = 0; j < 4; j++)
        {
            if ((int)cards[j].Rank - (int)cards[j + 1].Rank != 1)
            {
                return false;
            }
        }
        return true;
    }

    private static bool TryGetAceLowStraight(List<PlayingCard> distinctCards, out List<PlayingCard> straightCards)
    {
        straightCards = new List<PlayingCard>();

        var ace = distinctCards.FirstOrDefault(c => c.Rank == Rank.Ace);
        var five = distinctCards.FirstOrDefault(c => c.Rank == Rank.Five);
        var four = distinctCards.FirstOrDefault(c => c.Rank == Rank.Four);
        var three = distinctCards.FirstOrDefault(c => c.Rank == Rank.Three);
        var two = distinctCards.FirstOrDefault(c => c.Rank == Rank.Two);

        if (ace != null && five != null && four != null && three != null && two != null)
        {
            straightCards = new List<PlayingCard> { five, four, three, two, ace };
            return true;
        }

        return false;
    }
}
