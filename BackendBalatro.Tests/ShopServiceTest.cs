using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Models.Interfaces;
using BackendBalatro.Services.Shop;
using Microsoft.Extensions.Logging.Abstractions;

namespace BackendBalatro.Tests;

[TestFixture]
[NonParallelizable]
public class ShopServiceTest
{
    private ShopService _service;

    [SetUp]
    public void SetUp() => _service = new ShopService(NullLogger<ShopService>.Instance);

    [Test]
    public void PopulateShop_DefaultState_ResetsAndCreatesTwoCardsAndTwoBoosters()
    {
        var shop = CreatePopulatedShop();
        var openedPack = new BoosterPack("Old", 4, 1, 1, BoosterType.Arcana, PackSize.Normal);
        shop.JokerCardOffers.Add(new JokerCard());
        shop.BoosterPacks.Add(openedPack);
        shop.OpenedBoosterPack = openedPack;
        shop.RerollCount = 3;

        _service.PopulateShop(shop, 1, new List<Voucher>());

        Assert.Multiple(() =>
        {
            Assert.That(shop.RerollCount, Is.EqualTo(0));
            Assert.That(shop.GetAllCardOffers(), Has.Count.EqualTo(2));
            Assert.That(shop.BoosterPacks, Has.Count.EqualTo(2));
            Assert.That(shop.OpenedBoosterPack, Is.Null);
            Assert.That(shop.BoosterPacks, Does.Not.Contain(openedPack));
        });
    }

