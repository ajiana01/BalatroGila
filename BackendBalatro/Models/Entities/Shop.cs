using BackendBalatro.Models.Interfaces;

namespace BackendBalatro.Models.Entities;

public class Shop
{
    public List<JokerCard> JokerCardOffers { get; set; } = new();
    public List<PlayingCard> PlayingCardOffers { get; set; } = new();
    public List<TarotCard> TarotCardOffers { get; set; } = new();
    public List<PlanetCard> PlanetCardOffers { get; set; } = new();
    public List<SpectralCard> SpectralCardOffers { get; set; } = new();
    public List<BoosterPack> BoosterPacks { get; set; } = new();
    public Voucher? Voucher { get; set; }
    public BoosterPack? OpenedBoosterPack { get; set; }

    public int MaxItemCardOffers { get; set; } = 2;
    public int MaxItemBoosterPacks { get; set; } = 2;
    public int BaseRerollCost { get; set; } = 5;
    public int RerollCount { get; set; } = 0;
    public int RerollCost => Math.Max(0, BaseRerollCost + RerollCount);

    public Shop()
    {
    }

    public void ResetForNewShop()
    {
        RerollCount = 0;
        OpenedBoosterPack = null;
        JokerCardOffers.Clear();
        PlayingCardOffers.Clear();
        TarotCardOffers.Clear();
        PlanetCardOffers.Clear();
        SpectralCardOffers.Clear();
        BoosterPacks.Clear();
    }

    public List<IPurchasableCard> GetAllCardOffers()
    {
        var offers = new List<IPurchasableCard>();
        offers.AddRange(JokerCardOffers);
        offers.AddRange(PlayingCardOffers);
        offers.AddRange(TarotCardOffers);
        offers.AddRange(PlanetCardOffers);
        offers.AddRange(SpectralCardOffers);
        return offers;
    }

    public bool RemoveOfferById(string cardId)
    {
        if (JokerCardOffers.RemoveAll(c => c.Id == cardId) > 0) return true;
        if (PlayingCardOffers.RemoveAll(c => c.Id == cardId) > 0) return true;
        if (TarotCardOffers.RemoveAll(c => c.Id == cardId) > 0) return true;
        if (PlanetCardOffers.RemoveAll(c => c.Id == cardId) > 0) return true;
        if (SpectralCardOffers.RemoveAll(c => c.Id == cardId) > 0) return true;
        return false;
    }
}
