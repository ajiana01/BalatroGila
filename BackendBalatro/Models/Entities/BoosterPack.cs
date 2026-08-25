using BackendBalatro.Enums;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class BoosterPack : IPurchasableCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; } = 4;
    public int MaxPick { get; set; } = 1;
    public int TotalCard { get; set; } = 3;
    public BoosterType BoosterPackType { get; set; } = BoosterType.Standard;
    public PackSize PackSize { get; set; } = PackSize.Normal;
    public bool IsOpened { get; set; } = false;

    public List<PlayingCard> PlayingCards { get; set; } = new();
    public List<TarotCard> TarotCards { get; set; } = new();
    public List<PlanetCard> PlanetCards { get; set; } = new();
    public List<SpectralCard> SpectralCards { get; set; } = new();
    public List<JokerCard> JokerCards { get; set; } = new();

    public BoosterPack()
    {
    }

    public BoosterPack(string name, int price, int maxPick, int totalCard, BoosterType type, PackSize size)
    {
        Name = name;
        Price = price;
        MaxPick = maxPick;
        TotalCard = totalCard;
        BoosterPackType = type;
        PackSize = size;
    }
}
