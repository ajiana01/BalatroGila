using BackendBalatro.Enums;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class SpectralCard : IUsableCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; } = 4;
    public int SellValue => Math.Max(1, Price / 2);
    public SpectralType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    public SpectralCard()
    {
    }

    public SpectralCard(string name, int price, SpectralType type, string description = "")
    {
        Name = name;
        Price = price;
        Type = type;
        Description = string.IsNullOrEmpty(description) ? GetDefaultDescription(type) : description;
    }

    public static string GetDefaultDescription(SpectralType type)
    {
        return type switch
        {
            SpectralType.Familiar => "Destroy 1 random card in your hand, add 3 random Enhanced face cards.",
            SpectralType.Grim => "Destroy 1 random card in your hand, add 2 random Enhanced Aces.",
            SpectralType.Incantation => "Destroy 1 random card in your hand, add 4 random Enhanced numbered cards.",
            SpectralType.Wraith => "Creates a random Rare or Legendary Joker, sets money to $0.",
            SpectralType.Sigil => "Converts all cards in hand to a single random suit.",
            _ => "Mysterious Spectral power."
        };
    }
}
