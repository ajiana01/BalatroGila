using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Shop;

namespace BackendBalatro.Services.Consumables;

public class ConsumableEffectHandler : IConsumableEffectHandler
{
    private static readonly Random _random = new();
    private readonly ILogger<ConsumableEffectHandler> _logger;

    public ConsumableEffectHandler(ILogger<ConsumableEffectHandler> logger)
    {
        _logger = logger;
    }

    public bool UseTarot(GameController controller, TarotCard tarot, List<string> targetCardIds, out string message)
    {
        _logger.LogDebug(
            "Applying tarot {TarotType} with {TargetCardCount} target cards",
            tarot.Type,
            targetCardIds.Count);

        message = string.Empty;

        var targetCards = controller.Hand.Where(c => targetCardIds.Contains(c.Id)).ToList();

        switch (tarot.Type)
        {
            case TarotType.TheFool:
                if (controller.LastTarotUsed != null && controller.LastTarotUsed.Type != TarotType.TheFool)
                {
                    if (controller.Deck.IsConsumableContainerFull())
                    {
                        message = "Consumable inventory is full!";
                        return false;
                    }
                    var clone = new TarotCard(controller.LastTarotUsed.Name, controller.LastTarotUsed.Price, controller.LastTarotUsed.Type);
                    controller.Deck.UsableCards.Add(clone);
                    message = $"The Fool created {clone.Name}!";
                    return true;
                }
                if (controller.LastPlanetUsed != null)
                {
                    if (controller.Deck.IsConsumableContainerFull())
                    {
                        message = "Consumable inventory is full!";
                        return false;
                    }
                    var clone = new PlanetCard(controller.LastPlanetUsed.Name, controller.LastPlanetUsed.PokerHandType);
                    controller.Deck.UsableCards.Add(clone);
                    message = $"The Fool created {clone.Name}!";
                    return true;
                }
                message = "No previous Tarot or Planet card was used!";
                return false;

            case TarotType.TheMagician:
                if (targetCards.Count == 0 || targetCards.Count > 2)
                {
                    message = "Select 1 or 2 cards to enhance with Lucky Card.";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    card.Enhancement = EnhancePokerCard.LuckyCards;
                }
                message = $"Enhanced {targetCards.Count} card(s) to Lucky Cards.";
                return true;

            case TarotType.TheHighPriestess:
                if (controller.Deck.IsConsumableContainerFull())
                {
                    message = "Consumable slots are full!";
                    return false;
                }
                int countP = Math.Min(2, controller.Deck.MaxConsumableContainer - controller.Deck.UsableCards.Count + 1); // +1 because current tarot is being consumed
                for (int i = 0; i < countP; i++)
                {
                    var hand = (PokerHandType)_random.Next(Enum.GetValues<PokerHandType>().Length);
                    controller.Deck.UsableCards.Add(PlanetCard.CreateForHand(hand));
                }
                message = "Created Planet cards!";
                return true;

            case TarotType.TheEmpress:
                if (targetCards.Count == 0 || targetCards.Count > 2)
                {
                    message = "Select 1 or 2 cards to enhance with Mult (+4 Mult).";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    card.Enhancement = EnhancePokerCard.MultCards;
                }
                message = $"Enhanced {targetCards.Count} card(s) to Mult Cards.";
                return true;

            case TarotType.TheEmperor:
                if (controller.Deck.IsConsumableContainerFull())
                {
                    message = "Consumable slots are full!";
                    return false;
                }
                int countT = Math.Min(2, controller.Deck.MaxConsumableContainer - controller.Deck.UsableCards.Count + 1);
                for (int i = 0; i < countT; i++)
                {
                    var t = (TarotType)_random.Next(Enum.GetValues<TarotType>().Length);
                    controller.Deck.UsableCards.Add(new TarotCard(t.ToString(), 3, t));
                }
                message = "Created Tarot cards!";
                return true;

            case TarotType.TheHierophant:
                if (targetCards.Count == 0 || targetCards.Count > 2)
                {
                    message = "Select 1 or 2 cards to enhance with Bonus (+30 Chips).";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    card.Enhancement = EnhancePokerCard.BonusCards;
                }
                message = $"Enhanced {targetCards.Count} card(s) to Bonus Cards.";
                return true;

            case TarotType.TheLovers:
                if (targetCards.Count != 1)
                {
                    message = "Select exactly 1 card to turn into a Wild Card.";
                    return false;
                }
                targetCards[0].Enhancement = EnhancePokerCard.WildCards;
                message = $"Turned {targetCards[0].Name} into a Wild Card.";
                return true;

            case TarotType.TheChariot:
                if (targetCards.Count != 1)
                {
                    message = "Select exactly 1 card to turn into a Steel Card.";
                    return false;
                }
                targetCards[0].Enhancement = EnhancePokerCard.SteelCards;
                message = $"Turned {targetCards[0].Name} into a Steel Card.";
                return true;

            case TarotType.Justice:
                if (targetCards.Count != 1)
                {
                    message = "Select exactly 1 card to turn into a Glass Card.";
                    return false;
                }
                targetCards[0].Enhancement = EnhancePokerCard.GlassCards;
                message = $"Turned {targetCards[0].Name} into a Glass Card.";
                return true;

            case TarotType.TheHermit:
                int gained = Math.Min(20, controller.Money);
                controller.Money += gained;
                message = $"The Hermit doubled your money! (Gained ${gained})";
                return true;

            case TarotType.TheWheelFortune:
                if (controller.Deck.JokerCards.Count == 0)
                {
                    message = "No Jokers available to upgrade!";
                    return false;
                }
                bool wheelHit = _random.Next(4) == 0;
                if (wheelHit)
                {
                    var eligibleJokers = controller.Deck.JokerCards.Where(j => j.Edition == JokerEdition.Base).ToList();
                    if (eligibleJokers.Count > 0)
                    {
                        var targetJoker = eligibleJokers[_random.Next(eligibleJokers.Count)];
                        int edRoll = _random.Next(3);
                        targetJoker.Edition = edRoll switch
                        {
                            0 => JokerEdition.Foil,
                            1 => JokerEdition.Holographic,
                            _ => JokerEdition.Polychrome
                        };
                        message = $"Wheel of Fortune added {targetJoker.Edition} to {targetJoker.Name}!";
                        return true;
                    }
                }
                message = "Nope! Wheel of Fortune gave nothing.";
                return true;

            case TarotType.Strength:
                if (targetCards.Count == 0 || targetCards.Count > 2)
                {
                    message = "Select 1 or 2 cards to increase rank.";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    if (card.Rank < Rank.Ace)
                    {
                        card.Rank = (Rank)((int)card.Rank + 1);
                        card.BaseChips = PlayingCard.CalculateDefaultBaseChips(card.Rank);
                    }
                    else
                    {
                        card.Rank = Rank.Two;
                        card.BaseChips = PlayingCard.CalculateDefaultBaseChips(card.Rank);
                    }
                }
                message = $"Increased rank of {targetCards.Count} card(s).";
                return true;

            case TarotType.TheHangedMan:
                if (targetCards.Count == 0 || targetCards.Count > 2)
                {
                    message = "Select 1 or 2 cards to destroy.";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    controller.Hand.Remove(card);
                }
                message = $"Destroyed {targetCards.Count} card(s).";
                return true;

            case TarotType.Death:
                if (targetCards.Count != 2)
                {
                    message = "Select exactly 2 cards (first card converts into the second card).";
                    return false;
                }
                var left = targetCards[0];
                var right = targetCards[1];
                left.Rank = right.Rank;
                left.Suit = right.Suit;
                left.Enhancement = right.Enhancement;
                left.Edition = right.Edition;
                left.BaseChips = right.BaseChips;
                left.BaseMult = right.BaseMult;
                left.BaseXMult = right.BaseXMult;
                message = $"Converted {left.Name} to match {right.Name}.";
                return true;

            case TarotType.TheTemperance:
                int sellSum = controller.Deck.JokerCards.Sum(j => j.SellValue);
                int payout = Math.Min(50, sellSum);
                controller.Money += payout;
                message = $"Temperance gave ${payout} from Joker sell values!";
                return true;

            case TarotType.TheDevil:
                if (targetCards.Count != 1)
                {
                    message = "Select exactly 1 card to turn into a Gold Card.";
                    return false;
                }
                targetCards[0].Enhancement = EnhancePokerCard.GoldCards;
                message = $"Turned {targetCards[0].Name} into a Gold Card.";
                return true;

            case TarotType.TheTower:
                if (targetCards.Count != 1)
                {
                    message = "Select exactly 1 card to turn into a Stone Card.";
                    return false;
                }
                targetCards[0].Enhancement = EnhancePokerCard.StoneCards;
                message = $"Turned {targetCards[0].Name} into a Stone Card (+50 Chips).";
                return true;

            case TarotType.TheStar:
                if (targetCards.Count == 0 || targetCards.Count > 3)
                {
                    message = "Select 1 to 3 cards to convert to Diamonds.";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    card.Suit = Suit.Diamonds;
                }
                message = $"Converted {targetCards.Count} card(s) to Diamonds.";
                return true;

            case TarotType.TheMoon:
                if (targetCards.Count == 0 || targetCards.Count > 3)
                {
                    message = "Select 1 to 3 cards to convert to Clubs.";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    card.Suit = Suit.Clubs;
                }
                message = $"Converted {targetCards.Count} card(s) to Clubs.";
                return true;

            case TarotType.TheSun:
                if (targetCards.Count == 0 || targetCards.Count > 3)
                {
                    message = "Select 1 to 3 cards to convert to Hearts.";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    card.Suit = Suit.Hearts;
                }
                message = $"Converted {targetCards.Count} card(s) to Hearts.";
                return true;

            case TarotType.TheWorld:
                if (targetCards.Count == 0 || targetCards.Count > 3)
                {
                    message = "Select 1 to 3 cards to convert to Spades.";
                    return false;
                }
                foreach (var card in targetCards)
                {
                    card.Suit = Suit.Spades;
                }
                message = $"Converted {targetCards.Count} card(s) to Spades.";
                return true;

            case TarotType.Judgement:
                if (controller.Deck.IsJokerContainerFull())
                {
                    message = "Joker slots are full!";
                    return false;
                }
                var newJoker = ShopService.GenerateRandomJoker(false);
                controller.Deck.JokerCards.Add(newJoker);
                message = $"Judgement spawned {newJoker.Name}!";
                return true;

            default:
                message = "Tarot card used.";
                return true;
        }
    }

