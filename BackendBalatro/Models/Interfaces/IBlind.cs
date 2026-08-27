using BackendBalatro.Enums;

namespace BackendBalatro.Models.Interfaces;

public interface IBlind
{
    int Id { get; set; }
    BlindId BlindId { get; set; }
    string Name { get; set; }
    BlindType BlindType { get; set; }
    int ScoreToDefeat { get; set; }
    int RewardMoney { get; set; }
    bool IsDefeated { get; set; }
    string Description { get; set; }
}