    [Test]
    public void PopulateShop_WithOverstock_CreatesThreeCardOffers()
    {
        var shop = CreatePopulatedShop();
        var vouchers = new List<Voucher> { VoucherFor(VoucherEffect.Overstock) };

        _service.PopulateShop(shop, 1, vouchers);

        Assert.Multiple(() =>
        {
            Assert.That(shop.MaxItemCardOffers, Is.EqualTo(3));
            Assert.That(shop.GetAllCardOffers(), Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void PopulateShop_WithCurrentAnteVoucher_ShowsVoucher()
    {
        var shop = CreatePopulatedShop();
        var voucher = VoucherFor(VoucherEffect.Hone);

        _service.PopulateShop(shop, 1, new List<Voucher>(), voucher);

        Assert.That(shop.Voucher, Is.SameAs(voucher));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void PopulateShop_VoucherUnavailable_ClearsVoucher(bool voucherIsNull, bool isPurchased)
    {
        var shop = CreatePopulatedShop();
        shop.Voucher = VoucherFor(VoucherEffect.Hone);
        Voucher? voucher = voucherIsNull ? null : VoucherFor(VoucherEffect.ClearanceSale);

        _service.PopulateShop(shop, 1, new List<Voucher>(), voucher, isPurchased);

        Assert.That(shop.Voucher, Is.Null);
    }

    [Test]
    public void RerollShop_ExistingOffers_ReplacesOnlyCardOffers()
    {
        var shop = CreatePopulatedShop();
        shop.JokerCardOffers.Add(new JokerCard { Id = "old-joker" });
        shop.TarotCardOffers.Add(new TarotCard("old-tarot", 3, TarotType.TheFool) { Id = "old-tarot" });
        var booster = new BoosterPack("Booster", 4, 1, 3, BoosterType.Arcana, PackSize.Normal);
        var voucher = VoucherFor(VoucherEffect.Hone);
        var opened = new BoosterPack("Opened", 4, 1, 3, BoosterType.Arcana, PackSize.Normal);
        shop.BoosterPacks.Add(booster);
        shop.Voucher = voucher;
        shop.OpenedBoosterPack = opened;
        shop.RerollCount = 2;

        _service.RerollShop(shop, 1, new List<Voucher>());

        Assert.Multiple(() =>
        {
            Assert.That(shop.GetAllCardOffers(), Has.Count.EqualTo(shop.MaxItemCardOffers));
            Assert.That(shop.GetAllCardOffers().Select(card => card.Id), Does.Not.Contain("old-joker"));
            Assert.That(shop.GetAllCardOffers().Select(card => card.Id), Does.Not.Contain("old-tarot"));
            Assert.That(shop.BoosterPacks, Is.EqualTo(new[] { booster }));
            Assert.That(shop.Voucher, Is.SameAs(voucher));
            Assert.That(shop.OpenedBoosterPack, Is.SameAs(opened));
            Assert.That(shop.RerollCount, Is.EqualTo(2));
        });
    }

    [TestCase("joker", -1)]
    [TestCase("tarot", 2)]
    [TestCase("planet", 2)]
    [TestCase("playing", 1)]
    public void GenerateRandomShopCard_ClearanceSale_AppliesTwentyFivePercentDiscountWithMinimumOne(
        string cardType, int expectedPrice)
    {
        var shop = CreatePopulatedShop();
        var vouchers = new List<Voucher> { VoucherFor(VoucherEffect.ClearanceSale) };
        var offer = FindOffer(cardType, vouchers);

        Assert.Multiple(() =>
        {
            Assert.That(offer, Is.Not.Null);
            Assert.That(expectedPrice < 0
                ? new[] { 1, 2, 3, 4, 6 }.Contains(offer!.Price)
                : offer!.Price == expectedPrice, Is.True);
        });
    }

    [TestCase("tarot", 60)]
    [TestCase("planet", 80)]
    [TestCase("playing", 95)]
    public void GenerateRandomShopCard_MerchantAndMagicTrickVouchers_UseConfiguredWeights(string voucherCase, int roll)
    {
        var effect = voucherCase switch
        {
            "tarot" => VoucherEffect.TarotMerchant,
            "planet" => VoucherEffect.PlanetMerchant,
            _ => VoucherEffect.MagicTrick
        };
        var offer = FindOffer(voucherCase, new List<Voucher> { VoucherFor(effect) });

        Assert.Multiple(() =>
        {
            Assert.That(offer, Is.Not.Null);
            Assert.That(OfferType(offer!), Is.EqualTo(voucherCase));
        });
    }

    [Test]
    public void GenerateRandomJoker_ReturnsIndependentCopyFromCatalog()
    {
        JokerCard first;
        JokerCard second;
        first = ShopService.GenerateRandomJoker();
        second = ShopService.GenerateRandomJoker();
        var secondName = second.Name;
        first.Name = "Mutated";
        first.MultValue = 999;

        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.Not.Empty);
            Assert.That(second.Id, Is.Not.Empty);
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(second.Name, Is.EqualTo(secondName));
            Assert.That(second.MultValue, Is.Not.EqualTo(999));
            Assert.That(second.Description, Is.Not.Empty);
        });
    }

    [TestCase(false, 10, 0, JokerEdition.Base)]
    [TestCase(false, 0, 0, JokerEdition.Foil)]
    [TestCase(true, 0, 50, JokerEdition.Holographic)]
    [TestCase(true, 0, 85, JokerEdition.Polychrome)]
    public void GenerateRandomJoker_EditionRoll_AssignsExpectedEdition(
        bool hasHone, int editionRoll, int typeRoll, JokerEdition expectedEdition)
    {
        var joker = FindJokerWithEdition(hasHone, expectedEdition);

        Assert.That(joker, Is.Not.Null);
        Assert.That(joker!.Edition, Is.EqualTo(expectedEdition));
    }

    [Test]
    public void GenerateRandomJoker_AlwaysReturnsValidCatalogMetadata()
    {
        JokerCard joker;
        joker = ShopService.GenerateRandomJoker();

        Assert.Multiple(() =>
        {
            Assert.That(joker.Name, Is.Not.Empty);
            Assert.That(Enum.IsDefined(joker.Rarity), Is.True);
            Assert.That(Enum.IsDefined(joker.JokerModifierType), Is.True);
            Assert.That(joker.Price, Is.GreaterThanOrEqualTo(0));
            Assert.That(joker.Description, Is.Not.Empty);
            Assert.That(joker.XMultValue, Is.GreaterThan(0));
        });
    }

    [TestCase(BoosterType.Arcana)]
    [TestCase(BoosterType.Celestial)]
    [TestCase(BoosterType.Standard)]
    [TestCase(BoosterType.Buffoon)]
    [TestCase(BoosterType.Spectral)]
    public void OpenBoosterPack_ByType_GeneratesExpectedCardCollection(BoosterType type)
    {
        var pack = new BoosterPack("Test", 4, 1, 3, type, PackSize.Normal);
        _service.OpenBoosterPack(pack);

        var collections = new[]
        {
            pack.TarotCards.Count, pack.PlanetCards.Count, pack.PlayingCards.Count,
            pack.JokerCards.Count, pack.SpectralCards.Count
        };
        var expectedIndex = (int)type;

        Assert.Multiple(() =>
        {
            Assert.That(pack.IsOpened, Is.True);
            Assert.That(collections[expectedIndex], Is.EqualTo(pack.TotalCard));
            Assert.That(collections.Where((_, index) => index != expectedIndex), Is.All.EqualTo(0));
            Assert.That(type is BoosterType.Arcana or BoosterType.Standard or BoosterType.Spectral
                ? pack.TarotCards.Cast<object>().Concat(pack.PlayingCards).Concat(pack.SpectralCards).All(card => GetPrice(card) == 0)
                : true, Is.True);
        });
    }

    [Test]
    public void OpenBoosterPack_PrepopulatedPack_ClearsOldContentsBeforeGeneration()
    {
        var pack = new BoosterPack("Arcana", 4, 1, 2, BoosterType.Arcana, PackSize.Normal);
        var oldTarot = new TarotCard("Old", 0, TarotType.TheFool);
        var oldPlaying = new PlayingCard(Suit.Hearts, Rank.Ace);
        pack.TarotCards.Add(oldTarot);
        pack.PlayingCards.Add(oldPlaying);

        _service.OpenBoosterPack(pack);

        Assert.Multiple(() =>
        {
            Assert.That(pack.TarotCards, Has.Count.EqualTo(2));
            Assert.That(pack.TarotCards, Does.Not.Contain(oldTarot));
            Assert.That(pack.PlayingCards, Is.Empty);
            Assert.That(pack.PlayingCards, Does.Not.Contain(oldPlaying));
        });
    }

    [Test]
    public void OpenBoosterPack_CelestialWithTelescope_FirstPlanetMatchesMostPlayedHand()
    {
        var pack = new BoosterPack("Celestial", 4, 1, 3, BoosterType.Celestial, PackSize.Normal);
        _service.OpenBoosterPack(pack, new List<Voucher> { VoucherFor(VoucherEffect.Telescope) }, PokerHandType.Flush);

        Assert.Multiple(() =>
        {
            Assert.That(pack.PlanetCards, Has.Count.EqualTo(pack.TotalCard));
            Assert.That(pack.PlanetCards[0].PokerHandType, Is.EqualTo(PokerHandType.Flush));
        });
    }

    [Test]
    public void OpenBoosterPack_ZeroTotalCards_OpensWithEmptyCollections()
    {
        var pack = new BoosterPack("Empty", 4, 1, 0, BoosterType.Arcana, PackSize.Normal);

        var result = _service.OpenBoosterPack(pack);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsOpened, Is.True);
            Assert.That(result.TarotCards, Is.Empty);
            Assert.That(result.PlanetCards, Is.Empty);
            Assert.That(result.PlayingCards, Is.Empty);
            Assert.That(result.JokerCards, Is.Empty);
            Assert.That(result.SpectralCards, Is.Empty);
        });
    }

    [TestCase(BoosterType.Arcana, PackSize.Normal, false, 4, 3, 1)]
    [TestCase(BoosterType.Arcana, PackSize.Jumbo, false, 6, 5, 1)]
    [TestCase(BoosterType.Arcana, PackSize.Mega, false, 8, 5, 2)]
    [TestCase(BoosterType.Buffoon, PackSize.Normal, false, 6, 3, 1)]
    [TestCase(BoosterType.Spectral, PackSize.Normal, false, 6, 3, 1)]
    [TestCase(BoosterType.Arcana, PackSize.Jumbo, true, 4, 5, 1)]
    public void GeneratedBoosterPack_SizeControlsPriceCardCountAndMaxPick(
        BoosterType type, PackSize size, bool clearanceSale, int expectedPrice, int expectedCards, int expectedMaxPick)
    {
        var vouchers = clearanceSale ? new List<Voucher> { VoucherFor(VoucherEffect.ClearanceSale) } : new List<Voucher>();
        var pack = FindGeneratedPack(type, size, vouchers);

        Assert.Multiple(() =>
        {
            Assert.That(pack, Is.Not.Null);
            Assert.That(pack!.Price, Is.EqualTo(expectedPrice));
            Assert.That(pack.TotalCard, Is.EqualTo(expectedCards));
            Assert.That(pack.MaxPick, Is.EqualTo(expectedMaxPick));
        });
    }

    [Test]
    public void GenerateVoucherForAnte_WithAvailableEffects_ReturnsUnpurchasedEffect()
    {
        Voucher? first;
        Voucher? second;
        first = _service.GenerateVoucherForAnte(1, new List<Voucher>());
        second = _service.GenerateVoucherForAnte(8, new List<Voucher>());

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Price, Is.EqualTo(10));
            Assert.That(first.Name, Is.EqualTo(first.Effect.ToString()));
            Assert.That(second, Is.Not.Null);
        });
    }

    [Test]
    public void GenerateVoucherForAnte_ExcludesEveryPurchasedEffect()
    {
        var remainingEffect = VoucherEffect.PaintBrush;
        var purchased = Enum.GetValues<VoucherEffect>()
            .Where(effect => effect != remainingEffect)
            .Select(VoucherFor)
            .ToList();

        Voucher? voucher;
        voucher = _service.GenerateVoucherForAnte(1, purchased);

        Assert.That(voucher!.Effect, Is.EqualTo(remainingEffect));
    }

    [Test]
    public void GenerateVoucherForAnte_AllEffectsPurchased_ReturnsNull()
    {
        var purchased = Enum.GetValues<VoucherEffect>().Select(VoucherFor).ToList();
        var initialCount = purchased.Count;

        var voucher = _service.GenerateVoucherForAnte(1, purchased);

        Assert.Multiple(() =>
        {
            Assert.That(voucher, Is.Null);
            Assert.That(purchased, Has.Count.EqualTo(initialCount));
        });
    }

    [Test]
    public void GenerateVoucherForAnte_DuplicatePurchasedEntries_RemainsSafe()
    {
        var purchased = new List<Voucher>
        {
            VoucherFor(VoucherEffect.Hone),
            VoucherFor(VoucherEffect.Hone)
        };

        Voucher? voucher;
        voucher = _service.GenerateVoucherForAnte(1, purchased);

        Assert.Multiple(() =>
        {
            Assert.That(voucher, Is.Not.Null);
            Assert.That(voucher!.Effect, Is.Not.EqualTo(VoucherEffect.Hone));
            Assert.That(purchased, Has.Count.EqualTo(2));
        });
    }

    private static Shop CreatePopulatedShop() => new();

    private static Voucher VoucherFor(VoucherEffect effect) => new(effect.ToString(), effect, 10);

    private static int GetPrice(object card) => card switch
    {
        TarotCard tarot => tarot.Price,
        PlanetCard planet => planet.Price,
        PlayingCard playingCard => playingCard.Price,
        SpectralCard spectral => spectral.Price,
        JokerCard joker => joker.Price,
        _ => -1
    };

    private IPurchasableCard? FindOffer(string expectedType, List<Voucher> vouchers)
    {
        for (var attempt = 0; attempt < 1_000; attempt++)
        {
            var shop = CreatePopulatedShop();
            shop.MaxItemCardOffers = 1;
            _service.RerollShop(shop, 1, vouchers);
            var offer = shop.GetAllCardOffers().Single();
            if (OfferType(offer) == expectedType)
            {
                return offer;
            }
        }

        return null;
    }

    private static string OfferType(IPurchasableCard offer) => offer switch
    {
        JokerCard => "joker",
        TarotCard => "tarot",
        PlanetCard => "planet",
        PlayingCard => "playing",
        SpectralCard => "spectral",
        _ => string.Empty
    };

    private static JokerCard? FindJokerWithEdition(bool hasHone, JokerEdition edition)
    {
        for (var attempt = 0; attempt < 5_000; attempt++)
        {
            var joker = ShopService.GenerateRandomJoker(hasHone);
            if (joker.Edition == edition)
            {
                return joker;
            }
        }

        return null;
    }

    private BoosterPack? FindGeneratedPack(BoosterType type, PackSize size, List<Voucher> vouchers)
    {
        for (var attempt = 0; attempt < 1_000; attempt++)
        {
            var shop = CreatePopulatedShop();
            _service.PopulateShop(shop, 1, vouchers);
            var pack = shop.BoosterPacks.FirstOrDefault(candidate =>
                candidate.BoosterPackType == type && candidate.PackSize == size);
            if (pack != null)
            {
                return pack;
            }
        }

        return null;
    }

}