    public bool UsePlanet(GameController controller, PlanetCard planet, out string message)
    {
        _logger.LogDebug("Applying planet {PokerHandType}", planet.PokerHandType);

        if (controller.PokerHandLevels.ContainsKey(planet.PokerHandType))
        {
            controller.PokerHandLevels[planet.PokerHandType]++;
        }
        else
        {
            controller.PokerHandLevels[planet.PokerHandType] = 2;
        }

        ApplyConstellationJokerEffect(controller);

        int newLevel = controller.PokerHandLevels[planet.PokerHandType];
        message = $"Upgraded {planet.PokerHandType} to Level {newLevel}!";
        return true;
    }

    private static void ApplyConstellationJokerEffect(GameController controller)
    {
        var constellation = controller.Deck.JokerCards.FirstOrDefault(j => j.JokerId == JokerId.Constellation);
        if (constellation != null)
        {
            constellation.XMultValue += 0.1f;
        }
    }

    public bool UseSpectral(GameController controller, SpectralCard spectral, out string message)
    {
        _logger.LogDebug(
            "Applying spectral {SpectralType} with {HandCardCount} cards in hand",
            spectral.Type,
            controller.Hand.Count);

        message = string.Empty;

        switch (spectral.Type)
        {
            case SpectralType.Familiar:
                if (controller.Hand.Count > 0)
                {
                    var toDestroy = controller.Hand[_random.Next(controller.Hand.Count)];
                    controller.Hand.Remove(toDestroy);
                    for (int i = 0; i < 3; i++)
                    {
                        var faceRanks = new[] { Rank.Jack, Rank.Queen, Rank.King };
                        var r = faceRanks[_random.Next(faceRanks.Length)];
                        var s = (Suit)_random.Next(4);
                        var enh = (EnhancePokerCard)_random.Next(1, Enum.GetValues<EnhancePokerCard>().Length);
                        controller.Hand.Add(new PlayingCard(s, r, enh));
                    }
                    message = $"Familiar destroyed {toDestroy.Name} and added 3 Enhanced face cards!";
                    return true;
                }
                message = "Hand is empty!";
                return false;

            case SpectralType.Grim:
                if (controller.Hand.Count > 0)
                {
                    var toDestroy = controller.Hand[_random.Next(controller.Hand.Count)];
                    controller.Hand.Remove(toDestroy);
                    for (int i = 0; i < 2; i++)
                    {
                        var s = (Suit)_random.Next(4);
                        var enh = (EnhancePokerCard)_random.Next(1, Enum.GetValues<EnhancePokerCard>().Length);
                        controller.Hand.Add(new PlayingCard(s, Rank.Ace, enh));
                    }
                    message = $"Grim destroyed {toDestroy.Name} and added 2 Enhanced Aces!";
                    return true;
                }
                message = "Hand is empty!";
                return false;

            case SpectralType.Incantation:
                if (controller.Hand.Count > 0)
                {
                    var toDestroy = controller.Hand[_random.Next(controller.Hand.Count)];
                    controller.Hand.Remove(toDestroy);
                    for (int i = 0; i < 4; i++)
                    {
                        var r = (Rank)_random.Next(2, 11);
                        var s = (Suit)_random.Next(4);
                        var enh = (EnhancePokerCard)_random.Next(1, Enum.GetValues<EnhancePokerCard>().Length);
                        controller.Hand.Add(new PlayingCard(s, r, enh));
                    }
                    message = $"Incantation destroyed {toDestroy.Name} and added 4 Enhanced numbered cards!";
                    return true;
                }
                message = "Hand is empty!";
                return false;

            case SpectralType.Wraith:
                if (controller.Deck.IsJokerContainerFull())
                {
                    message = "Joker slots are full!";
                    return false;
                }
                var wraithJoker = ShopService.GenerateRandomJoker(false);
                wraithJoker.Rarity = JokerRarity.Rare;
                controller.Deck.JokerCards.Add(wraithJoker);
                controller.Money = 0;
                message = $"Wraith summoned Rare Joker {wraithJoker.Name} and set money to $0!";
                return true;

            case SpectralType.Sigil:
                if (controller.Hand.Count > 0)
                {
                    var targetSuit = (Suit)_random.Next(4);
                    foreach (var card in controller.Hand)
                    {
                        card.Suit = targetSuit;
                    }
                    message = $"Sigil converted all cards in hand to {targetSuit}!";
                    return true;
                }
                message = "Hand is empty!";
                return false;

            default:
                message = "Spectral card used.";
                return true;
        }
    }
}
