using BackendBalatro.Enums;

namespace BackendBalatro.Models.Interfaces;

public interface IBlind
{
    string Name { get; set; }
    BlindType BlindType { get; set; }
    int ScoreToDefeat { get; set; }
    bool IsDefeated { get; set; }
}
