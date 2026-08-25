using BackendBalatro.Enums;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class JokerCard : IPurchasableCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public JokerEdition Edition { get; set; } = JokerEdition.Base;
    public JokerRarity Rarity { get; set; } = JokerRarity.Common;
    public JokerModifierType JokerModifierType { get; set; } = JokerModifierType.AdditionMultiplier;
    public int ChipsValue { get; set; } = 0;
    public float MultValue { get; set; } = 0f;
    public float XMultValue { get; set; } = 1.0f;
    public int MoneyValue { get; set; } = 0;
    public int Price { get; set; } = 4;
    public int SellValue => Math.Max(1, Price / 2);
    public string Description { get; set; } = string.Empty;
    public string JokerKey { get; set; } = string.Empty;

    public JokerCard()
    {
    }

    public JokerCard(string name, JokerEdition edition, JokerRarity rarity, JokerModifierType type, float value, int price)
    {
        Name = name;
        Edition = edition;
        Rarity = rarity;
        JokerModifierType = type;
        Price = price;
        switch (type)
        {
            case JokerModifierType.Chips:
                ChipsValue = (int)value;
                break;
            case JokerModifierType.AdditionMultiplier:
                MultValue = value;
                break;
            case JokerModifierType.MultiplierMultiplier:
                XMultValue = value;
                break;
            case JokerModifierType.Money:
                MoneyValue = (int)value;
                break;
        }
    }
}
