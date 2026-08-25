using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Models.DTOs;

public class ScoreCalculationResultDto
{
    public PokerHandType HandType { get; set; }
    public string HandName => HandType.ToString();
    public int HandLevel { get; set; }
    public int BaseChips { get; set; }
    public float BaseMult { get; set; }
    public int CardChips { get; set; }
    public float CardMult { get; set; }
    public float CardXMult { get; set; } = 1.0f;
    public int JokerChips { get; set; }
    public float JokerMult { get; set; }
    public float JokerXMult { get; set; } = 1.0f;
    public int TotalChips { get; set; }
    public float TotalMult { get; set; }
    public int FinalScore { get; set; }
    public List<PlayingCard> ScoringCards { get; set; } = new();
    public List<PlayingCard> UnscoredCards { get; set; } = new();
    public List<string> JokerTriggerMessages { get; set; } = new();
}
