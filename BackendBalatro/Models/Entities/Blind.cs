using BackendBalatro.Enums;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class Blind : IBlind
{
    public int Id { get; set; }
    public BlindId BlindId { get; set; } = BlindId.SmallBlind;
    public string Name { get; set; } = string.Empty;
    public BlindType BlindType { get; set; }
    public int ScoreToDefeat { get; set; }
    public int RewardMoney { get; set; }
    public bool IsDefeated { get; set; } = false;
    public string Description { get; set; } = string.Empty;

    public Blind()
    {
    }

    public Blind(BlindId blindId, string name, BlindType blindType, int score, int reward = 3, string description = "")
    {
        BlindId = blindId;
        Name = name;
        BlindType = blindType;
        ScoreToDefeat = score;
        RewardMoney = reward;
        Description = description;
    }

    public Blind(string name, BlindType blindType, int score, int reward = 3, string description = "")
    {
        Name = name;
        BlindType = blindType;
        ScoreToDefeat = score;
        RewardMoney = reward;
        Description = description;
        BlindId = blindType switch
        {
            BlindType.Small => BlindId.SmallBlind,
            BlindType.Big => BlindId.BigBlind,
            _ => BlindId.SmallBlind
        };
    }
}
