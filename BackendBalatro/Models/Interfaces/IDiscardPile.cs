using BackendBalatro.Models.Entities;

namespace BackendBalatro.Models.Interfaces;

public interface IDiscardPile
{
    List<PlayingCard> PlayingCards { get; }
    int Count { get; }
    void DiscardCards(IEnumerable<PlayingCard> cards);
    List<PlayingCard> PullAllCards();
    void Clear();
}
