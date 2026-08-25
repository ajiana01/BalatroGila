using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Shop;

public class ShopService : IShopService
{
    private static readonly Random _random = new();

    public void PopulateShop(BackendBalatro.Models.Entities.Shop shop, int ante, List<Voucher> purchasedVouchers)
    {
        shop.ResetForNewShop();

        // Check vouchers for slots
        bool hasOverstock = purchasedVouchers.Any(v => v.Effect == VoucherEffect.Overstock);
        shop.MaxItemCardOffers = hasOverstock ? 3 : 2;

        // Generate Card Offers
        for (int i = 0; i < shop.MaxItemCardOffers; i++)
        {
            GenerateRandomShopCard(shop, ante, purchasedVouchers);
        }

        // Generate 2 Booster Packs
        for (int i = 0; i < shop.MaxItemBoosterPacks; i++)
        {
            shop.BoosterPacks.Add(GenerateRandomBoosterPack());
        }

        // Generate 1 Voucher for the Ante if not already purchased
        shop.Voucher = GenerateVoucherForAnte(ante, purchasedVouchers);
    }

    public void RerollShop(BackendBalatro.Models.Entities.Shop shop, int ante, List<Voucher> purchasedVouchers)
    {
        shop.JokerCardOffers.Clear();
        shop.PlayingCardOffers.Clear();
        shop.TarotCardOffers.Clear();
        shop.PlanetCardOffers.Clear();
        shop.SpectralCardOffers.Clear();

        for (int i = 0; i < shop.MaxItemCardOffers; i++)
        {
            GenerateRandomShopCard(shop, ante, purchasedVouchers);
        }
    }

    private static void GenerateRandomShopCard(BackendBalatro.Models.Entities.Shop shop, int ante, List<Voucher> vouchers)
    {
        int roll = _random.Next(100);

        // Hone voucher effect
        bool hasHone = vouchers.Any(v => v.Effect == VoucherEffect.Hone);

        if (roll < 60)
        {
            // Joker (60% chance)
            var joker = GenerateRandomJoker(hasHone);
            shop.JokerCardOffers.Add(joker);
        }
        else if (roll < 80)
        {
            // Tarot (20% chance)
            var tarotType = (TarotType)_random.Next(Enum.GetValues<TarotType>().Length);
            shop.TarotCardOffers.Add(new TarotCard(tarotType.ToString(), 3, tarotType));
        }
        else if (roll < 95)
        {
            // Planet (15% chance)
            var handType = (PokerHandType)_random.Next(Enum.GetValues<PokerHandType>().Length);
            shop.PlanetCardOffers.Add(PlanetCard.CreateForHand(handType));
        }
        else
        {
            // Playing Card (5% chance)
            var suit = (Suit)_random.Next(4);
            var rank = (Rank)_random.Next(2, 15);
            var enhancement = _random.Next(100) < 50 ? (EnhancePokerCard)_random.Next(1, Enum.GetValues<EnhancePokerCard>().Length) : EnhancePokerCard.None;
            var edition = hasHone && _random.Next(100) < 20 ? (JokerEdition)_random.Next(1, 4) : JokerEdition.Base;
            shop.PlayingCardOffers.Add(new PlayingCard(suit, rank, enhancement, 2) { Edition = edition });
        }
    }

    public static JokerCard GenerateRandomJoker(bool hasHone = false)
    {
        var catalog = GetJokerCatalog();
        var template = catalog[_random.Next(catalog.Count)];

        var edition = JokerEdition.Base;
        int editionRoll = _random.Next(100);
        int threshold = hasHone ? 20 : 10;

        if (editionRoll < threshold)
        {
            int typeRoll = _random.Next(100);
            if (typeRoll < 50) edition = JokerEdition.Foil;
            else if (typeRoll < 85) edition = JokerEdition.Holographic;
            else edition = JokerEdition.Polychrome;
        }

        return new JokerCard
        {
            Id = Guid.NewGuid().ToString(),
            Name = template.Name,
            Edition = edition,
            Rarity = template.Rarity,
            JokerModifierType = template.JokerModifierType,
            ChipsValue = template.ChipsValue,
            MultValue = template.MultValue,
            XMultValue = template.XMultValue,
            MoneyValue = template.MoneyValue,
            Price = template.Price,
            Description = template.Description,
            JokerKey = template.JokerKey
        };
    }

