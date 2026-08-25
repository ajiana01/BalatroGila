using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class DrawPile : IDrawPile
{
    private readonly List<PlayingCard> _cards = new();
    private static readonly Random _random = new();

    public List<PlayingCard> PlayingCards => _cards;
    public int Count => _cards.Count;

    public void AddCards(IEnumerable<PlayingCard> cards)
    {
        _cards.AddRange(cards);
    }

    public List<PlayingCard> DrawCards(int count)
    {
        var drawn = new List<PlayingCard>();
        int toDraw = Math.Min(count, _cards.Count);
        for (int i = 0; i < toDraw; i++)
        {
            var card = _cards[0];
            _cards.RemoveAt(0);
            drawn.Add(card);
        }
        return drawn;
    }

    public void Shuffle()
    {
        int n = _cards.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (_cards[k], _cards[n]) = (_cards[n], _cards[k]);
        }
    }

    public void Clear()
    {
        _cards.Clear();
    }
}
