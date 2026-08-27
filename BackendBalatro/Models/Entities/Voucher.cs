using BackendBalatro.Enums;

namespace BackendBalatro.Models.Entities;

public class Voucher
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public VoucherEffect Effect { get; set; }
    public int Price { get; set; } = 10;
    public string Description { get; set; } = string.Empty;
    public bool IsPurchased { get; set; } = false;

    public Voucher()
    {
    }

    public Voucher(string name, VoucherEffect effect, int price = 10, string description = "")
    {
        Name = name;
        Effect = effect;
        Price = price;
        Description = string.IsNullOrEmpty(description) ? GetDefaultDescription(effect) : description;
    }

    public static string GetDefaultDescription(VoucherEffect effect)
    {
        return effect switch
        {
            VoucherEffect.Overstock => "+1 Card slot available in shop.",
            VoucherEffect.ClearanceSale => "All cards and packs in shop are 25% off.",
            VoucherEffect.Hone => "Foil, Holographic, and Polychrome cards appear 2x more often.",
            VoucherEffect.RerollSurplus => "Rerolls cost $2 less.",
            VoucherEffect.CrystalBall => "+1 Consumable slot.",
            VoucherEffect.Telescope => "Celestial Packs always contain the Planet card for your most played poker hand.",
            VoucherEffect.Grabber => "Permanently gain +1 Hand per round.",
            VoucherEffect.Wasteful => "Permanently gain +1 Discard per round.",
            VoucherEffect.TarotMerchant => "Tarot cards appear 2x more frequently in the shop.",
            VoucherEffect.PlanetMerchant => "Planet cards appear 2x more frequently in the shop.",
            VoucherEffect.SeedMoney => "Raise the cap on interest earned per round to $10.",
            VoucherEffect.Blank => "Does nothing?",
            VoucherEffect.MagicTrick => "Standard playing cards can be purchased from the shop.",
            VoucherEffect.Hieroglyph => "-1 Ante, -1 Hand each round.",
            VoucherEffect.DirectorsCut => "Reroll Boss Blind 1 time per Ante for $10.",
            VoucherEffect.PaintBrush => "+1 Hand size.",
            _ => "Voucher effect"
        };
    }
}
