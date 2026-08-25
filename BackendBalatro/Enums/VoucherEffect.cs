namespace BackendBalatro.Enums;

public enum VoucherEffect
{
    Overstock, // +1 card slot in shop
    ClearanceSale, // 25% off shop prices
    Hone, // Foil/Holo/Poly cards appear 2x more often
    RerollSurplus, // Rerolls cost $2 less
    CrystalBall, // +1 consumable slot
    Grabber, // +1 Hand per round
    Wasteful, // +1 Discard per round
    TarotMerchant, // Tarot cards appear 2x more often in shop
    PlanetMerchant, // Planet cards appear 2x more often in shop
    Hieroglyph, // -1 Ante, -1 Hand per round
    DirectorsCut // Reroll Boss Blind
}
