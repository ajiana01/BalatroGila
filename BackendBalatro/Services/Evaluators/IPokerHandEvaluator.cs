using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Evaluators;

public interface IPokerHandEvaluator
{
    PokerHandEvaluationResult Evaluate(List<PlayingCard> playedCards);
}
