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

        // Separate Stone cards (which have no rank/suit, but can be played) from standard cards
        var standardCards = playedCards.Where(c => c.Enhancement != EnhancePokerCard.StoneCards).ToList();
        var stoneCards = playedCards.Where(c => c.Enhancement == EnhancePokerCard.StoneCards).ToList();

        if (standardCards.Count == 0)
        {
            // All cards are stone cards
            return (PokerHandType.HighCard, playedCards, new List<PlayingCard>());
        }

        // Check Straight Flush (requires 5 cards)
        if (playedCards.Count >= 5 && standardCards.Count >= 5)
        {
            if (IsFlush(standardCards) && TryGetStraight(standardCards, out var straightCards))
            {
                return (PokerHandType.StraightFlush, straightCards, playedCards.Except(straightCards).ToList());
            }
        }

        // Group cards by rank
        var rankGroups = standardCards
            .GroupBy(c => c.Rank)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => (int)g.Key)
            .ToList();

        // Check Four of a Kind
        if (rankGroups.Any(g => g.Count() >= 4))
        {
            var fourGroup = rankGroups.First(g => g.Count() >= 4).Take(4).ToList();
            var unscored = playedCards.Except(fourGroup).ToList();
            return (PokerHandType.FourOfAKind, fourGroup, unscored);
        }

        // Check Full House (3 of one rank + 2 of another)
        if (rankGroups.Count >= 2 && rankGroups[0].Count() >= 3 && rankGroups[1].Count() >= 2)
        {
            var fullHouseCards = rankGroups[0].Take(3).Concat(rankGroups[1].Take(2)).ToList();
            var unscored = playedCards.Except(fullHouseCards).ToList();
            return (PokerHandType.FullHouse, fullHouseCards, unscored);
        }

        // Check Flush (5 cards of same suit)
        if (playedCards.Count >= 5 && standardCards.Count >= 5 && IsFlush(standardCards))
        {
            var flushCards = standardCards.Take(5).ToList();
            var unscored = playedCards.Except(flushCards).ToList();
            return (PokerHandType.Flush, flushCards, unscored);
        }

        // Check Straight (5 sequential ranks)
        if (playedCards.Count >= 5 && standardCards.Count >= 5 && TryGetStraight(standardCards, out var straightCardsResult))
        {
            var unscored = playedCards.Except(straightCardsResult).ToList();
            return (PokerHandType.Straight, straightCardsResult, unscored);
        }

        // Check Three of a Kind
        if (rankGroups.Any(g => g.Count() >= 3))
        {
            var threeGroup = rankGroups.First(g => g.Count() >= 3).Take(3).ToList();
            var unscored = playedCards.Except(threeGroup).ToList();
            return (PokerHandType.ThreeOfAKind, threeGroup, unscored);
        }

        // Check Two Pair
        if (rankGroups.Count >= 2 && rankGroups[0].Count() >= 2 && rankGroups[1].Count() >= 2)
        {
            var twoPairCards = rankGroups[0].Take(2).Concat(rankGroups[1].Take(2)).ToList();
            var unscored = playedCards.Except(twoPairCards).ToList();
            return (PokerHandType.TwoPair, twoPairCards, unscored);
        }

        // Check Pair
        if (rankGroups.Any(g => g.Count() >= 2))
        {
            var pairGroup = rankGroups.First(g => g.Count() >= 2).Take(2).ToList();
            var unscored = playedCards.Except(pairGroup).ToList();
            return (PokerHandType.Pair, pairGroup, unscored);
        }

        // High Card (Single highest card)
        var highestCard = standardCards.OrderByDescending(c => (int)c.Rank).First();
        var highCardList = new List<PlayingCard> { highestCard };
        var unscoredList = playedCards.Except(highCardList).ToList();
        return (PokerHandType.HighCard, highCardList, unscoredList);
    }

    private static bool IsFlush(List<PlayingCard> cards)
    {
        if (cards.Count < 5) return false;

        // Check for each suit whether all non-wild cards match it
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            bool allMatch = cards.All(c => c.Enhancement == EnhancePokerCard.WildCards || c.Suit == suit);
            if (allMatch) return true;
        }
        return false;
    }

    private static bool TryGetStraight(List<PlayingCard> cards, out List<PlayingCard> straightCards)
    {
        straightCards = new List<PlayingCard>();
        if (cards.Count < 5) return false;

        // Get distinct ranks ordered descending
        var distinctCards = cards
            .GroupBy(c => (int)c.Rank)
            .Select(g => g.First())
            .OrderByDescending(c => (int)c.Rank)
            .ToList();

        if (distinctCards.Count < 5) return false;

        // Standard high Ace or regular sequence check
        for (int i = 0; i <= distinctCards.Count - 5; i++)
        {
            var subset = distinctCards.Skip(i).Take(5).ToList();
            bool isSeq = true;
            for (int j = 0; j < 4; j++)
            {
                if ((int)subset[j].Rank - (int)subset[j + 1].Rank != 1)
                {
                    isSeq = false;
                    break;
                }
            }
            if (isSeq)
            {
                straightCards = subset;
                return true;
            }
        }

        // Check Ace-low straight (A, 5, 4, 3, 2)
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
