using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Shop;

namespace BackendBalatro.Services.Consumables;

public class ConsumableEffectHandler : IConsumableEffectHandler
{
    private static readonly Random _random = new();

    public bool UseTarot(GameEngine engine, TarotCard tarot, List<string> targetCardIds, out string message)
    {
        message = string.Empty;

        var targetCards = engine.Hand.Where(c => targetCardIds.Contains(c.Id)).ToList();

        switch (tarot.Type)
        {
            case TarotType.TheFool:
                if (engine.LastTarotUsed != null && engine.LastTarotUsed.Type != TarotType.TheFool)
                {
                    if (engine.Deck.IsConsumableContainerFull())
                    {
                        message = "Consumable inventory is full!";
                        return false;
                    }
                    var clone = new TarotCard(engine.LastTarotUsed.Name, engine.LastTarotUsed.Price, engine.LastTarotUsed.Type);
                    engine.Deck.UsableCards.Add(clone);
                    message = $"The Fool created {clone.Name}!";
                    return true;
                }
                if (engine.LastPlanetUsed != null)
                {
                    if (engine.Deck.IsConsumableContainerFull())
                    {
                        message = "Consumable inventory is full!";
                        return false;
                    }
                    var clone = new PlanetCard(engine.LastPlanetUsed.Name, engine.LastPlanetUsed.PokerHandType);
                    engine.Deck.UsableCards.Add(clone);
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
                if (engine.Deck.IsConsumableContainerFull())
                {
                    message = "Consumable slots are full!";
                    return false;
                }
                int countP = Math.Min(2, engine.Deck.MaxConsumableContainer - engine.Deck.UsableCards.Count + 1); // +1 because current tarot is being consumed
                for (int i = 0; i < countP; i++)
                {
                    var hand = (PokerHandType)_random.Next(Enum.GetValues<PokerHandType>().Length);
                    engine.Deck.UsableCards.Add(PlanetCard.CreateForHand(hand));
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
                if (engine.Deck.IsConsumableContainerFull())
                {
                    message = "Consumable slots are full!";
                    return false;
                }
                int countT = Math.Min(2, engine.Deck.MaxConsumableContainer - engine.Deck.UsableCards.Count + 1);
                for (int i = 0; i < countT; i++)
                {
                    var t = (TarotType)_random.Next(Enum.GetValues<TarotType>().Length);
                    engine.Deck.UsableCards.Add(new TarotCard(t.ToString(), 3, t));
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
                int gained = Math.Min(20, engine.Money);
                engine.Money += gained;
                message = $"The Hermit doubled your money! (Gained ${gained})";
                return true;

            case TarotType.TheWheelFortune:
                if (engine.Deck.JokerCards.Count == 0)
                {
                    message = "No Jokers available to upgrade!";
                    return false;
                }
                bool wheelHit = _random.Next(4) == 0; // 1 in 4 chance
                if (wheelHit)
                {
                    var eligibleJokers = engine.Deck.JokerCards.Where(j => j.Edition == JokerEdition.Base).ToList();
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
                    engine.Hand.Remove(card);
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
                if (engine.Deck.IsJokerContainerFull())
                {
                    message = "Joker slots are full!";
                    return false;
                }
                var newJoker = ShopService.GenerateRandomJoker(false);
                engine.Deck.JokerCards.Add(newJoker);
                message = $"Judgement spawned {newJoker.Name}!";
                return true;

            default:
                message = "Tarot card used.";
                return true;
        }
    }

    public bool UsePlanet(GameEngine engine, PlanetCard planet, out string message)
    {
        if (engine.PokerHandLevels.ContainsKey(planet.PokerHandType))
        {
            engine.PokerHandLevels[planet.PokerHandType]++;
        }
        else
        {
            engine.PokerHandLevels[planet.PokerHandType] = 2;
        }

        // Check if player has Constellation joker (+X0.1 Mult on planet use)
        var constellation = engine.Deck.JokerCards.FirstOrDefault(j => j.JokerKey == "constellation");
        if (constellation != null)
        {
            constellation.XMultValue += 0.1f;
        }

        int newLevel = engine.PokerHandLevels[planet.PokerHandType];
        message = $"Upgraded {planet.PokerHandType} to Level {newLevel}!";
        return true;
    }

    public bool UseSpectral(GameEngine engine, SpectralCard spectral, out string message)
    {
        message = string.Empty;

        switch (spectral.Type)
        {
            case SpectralType.Familiar:
                if (engine.Hand.Count > 0)
                {
                    var toDestroy = engine.Hand[_random.Next(engine.Hand.Count)];
                    engine.Hand.Remove(toDestroy);
                    for (int i = 0; i < 3; i++)
                    {
                        var faceRanks = new[] { Rank.Jack, Rank.Queen, Rank.King };
                        var r = faceRanks[_random.Next(faceRanks.Length)];
                        var s = (Suit)_random.Next(4);
                        var enh = (EnhancePokerCard)_random.Next(1, Enum.GetValues<EnhancePokerCard>().Length);
                        engine.Hand.Add(new PlayingCard(s, r, enh));
                    }
                    message = $"Familiar destroyed {toDestroy.Name} and added 3 Enhanced face cards!";
                    return true;
                }
                message = "Hand is empty!";
                return false;

            case SpectralType.Grim:
                if (engine.Hand.Count > 0)
                {
                    var toDestroy = engine.Hand[_random.Next(engine.Hand.Count)];
                    engine.Hand.Remove(toDestroy);
                    for (int i = 0; i < 2; i++)
                    {
                        var s = (Suit)_random.Next(4);
                        var enh = (EnhancePokerCard)_random.Next(1, Enum.GetValues<EnhancePokerCard>().Length);
                        engine.Hand.Add(new PlayingCard(s, Rank.Ace, enh));
                    }
                    message = $"Grim destroyed {toDestroy.Name} and added 2 Enhanced Aces!";
                    return true;
                }
                message = "Hand is empty!";
                return false;

            case SpectralType.Incantation:
                if (engine.Hand.Count > 0)
                {
                    var toDestroy = engine.Hand[_random.Next(engine.Hand.Count)];
                    engine.Hand.Remove(toDestroy);
                    for (int i = 0; i < 4; i++)
                    {
                        var r = (Rank)_random.Next(2, 11);
                        var s = (Suit)_random.Next(4);
                        var enh = (EnhancePokerCard)_random.Next(1, Enum.GetValues<EnhancePokerCard>().Length);
                        engine.Hand.Add(new PlayingCard(s, r, enh));
                    }
                    message = $"Incantation destroyed {toDestroy.Name} and added 4 Enhanced numbered cards!";
                    return true;
                }
                message = "Hand is empty!";
                return false;

            case SpectralType.Wraith:
                if (engine.Deck.IsJokerContainerFull())
                {
                    message = "Joker slots are full!";
                    return false;
                }
                var wraithJoker = ShopService.GenerateRandomJoker(false);
                wraithJoker.Rarity = JokerRarity.Rare;
                engine.Deck.JokerCards.Add(wraithJoker);
                engine.Money = 0;
                message = $"Wraith summoned Rare Joker {wraithJoker.Name} and set money to $0!";
                return true;

            case SpectralType.Sigil:
                if (engine.Hand.Count > 0)
                {
                    var targetSuit = (Suit)_random.Next(4);
                    foreach (var card in engine.Hand)
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
