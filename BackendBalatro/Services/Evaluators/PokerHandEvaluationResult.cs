using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Evaluators;

public sealed class PokerHandEvaluationResult
{
    public PokerHandEvaluationResult(
        PokerHandType handType,
        List<PlayingCard> scoringCards,
        List<PlayingCard> unscoredCards)
    {
        HandType = handType;
        ScoringCards = scoringCards;
        UnscoredCards = unscoredCards;
    }

    public PokerHandType HandType { get; }
    public List<PlayingCard> ScoringCards { get; }
    public List<PlayingCard> UnscoredCards { get; }
}
