using BackendBalatro.Enums;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class PlanetCard : IUsableCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; } = 3;
    public int SellValue => Math.Max(1, Price / 2);
    public PokerHandType PokerHandType { get; set; }
    public string Description { get; set; } = string.Empty;

    public PlanetCard()
    {
    }

    public PlanetCard(string name, PokerHandType pokerHand, int price = 3, string description = "")
    {
        Name = name;
        PokerHandType = pokerHand;
        Price = price;
        Description = string.IsNullOrEmpty(description) ? $"Upgrades {pokerHand} level (+Chips and +Mult)." : description;
    }

    public static PlanetCard CreateForHand(PokerHandType handType)
    {
        return handType switch
        {
            PokerHandType.HighCard => new PlanetCard("Pluto", PokerHandType.HighCard),
            PokerHandType.Pair => new PlanetCard("Mercury", PokerHandType.Pair),
            PokerHandType.TwoPair => new PlanetCard("Uranus", PokerHandType.TwoPair),
            PokerHandType.ThreeOfAKind => new PlanetCard("Venus", PokerHandType.ThreeOfAKind),
            PokerHandType.Straight => new PlanetCard("Saturn", PokerHandType.Straight),
            PokerHandType.Flush => new PlanetCard("Jupiter", PokerHandType.Flush),
            PokerHandType.FullHouse => new PlanetCard("Earth", PokerHandType.FullHouse),
            PokerHandType.FourOfAKind => new PlanetCard("Mars", PokerHandType.FourOfAKind),
            PokerHandType.StraightFlush => new PlanetCard("Neptune", PokerHandType.StraightFlush),
            _ => new PlanetCard("Planet", handType)
        };
    }
}
