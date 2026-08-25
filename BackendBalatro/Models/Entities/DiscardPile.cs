using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class DiscardPile : IDiscardPile
{
    private readonly List<PlayingCard> _cards = new();

    public List<PlayingCard> PlayingCards => _cards;
    public int Count => _cards.Count;

    public void DiscardCards(IEnumerable<PlayingCard> cards)
    {
        _cards.AddRange(cards);
    }

    public List<PlayingCard> PullAllCards()
    {
        var pulled = new List<PlayingCard>(_cards);
        _cards.Clear();
        return pulled;
    }

    public void Clear()
    {
        _cards.Clear();
    }
}
