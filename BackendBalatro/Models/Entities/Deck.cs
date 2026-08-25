using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class Deck
{
    public int MaxJokerContainer { get; set; } = 5;
    public int MaxConsumableContainer { get; set; } = 2;
    public List<JokerCard> JokerCards { get; set; } = new();
    public List<IUsableCard> UsableCards { get; set; } = new();

    public Deck()
    {
    }

    public Deck(int sizeJokerContainer, int sizeConsumableContainer)
    {
        MaxJokerContainer = sizeJokerContainer;
        MaxConsumableContainer = sizeConsumableContainer;
    }

    public bool IsJokerContainerFull()
    {
        // Negative jokers do not count towards the cap
        int nonNegativeCount = JokerCards.Count(j => j.Edition != Enums.JokerEdition.Negative);
        return nonNegativeCount >= MaxJokerContainer;
    }

    public bool IsConsumableContainerFull()
    {
        return UsableCards.Count >= MaxConsumableContainer;
    }
}
