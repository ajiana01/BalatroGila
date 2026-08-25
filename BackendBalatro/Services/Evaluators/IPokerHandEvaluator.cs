using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Evaluators;

public interface IPokerHandEvaluator
{
    (PokerHandType HandType, List<PlayingCard> ScoringCards, List<PlayingCard> UnscoredCards) Evaluate(List<PlayingCard> playedCards);
}
