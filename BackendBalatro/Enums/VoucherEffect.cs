namespace BackendBalatro.Enums;

public enum VoucherEffect
{
    Overstock,      // +1 card slot in shop
    ClearanceSale,  // 25% off shop prices
    Hone,           // Foil/Holo/Poly cards appear 2x more often
    RerollSurplus,  // Rerolls cost $2 less
    CrystalBall,    // +1 consumable slot
    Telescope,      // Celestial Packs always contain Planet for most played poker hand
    Grabber,        // +1 Hand per round
    Wasteful,       // +1 Discard per round
    TarotMerchant,  // Tarot cards appear 2x more often in shop
    PlanetMerchant, // Planet cards appear 2x more often in shop
    SeedMoney,      // Raises interest cap to $10
    Blank,          // Does nothing?
    MagicTrick,     // Playing cards can be bought directly from shop
    Hieroglyph,     // -1 Ante, -1 Hand per round
    DirectorsCut,   // Reroll Boss Blind 1 time per Ante for $10
    PaintBrush      // +1 Hand size
}
