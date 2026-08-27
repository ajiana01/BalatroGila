using BackendBalatro.Enums;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class TarotCard : IUsableCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; } = 3;
    public int SellValue => Math.Max(1, Price / 2);
    public TarotType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    public TarotCard()
    {
    }

    public TarotCard(string name, int price, TarotType type, string description = "")
    {
        Name = name;
        Price = price;
        Type = type;
        Description = string.IsNullOrEmpty(description) ? GetDefaultDescription(type) : description;
    }

    public static string GetDefaultDescription(TarotType type)
    {
        return type switch
        {
            TarotType.TheFool => "Creates the last Tarot or Planet card used during this run.",
            TarotType.TheMagician => "Enhances up to 2 selected cards to Lucky Cards.",
            TarotType.TheHighPriestess => "Creates up to 2 random Planet cards.",
            TarotType.TheEmpress => "Enhances up to 2 selected cards to Mult Cards (+4 Mult).",
            TarotType.TheEmperor => "Creates up to 2 random Tarot cards.",
            TarotType.TheHierophant => "Enhances up to 2 selected cards to Bonus Cards (+30 Chips).",
            TarotType.TheLovers => "Enhances 1 selected card into a Wild Card.",
            TarotType.TheChariot => "Enhances 1 selected card into a Steel Card (+1.5x Mult in hand).",
            TarotType.Justice => "Enhances 1 selected card into a Glass Card (x2 Mult, may break).",
            TarotType.TheHermit => "Doubles money (Max of $20).",
            TarotType.TheWheelFortune => "1 in 4 chance to add Foil, Holographic, or Polychrome edition to a random Joker.",
            TarotType.Strength => "Increases rank of up to 2 selected cards by 1.",
            TarotType.TheHangedMan => "Destroys up to 2 selected cards.",
            TarotType.Death => "Converts the left selected card into the right selected card.",
            TarotType.TheTemperance => "Gives the total sell value of all current Jokers (Max of $50).",
            TarotType.TheDevil => "Enhances 1 selected card into a Gold Card ($3 held at end of round).",
            TarotType.TheTower => "Enhances 1 selected card into a Stone Card (+50 Chips, no rank/suit).",
            TarotType.TheStar => "Converts up to 3 selected cards to Diamonds.",
            TarotType.TheMoon => "Converts up to 3 selected cards to Clubs.",
            TarotType.TheSun => "Converts up to 3 selected cards to Hearts.",
            TarotType.TheWorld => "Converts up to 3 selected cards to Spades.",
            TarotType.Judgement => "Creates a random Joker card.",
            _ => "Mysterious Tarot power."
        };
    }
}
