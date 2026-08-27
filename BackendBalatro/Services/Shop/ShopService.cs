using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Shop;

public class ShopService : IShopService
{
    private static readonly Random _random = new();

    public void PopulateShop(
        BackendBalatro.Models.Entities.Shop shop,
        int ante,
        List<Voucher> purchasedVouchers,
        Voucher? currentAnteVoucher = null,
        bool isAnteVoucherPurchased = false)
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
            shop.BoosterPacks.Add(GenerateRandomBoosterPack(purchasedVouchers));
        }

        // Voucher retention per Ante
        if (!isAnteVoucherPurchased && currentAnteVoucher != null)
        {
            shop.Voucher = currentAnteVoucher;
        }
        else
        {
            shop.Voucher = null;
        }
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
        bool hasHone = vouchers.Any(v => v.Effect == VoucherEffect.Hone);
        bool hasClearance = vouchers.Any(v => v.Effect == VoucherEffect.ClearanceSale);
        bool hasTarotMerchant = vouchers.Any(v => v.Effect == VoucherEffect.TarotMerchant);
        bool hasPlanetMerchant = vouchers.Any(v => v.Effect == VoucherEffect.PlanetMerchant);
        bool hasMagicTrick = vouchers.Any(v => v.Effect == VoucherEffect.MagicTrick);

        // Calculate dynamic weighted chances based on merchant vouchers
        // Default weights: Joker: 60, Tarot: 20, Planet: 15, PlayingCard: 5 (or 15 with Magic Trick)
        int weightJoker = 60;
        int weightTarot = hasTarotMerchant ? 40 : 20;
        int weightPlanet = hasPlanetMerchant ? 30 : 15;
        int weightPlayingCard = hasMagicTrick ? 20 : 5;

        int totalWeight = weightJoker + weightTarot + weightPlanet + weightPlayingCard;
        int roll = _random.Next(totalWeight);

        if (roll < weightJoker)
        {
            // Joker
            var joker = GenerateRandomJoker(hasHone);
            if (hasClearance)
            {
                joker.Price = Math.Max(1, (int)Math.Floor(joker.Price * 0.75));
            }
            shop.JokerCardOffers.Add(joker);
        }
        else if (roll < weightJoker + weightTarot)
        {
            // Tarot
            var tarotType = (TarotType)_random.Next(Enum.GetValues<TarotType>().Length);
            int price = hasClearance ? Math.Max(1, (int)Math.Floor(3 * 0.75)) : 3;
            shop.TarotCardOffers.Add(new TarotCard(tarotType.ToString(), price, tarotType));
        }
        else if (roll < weightJoker + weightTarot + weightPlanet)
        {
            // Planet
            var handType = (PokerHandType)_random.Next(Enum.GetValues<PokerHandType>().Length);
            var planet = PlanetCard.CreateForHand(handType);
            if (hasClearance)
            {
                planet.Price = Math.Max(1, (int)Math.Floor(planet.Price * 0.75));
            }
            shop.PlanetCardOffers.Add(planet);
        }
        else
        {
            // Playing Card
            var suit = (Suit)_random.Next(4);
            var rank = (Rank)_random.Next(2, 15);
            var enhancement = _random.Next(100) < 50 ? (EnhancePokerCard)_random.Next(1, Enum.GetValues<EnhancePokerCard>().Length) : EnhancePokerCard.None;
            var edition = hasHone && _random.Next(100) < 20 ? (JokerEdition)_random.Next(1, 4) : JokerEdition.Base;
            int price = hasClearance ? Math.Max(1, (int)Math.Floor(2 * 0.75)) : 2;
            shop.PlayingCardOffers.Add(new PlayingCard(suit, rank, enhancement, price) { Edition = edition });
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
            JokerId = template.JokerId,
            Name = template.Name,
            Edition = edition,
            Rarity = template.Rarity,
            JokerModifierType = template.JokerModifierType,
            ChipsValue = template.ChipsValue,
            MultValue = template.MultValue,
            XMultValue = template.XMultValue,
            MoneyValue = template.MoneyValue,
            Price = template.Price,
            Description = template.Description
        };
    }

    private static List<JokerCard> GetJokerCatalog()
    {
        return new List<JokerCard>
        {
            new JokerCard(JokerId.Joker, "Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 4, 2, "+4 Mult"),
            new JokerCard(JokerId.GreedyJoker, "Greedy Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5, "Played cards with Diamond suit give +4 Mult when scored"),
            new JokerCard(JokerId.LustyJoker, "Lusty Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5, "Played cards with Heart suit give +4 Mult when scored"),
            new JokerCard(JokerId.WrathfulJoker, "Wrathful Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5, "Played cards with Spade suit give +4 Mult when scored"),
            new JokerCard(JokerId.GluttonousJoker, "Gluttonous Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5, "Played cards with Club suit give +4 Mult when scored"),
            new JokerCard(JokerId.JollyJoker, "Jolly Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 3, "+8 Mult if played hand contains a Pair"),
            new JokerCard(JokerId.ZanyJoker, "Zany Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "+12 Mult if played hand contains a Three of a Kind"),
            new JokerCard(JokerId.MadJoker, "Mad Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "+10 Mult if played hand contains a Two Pair"),
            new JokerCard(JokerId.CrazyJoker, "Crazy Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "+12 Mult if played hand contains a Straight"),
            new JokerCard(JokerId.DrollJoker, "Droll Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "+10 Mult if played hand contains a Flush"),
            new JokerCard(JokerId.SlyJoker, "Sly Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 0, 3, "+50 Chips if played hand contains a Pair"),
            new JokerCard(JokerId.WilyJoker, "Wily Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 0, 4, "+100 Chips if played hand contains a Three of a Kind"),
            new JokerCard(JokerId.CleverJoker, "Clever Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 0, 4, "+80 Chips if played hand contains a Two Pair"),
            new JokerCard(JokerId.DeviousJoker, "Devious Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 0, 4, "+100 Chips if played hand contains a Straight"),
            new JokerCard(JokerId.CraftyJoker, "Crafty Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 0, 4, "+80 Chips if played hand contains a Flush"),
            new JokerCard(JokerId.HalfJoker, "Half Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5, "+20 Mult if played hand contains 3 or fewer cards"),
            new JokerCard(JokerId.Banner, "Banner", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 30, 5, "+30 Chips for each remaining discard"),
            new JokerCard(JokerId.MysticSummit, "Mystic Summit", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 15, 5, "+15 Mult when 0 discards remaining"),
            new JokerCard(JokerId.Misprint, "Misprint", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "+0-23 Mult"),
            new JokerCard(JokerId.RaisedFist, "Raised Fist", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 5, "Adds double the rank of lowest card held in hand to Mult"),
            new JokerCard(JokerId.ChaosTheClown, "Chaos the Clown", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "1 free Reroll per shop"),
            new JokerCard(JokerId.Fibonacci, "Fibonacci", JokerEdition.Base, JokerRarity.Uncommon, JokerModifierType.AdditionMultiplier, 0, 8, "Each played Ace, 2, 3, 5, or 8 gives +8 Mult when scored"),
            new JokerCard(JokerId.ScaryFace, "Scary Face", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 0, 4, "Played face cards give +30 Chips when scored"),
            new JokerCard(JokerId.SmileyFace, "Smiley Face", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "Played face cards give +5 Mult when scored"),
            new JokerCard(JokerId.Photograph, "Photograph", JokerEdition.Base, JokerRarity.Common, JokerModifierType.MultiplierMultiplier, 1.0f, 5, "First played face card gives X2 Mult when scored"),
            new JokerCard(JokerId.AbstractJoker, "Abstract Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 6, 5, "+3 Mult for each Joker card (starts with +6 Mult)"),
            new JokerCard(JokerId.GrosMichel, "Gros Michel", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 15, 5, "+15 Mult (1 in 6 chance to destroy at end of round)"),
            new JokerCard(JokerId.Cavendish, "Cavendish", JokerEdition.Base, JokerRarity.Common, JokerModifierType.MultiplierMultiplier, 3.0f, 4, "X3 Mult (1 in 1000 chance to destroy at end of round)"),
            new JokerCard(JokerId.EvenSteven, "Even Steven", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "Played cards with even rank give +4 Mult when scored (10, 8, 6, 4, 2)"),
            new JokerCard(JokerId.OddTodd, "Odd Todd", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 0, 4, "Played cards with odd rank give +31 Chips when scored (A, 9, 7, 5, 3)"),
            new JokerCard(JokerId.Scholar, "Scholar", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "Played Aces give +20 Chips and +4 Mult when scored"),
            new JokerCard(JokerId.WalkieTalkie, "Walkie Talkie", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 0, 4, "Each played 10 or 4 gives +10 Chips and +4 Mult when scored"),
            new JokerCard(JokerId.Baron, "Baron", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 1.0f, 8, "Each King held in hand gives X1.5 Mult"),
            new JokerCard(JokerId.Blackboard, "Blackboard", JokerEdition.Base, JokerRarity.Uncommon, JokerModifierType.MultiplierMultiplier, 1.0f, 6, "X3 Mult if all cards held in hand are Spades or Clubs"),
            new JokerCard(JokerId.Bull, "Bull", JokerEdition.Base, JokerRarity.Uncommon, JokerModifierType.Chips, 0, 6, "+2 Chips for each $1 you have"),
            new JokerCard(JokerId.Popcorn, "Popcorn", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 20, 5, "+20 Mult (reduces by 4 each round)"),
            new JokerCard(JokerId.IceCream, "Ice Cream", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 100, 5, "+100 Chips (-5 Chips for every hand played)"),
            new JokerCard(JokerId.BlueJoker, "Blue Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 50, 5, "+2 Chips for each remaining card in deck"),
            new JokerCard(JokerId.Constellation, "Constellation", JokerEdition.Base, JokerRarity.Uncommon, JokerModifierType.MultiplierMultiplier, 1.5f, 6, "Gains X0.1 Mult every time a Planet card is used (currently X1.5 Mult)"),
            new JokerCard(JokerId.TheDuo, "The Duo", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 2.0f, 8, "X2 Mult if played hand contains a Pair"),
            new JokerCard(JokerId.TheTrio, "The Trio", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 3.0f, 8, "X3 Mult if played hand contains a Three of a Kind"),
            new JokerCard(JokerId.TheOrder, "The Order", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 3.0f, 8, "X3 Mult if played hand contains a Straight"),
            new JokerCard(JokerId.TheTribe, "The Tribe", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 2.0f, 8, "X2 Mult if played hand contains a Flush"),
            new JokerCard(JokerId.TheFamily, "The Family", JokerEdition.Base, JokerRarity.Rare, JokerModifierType.MultiplierMultiplier, 4.0f, 8, "X4 Mult if played hand contains a Four of a Kind"),
            new JokerCard(JokerId.GoldenJoker, "Golden Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Money, 4, 6, "Earn $4 at end of round")
        };
    }

    private static BoosterPack GenerateRandomBoosterPack(List<Voucher>? vouchers = null)
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

        if (vouchers?.Any(v => v.Effect == VoucherEffect.ClearanceSale) == true)
        {
            price = Math.Max(1, (int)Math.Floor(price * 0.75));
        }

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

    public BoosterPack OpenBoosterPack(BoosterPack pack, List<Voucher>? purchasedVouchers = null, PokerHandType mostPlayedHand = PokerHandType.HighCard)
    {
        pack.IsOpened = true;
        pack.PlayingCards.Clear();
        pack.TarotCards.Clear();
        pack.PlanetCards.Clear();
        pack.SpectralCards.Clear();
        pack.JokerCards.Clear();

        bool hasTelescope = purchasedVouchers?.Any(v => v.Effect == VoucherEffect.Telescope) == true;

        for (int i = 0; i < pack.TotalCard; i++)
        {
            switch (pack.BoosterPackType)
            {
                case BoosterType.Arcana:
                    var tarot = (TarotType)_random.Next(Enum.GetValues<TarotType>().Length);
                    pack.TarotCards.Add(new TarotCard(tarot.ToString(), 0, tarot));
                    break;
                case BoosterType.Celestial:
                    // If Telescope voucher is active, the first planet card is guaranteed for the most played hand
                    if (i == 0 && hasTelescope)
                    {
                        pack.PlanetCards.Add(PlanetCard.CreateForHand(mostPlayedHand));
                    }
                    else
                    {
                        var hand = (PokerHandType)_random.Next(Enum.GetValues<PokerHandType>().Length);
                        pack.PlanetCards.Add(PlanetCard.CreateForHand(hand));
                    }
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

    public Voucher? GenerateVoucherForAnte(int ante, List<Voucher> purchasedVouchers)
    {
        var allEffects = Enum.GetValues<VoucherEffect>()
            .Where(e => !purchasedVouchers.Any(p => p.Effect == e))
            .ToList();

        if (allEffects.Count == 0) return null;

        var effect = allEffects[_random.Next(allEffects.Count)];
        return new Voucher(effect.ToString(), effect, 10);
    }
}