    private static List<JokerCard> GetJokerCatalog()
    {
        return new List<JokerCard>
        {
            new JokerCard("Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 4, 4)
            {
                Description = "+4 Mult",
                JokerKey = "joker"
            },
            new JokerCard("Greedy Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5)
            {
                Description = "Played cards with Diamond suit give +4 Mult when scored",
                JokerKey = "greedyjoker"
            },
            new JokerCard("Lusty Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5)
            {
                Description = "Played cards with Heart suit give +4 Mult when scored",
                JokerKey = "lustyjoker"
            },
            new JokerCard("Wrathful Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5)
            {
                Description = "Played cards with Spade suit give +4 Mult when scored",
                JokerKey = "wrathfuljoker"
            },
            new JokerCard("Gluttonous Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5)
            {
                Description = "Played cards with Club suit give +4 Mult when scored",
                JokerKey = "gluttonousjoker"
            },
            new JokerCard("Half Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4)
            {
                Description = "+20 Mult if played hand contains 3 or fewer cards",
                JokerKey = "halfjoker"
            },
            new JokerCard("Scary Face", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 0, 4)
            {
                Description = "Played face cards give +30 Chips when scored",
                JokerKey = "scaryface"
            },
            new JokerCard("Raised Fist", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5)
            {
                Description = "Adds double the rank of lowest card held in hand to Mult",
                JokerKey = "raisedfist"
            },
            new JokerCard("Abstract Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 6, 5)
            {
                Description = "+3 Mult for each Joker card (starts with +6 Mult)",
                JokerKey = "abstractjoker"
            },
            new JokerCard("Banner", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 40, 5)
            {
                Description = "+40 Chips for each remaining discard",
                JokerKey = "banner"
            },
            new JokerCard("Mystic Summit", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 15, 5)
            {
                Description = "+15 Mult when 0 discards remaining",
                JokerKey = "mysticsummit"
            },
            new JokerCard("Fibonacci", JokerEdition.Base, JokerRarity.Uncommon, JokerModifierType.AdditionMultiplier, 0, 8)
            {
                Description = "Each played Ace, 2, 3, 5, or 8 gives +8 Mult when scored",
                JokerKey = "fibonacci"
            },
            new JokerCard("Cavendish", JokerEdition.Base, JokerRarity.Common, JokerModifierType.MultiplierMultiplier, 3.0f, 6)
            {
                Description = "X3 Mult (1 in 1000 chance to destroy at end of round)",
                JokerKey = "cavendish"
            },
            new JokerCard("Gros Michel", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 15, 5)
            {
                Description = "+15 Mult (1 in 6 chance to destroy at end of round)",
                JokerKey = "grosmichel"
            },
            new JokerCard("Constellation", JokerEdition.Base, JokerRarity.Uncommon, JokerModifierType.MultiplierMultiplier, 1.5f, 6)
            {
                Description = "Gains X0.1 Mult every time a Planet card is used (currently X1.5 Mult)",
                JokerKey = "constellation"
            },
            new JokerCard("The Tribe", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 2.0f, 8)
            {
                Description = "X2 Mult if played hand contains a Flush",
                JokerKey = "thetribe"
            },
            new JokerCard("The Duo", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 2.0f, 8)
            {
                Description = "X2 Mult if played hand contains a Pair",
                JokerKey = "theduo"
            },
            new JokerCard("The Trio", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 3.0f, 8)
            {
                Description = "X3 Mult if played hand contains a Three of a Kind",
                JokerKey = "thetrio"
            },
            new JokerCard("The Order", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 3.0f, 8)
            {
                Description = "X3 Mult if played hand contains a Straight",
                JokerKey = "theorder"
            }
        };
    }

    private static BoosterPack GenerateRandomBoosterPack()
    {
        var types = new[] { BoosterType.Arcana, BoosterType.Celestial, BoosterType.Standard, BoosterType.Buffoon, BoosterType.Spectral };
        var type = types[_random.Next(types.Length)];
        var size = (PackSize)_random.Next(3); // Normal, Jumbo, Mega

        int price = type switch
        {
            BoosterType.Buffoon => 6,
            BoosterType.Spectral => 6,
            _ => 4
        };

        if (size == PackSize.Jumbo) price += 2;
        if (size == PackSize.Mega) price += 4;

        int totalCards = size switch
        {
            PackSize.Normal => 3,
            PackSize.Jumbo => 5,
            PackSize.Mega => 5,
            _ => 3
        };

        int maxPick = size == PackSize.Mega ? 2 : 1;

        string name = $"{size} {type} Pack";
        return new BoosterPack(name, price, maxPick, totalCards, type, size);
    }

    public BoosterPack OpenBoosterPack(BoosterPack pack)
    {
        pack.IsOpened = true;
        pack.PlayingCards.Clear();
        pack.TarotCards.Clear();
        pack.PlanetCards.Clear();
        pack.SpectralCards.Clear();
        pack.JokerCards.Clear();

        for (int i = 0; i < pack.TotalCard; i++)
        {
            switch (pack.BoosterPackType)
            {
                case BoosterType.Arcana:
                    var tarot = (TarotType)_random.Next(Enum.GetValues<TarotType>().Length);
                    pack.TarotCards.Add(new TarotCard(tarot.ToString(), 0, tarot));
                    break;
                case BoosterType.Celestial:
                    var hand = (PokerHandType)_random.Next(Enum.GetValues<PokerHandType>().Length);
                    pack.PlanetCards.Add(PlanetCard.CreateForHand(hand));
                    break;
                case BoosterType.Standard:
                    var suit = (Suit)_random.Next(4);
                    var rank = (Rank)_random.Next(2, 15);
                    var enh = (EnhancePokerCard)_random.Next(Enum.GetValues<EnhancePokerCard>().Length);
                    pack.PlayingCards.Add(new PlayingCard(suit, rank, enh, 0));
                    break;
                case BoosterType.Buffoon:
                    pack.JokerCards.Add(GenerateRandomJoker(false));
                    break;
                case BoosterType.Spectral:
                    var spec = (SpectralType)_random.Next(Enum.GetValues<SpectralType>().Length);
                    pack.SpectralCards.Add(new SpectralCard(spec.ToString(), 0, spec));
                    break;
            }
        }

        return pack;
    }

    private static Voucher? GenerateVoucherForAnte(int ante, List<Voucher> purchasedVouchers)
    {
        var allEffects = Enum.GetValues<VoucherEffect>()
            .Where(e => !purchasedVouchers.Any(p => p.Effect == e))
            .ToList();

        if (allEffects.Count == 0) return null;

        var effect = allEffects[_random.Next(allEffects.Count)];
        return new Voucher(effect.ToString(), effect, 10);
    }
}
