using BackendBalatro.Enums;
using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class JokerCard : IPurchasableCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public JokerId JokerId { get; set; } = JokerId.Joker;
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
    
    public string JokerKey
    {
        get => JokerId.ToString();
        set
        {
            if (Enum.TryParse<JokerId>(value, true, out var parsed))
            {
                JokerId = parsed;
            }
        }
    }

    public JokerCard()
    {
    }

    public JokerCard(JokerId jokerId, string name, JokerEdition edition, JokerRarity rarity, JokerModifierType type, float value, int price, string description = "")
    {
        JokerId = jokerId;
        Name = name;
        Edition = edition;
        Rarity = rarity;
        JokerModifierType = type;
        Price = price;
        Description = description;
        ApplyModifier(type, value);
    }

    public JokerCard(string name, JokerEdition edition, JokerRarity rarity, JokerModifierType type, float value, int price)
    {
        Name = name;
        Edition = edition;
        Rarity = rarity;
        JokerModifierType = type;
        Price = price;
        if (Enum.TryParse<JokerId>(name.Replace(" ", "").Replace("-", ""), true, out var parsed))
        {
            JokerId = parsed;
        }
        ApplyModifier(type, value);
    }

    private void ApplyModifier(JokerModifierType type, float value)
    {
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
