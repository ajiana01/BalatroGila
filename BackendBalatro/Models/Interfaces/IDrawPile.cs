using BackendBalatro.Models.Entities;

namespace BackendBalatro.Models.Interfaces;

public interface IDrawPile
{
    List<PlayingCard> PlayingCards { get; }
    int Count { get; }
    void AddCards(IEnumerable<PlayingCard> cards);
    List<PlayingCard> DrawCards(int count);
    void Shuffle();
    void Clear();
}
