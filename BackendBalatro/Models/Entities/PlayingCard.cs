using BackendBalatro.Enums;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class PlayingCard : IPurchasableCard
{
    private string? _customName;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name
    {
        get => _customName ?? $"{Rank} of {Suit}";
        set => _customName = value;
    }
    public Suit Suit { get; set; }
    public Rank Rank { get; set; }
    public EnhancePokerCard Enhancement { get; set; } = EnhancePokerCard.None;
    public JokerEdition Edition { get; set; } = JokerEdition.Base;
    public int BaseChips { get; set; }
    public float BaseMult { get; set; } = 0f;
    public float BaseXMult { get; set; } = 1.0f;
    public int Price { get; set; } = 1;
    public bool IsDebuffed { get; set; } = false;

    public PlayingCard()
    {
    }

    public PlayingCard(Suit suit, Rank rank, EnhancePokerCard enhancement = EnhancePokerCard.None, int price = 1)
    {
        Suit = suit;
        Rank = rank;
        Enhancement = enhancement;
        Price = price;
        BaseChips = CalculateDefaultBaseChips(rank);
    }

    public static int CalculateDefaultBaseChips(Rank rank)
    {
        return rank switch
        {
            Rank.Two => 2,
            Rank.Three => 3,
            Rank.Four => 4,
            Rank.Five => 5,
            Rank.Six => 6,
            Rank.Seven => 7,
            Rank.Eight => 8,
            Rank.Nine => 9,
            Rank.Ten => 10,
            Rank.Jack => 10,
            Rank.Queen => 10,
            Rank.King => 10,
            Rank.Ace => 11,
            _ => (int)rank
        };
    }

    public int GetEffectiveChips()
    {
        if (IsDebuffed) return 0;
        int chips = BaseChips > 0 ? BaseChips : CalculateDefaultBaseChips(Rank);
        if (Enhancement == EnhancePokerCard.BonusCards) chips += 30;
        if (Enhancement == EnhancePokerCard.StoneCards) chips += 50;
        if (Edition == JokerEdition.Foil) chips += 50;
        return chips;
    }

    public float GetEffectiveMult()
    {
        if (IsDebuffed) return 0f;
        float mult = BaseMult;
        if (Enhancement == EnhancePokerCard.MultCards) mult += 4f;
        if (Edition == JokerEdition.Holographic) mult += 10f;
        return mult;
    }

    public float GetEffectiveXMult()
    {
        if (IsDebuffed) return 1f;
        float xmult = BaseXMult <= 0 ? 1f : BaseXMult;
        if (Enhancement == EnhancePokerCard.GlassCards) xmult *= 2f;
        if (Edition == JokerEdition.Polychrome) xmult *= 1.5f;
        return xmult;
    }
}
