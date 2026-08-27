using BackendBalatro.Enums;
using BackendBalatro.Models.DTOs;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Evaluators;

public interface IScoringService
{
    (int BaseChips, float BaseMult) GetBaseChipsAndMult(PokerHandType handType, int level);
    (int LevelUpChips, float LevelUpMult) GetLevelUpBonus(PokerHandType handType);
    ScoreCalculationResultDto CalculateScore(
        List<PlayingCard> playedCards,
        List<PlayingCard> handCardsRemaining,
        List<JokerCard> jokers,
        Dictionary<PokerHandType, int> handLevels,
        BlindId? activeBlindId = null);
}
