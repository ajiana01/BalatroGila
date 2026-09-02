using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackendBalatro.Tests;

[TestFixture]
public class ConsumableEffectHandlerServiceTests
{
    private ConsumableEffectHandler _handler;
    private GameController _controller;

    [SetUp]
    public void SetUp()
    {
        _handler = new ConsumableEffectHandler(NullLogger<ConsumableEffectHandler>.Instance);
        _controller = CreateController();
    }

    private GameController CreateController()
    {
        return new GameController(
            new Mock<IScoringService>().Object,
            new Mock<IShopService>().Object,
            _handler,
            NullLogger<GameController>.Instance);
    }

    [Test]
    public void UseTarot_TheFoolAfterTarot_CreatesCloneOfLastTarot()
    {
        var lastTarot = new TarotCard("The Magician", 3, TarotType.TheMagician);
        var fool = new TarotCard("The Fool", 3, TarotType.TheFool);
        _controller.LastTarotUsed = lastTarot;

        var result = _handler.UseTarot(_controller, fool, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Deck.UsableCards, Has.Count.EqualTo(1));
            Assert.That(_controller.Deck.UsableCards[0], Is.TypeOf<TarotCard>());
            var clone = (TarotCard)_controller.Deck.UsableCards[0];
            Assert.That(clone.Name, Is.EqualTo(lastTarot.Name));
            Assert.That(clone.Price, Is.EqualTo(lastTarot.Price));
            Assert.That(clone.Type, Is.EqualTo(lastTarot.Type));
            Assert.That(clone.Id, Is.Not.EqualTo(lastTarot.Id));
            Assert.That(message, Is.EqualTo("The Fool created The Magician!"));
        });
    }

    [Test]
    public void UseTarot_TheFoolAfterPlanet_CreatesCloneOfLastPlanet()
    {
        var lastPlanet = PlanetCard.CreateForHand(PokerHandType.Flush);
        var fool = new TarotCard("The Fool", 3, TarotType.TheFool);
        _controller.LastPlanetUsed = lastPlanet;

        var result = _handler.UseTarot(_controller, fool, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Deck.UsableCards, Has.Count.EqualTo(1));
            Assert.That(_controller.Deck.UsableCards[0], Is.TypeOf<PlanetCard>());
            var clone = (PlanetCard)_controller.Deck.UsableCards[0];
            Assert.That(clone.Name, Is.EqualTo(lastPlanet.Name));
            Assert.That(clone.PokerHandType, Is.EqualTo(lastPlanet.PokerHandType));
            Assert.That(clone.Id, Is.Not.EqualTo(lastPlanet.Id));
            Assert.That(message, Is.EqualTo($"The Fool created {lastPlanet.Name}!"));
        });
    }

    [Test]
    public void UseTarot_TheFoolWithoutHistory_ReturnsFalse()
    {
        var fool = new TarotCard("The Fool", 3, TarotType.TheFool);

        var result = _handler.UseTarot(_controller, fool, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("No previous Tarot or Planet card was used!"));
            Assert.That(_controller.Deck.UsableCards, Is.Empty);
        });
    }

    [Test]
    public void UseTarot_TheFoolWhenInventoryFull_ReturnsFalse()
    {
        _controller.LastTarotUsed = new TarotCard("The Magician", 3, TarotType.TheMagician);
        _controller.Deck.UsableCards.Add(new TarotCard("Existing Tarot 1", 3, TarotType.TheSun));
        _controller.Deck.UsableCards.Add(new TarotCard("Existing Tarot 2", 3, TarotType.TheMoon));
        var fool = new TarotCard("The Fool", 3, TarotType.TheFool);

        var result = _handler.UseTarot(_controller, fool, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("Consumable inventory is full!"));
            Assert.That(_controller.Deck.UsableCards, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void UseTarot_HighPriestessWithCapacity_CreatesUpToTwoPlanetCards()
    {
        var priestess = new TarotCard("The High Priestess", 3, TarotType.TheHighPriestess);
        _controller.Deck.UsableCards.Add(priestess);

        var result = _controller.UseConsumable(priestess.Id, new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Created Planet cards!"));
            Assert.That(_controller.Deck.UsableCards, Has.Count.EqualTo(2));
            Assert.That(_controller.Deck.UsableCards, Has.All.TypeOf<PlanetCard>());
            Assert.That(_controller.Deck.IsConsumableContainerFull(), Is.True);
        });
    }

    [Test]
    public void UseTarot_HighPriestessWhenInventoryFull_ReturnsFalse()
    {
        _controller.Deck.UsableCards.Add(new TarotCard("Existing Tarot 1", 3, TarotType.TheSun));
        _controller.Deck.UsableCards.Add(new TarotCard("Existing Tarot 2", 3, TarotType.TheMoon));
        var priestess = new TarotCard("The High Priestess", 3, TarotType.TheHighPriestess);

        var result = _handler.UseTarot(_controller, priestess, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("Consumable slots are full!"));
            Assert.That(_controller.Deck.UsableCards, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void UseTarot_EmperorWithCapacity_CreatesUpToTwoTarotCards()
    {
        var emperor = new TarotCard("The Emperor", 3, TarotType.TheEmperor);
        _controller.Deck.UsableCards.Add(emperor);

        var result = _controller.UseConsumable(emperor.Id, new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Created Tarot cards!"));
            Assert.That(_controller.Deck.UsableCards, Has.Count.EqualTo(2));
            Assert.That(_controller.Deck.UsableCards, Has.All.TypeOf<TarotCard>());
            Assert.That(_controller.Deck.IsConsumableContainerFull(), Is.True);
        });
    }

    [Test]
    public void UseTarot_EmperorWhenInventoryFull_ReturnsFalse()
    {
        _controller.Deck.UsableCards.Add(new TarotCard("Existing Tarot 1", 3, TarotType.TheSun));
        _controller.Deck.UsableCards.Add(new TarotCard("Existing Tarot 2", 3, TarotType.TheMoon));
        var emperor = new TarotCard("The Emperor", 3, TarotType.TheEmperor);

        var result = _handler.UseTarot(_controller, emperor, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("Consumable slots are full!"));
            Assert.That(_controller.Deck.UsableCards, Has.Count.EqualTo(2));
        });
    }

    [TestCase(TarotType.TheMagician, EnhancePokerCard.LuckyCards, "Lucky Card", 1)]
    [TestCase(TarotType.TheMagician, EnhancePokerCard.LuckyCards, "Lucky Card", 2)]
    [TestCase(TarotType.TheEmpress, EnhancePokerCard.MultCards, "Mult (+4 Mult)", 1)]
    [TestCase(TarotType.TheEmpress, EnhancePokerCard.MultCards, "Mult (+4 Mult)", 2)]
    [TestCase(TarotType.TheHierophant, EnhancePokerCard.BonusCards, "Bonus (+30 Chips)", 1)]
    [TestCase(TarotType.TheHierophant, EnhancePokerCard.BonusCards, "Bonus (+30 Chips)", 2)]
    public void UseTarot_OneOrTwoTargets_AppliesExpectedEnhancement(
        TarotType tarotType, EnhancePokerCard expectedEnhancement, string _, int targetCount)
    {
        var targetCards = new[]
        {
            new PlayingCard(Suit.Hearts, Rank.Ace),
            new PlayingCard(Suit.Spades, Rank.King),
            new PlayingCard(Suit.Clubs, Rank.Two)
        };
        _controller.Hand.AddRange(targetCards);
        var tarot = new TarotCard(tarotType.ToString(), 3, tarotType);

        var result = _handler.UseTarot(
            _controller, tarot, targetCards.Take(targetCount).Select(card => card.Id).ToList(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(targetCards.Take(targetCount).All(card => card.Enhancement == expectedEnhancement), Is.True);
            Assert.That(targetCards.Skip(targetCount).All(card => card.Enhancement == EnhancePokerCard.None), Is.True);
            Assert.That(message, Does.Contain($"Enhanced {targetCount} card(s)"));
        });
    }

    [TestCase(TarotType.TheMagician, 0, "Select 1 or 2 cards to enhance with Lucky Card.")]
    [TestCase(TarotType.TheMagician, 3, "Select 1 or 2 cards to enhance with Lucky Card.")]
    [TestCase(TarotType.TheEmpress, 0, "Select 1 or 2 cards to enhance with Mult (+4 Mult).")]
    [TestCase(TarotType.TheEmpress, 3, "Select 1 or 2 cards to enhance with Mult (+4 Mult).")]
    [TestCase(TarotType.TheHierophant, 0, "Select 1 or 2 cards to enhance with Bonus (+30 Chips).")]
    [TestCase(TarotType.TheHierophant, 3, "Select 1 or 2 cards to enhance with Bonus (+30 Chips).")]
    public void UseTarot_EnhancementTargetCountOutsideRange_ReturnsFalse(
        TarotType tarotType, int targetCount, string expectedMessage)
    {
        var cards = new[]
        {
            new PlayingCard(Suit.Hearts, Rank.Ace),
            new PlayingCard(Suit.Spades, Rank.King),
            new PlayingCard(Suit.Clubs, Rank.Two)
        };
        _controller.Hand.AddRange(cards);
        var tarot = new TarotCard(tarotType.ToString(), 3, tarotType);
        var targetIds = targetCount == 0
            ? new List<string>()
            : cards.Select(card => card.Id).ToList();

        var result = _handler.UseTarot(_controller, tarot, targetIds, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo(expectedMessage));
            Assert.That(cards.All(card => card.Enhancement == EnhancePokerCard.None), Is.True);
        });
    }

    [TestCase(TarotType.TheLovers, EnhancePokerCard.WildCards, "Wild Card")]
    [TestCase(TarotType.TheChariot, EnhancePokerCard.SteelCards, "Steel Card")]
    [TestCase(TarotType.Justice, EnhancePokerCard.GlassCards, "Glass Card")]
    [TestCase(TarotType.TheDevil, EnhancePokerCard.GoldCards, "Gold Card")]
    [TestCase(TarotType.TheTower, EnhancePokerCard.StoneCards, "Stone Card (+50 Chips)")]
    public void UseTarot_ExactlyOneTarget_AppliesExpectedEnhancement(
        TarotType tarotType, EnhancePokerCard expectedEnhancement, string expectedMessagePart)
    {
        var target = new PlayingCard(Suit.Hearts, Rank.Ace);
        var other = new PlayingCard(Suit.Spades, Rank.King);
        _controller.Hand.AddRange(new[] { target, other });
        var tarot = new TarotCard(tarotType.ToString(), 3, tarotType);

        var result = _handler.UseTarot(_controller, tarot, new List<string> { target.Id }, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(target.Enhancement, Is.EqualTo(expectedEnhancement));
            Assert.That(other.Enhancement, Is.EqualTo(EnhancePokerCard.None));
            Assert.That(message, Does.Contain(expectedMessagePart));
        });
    }

    [TestCase(TarotType.TheLovers)]
    [TestCase(TarotType.TheChariot)]
    [TestCase(TarotType.Justice)]
    [TestCase(TarotType.TheDevil)]
    [TestCase(TarotType.TheTower)]
    public void UseTarot_ExactlyOneTargetWithInvalidCount_ReturnsFalse(TarotType tarotType)
    {
        var target = new PlayingCard(Suit.Hearts, Rank.Ace);
        var second = new PlayingCard(Suit.Spades, Rank.King);
        _controller.Hand.AddRange(new[] { target, second });
        var tarot = new TarotCard(tarotType.ToString(), 3, tarotType);

        var zeroResult = _handler.UseTarot(_controller, tarot, new List<string>(), out var zeroMessage);
        var twoResult = _handler.UseTarot(
            _controller, tarot, new List<string> { target.Id, second.Id }, out var twoMessage);

        Assert.Multiple(() =>
        {
            Assert.That(zeroResult, Is.False);
            Assert.That(twoResult, Is.False);
            Assert.That(zeroMessage, Does.StartWith("Select exactly 1 card"));
            Assert.That(twoMessage, Does.StartWith("Select exactly 1 card"));
            Assert.That(target.Enhancement, Is.EqualTo(EnhancePokerCard.None));
            Assert.That(second.Enhancement, Is.EqualTo(EnhancePokerCard.None));
        });
    }

    [TestCase(TarotType.TheStar, Suit.Diamonds)]
    [TestCase(TarotType.TheMoon, Suit.Clubs)]
    [TestCase(TarotType.TheSun, Suit.Hearts)]
    [TestCase(TarotType.TheWorld, Suit.Spades)]
    public void UseTarot_SuitConversion_ConvertsOneToThreeCards(TarotType tarotType, Suit expectedSuit)
    {
        var cards = new[]
        {
            new PlayingCard(Suit.Hearts, Rank.Ace, EnhancePokerCard.MultCards) { Edition = JokerEdition.Foil },
            new PlayingCard(Suit.Spades, Rank.King),
            new PlayingCard(Suit.Clubs, Rank.Two)
        };
        _controller.Hand.AddRange(cards);
        var originalRanks = cards.Select(card => card.Rank).ToList();
        var tarot = new TarotCard(tarotType.ToString(), 3, tarotType);

        var result = _handler.UseTarot(
            _controller, tarot, cards.Select(card => card.Id).ToList(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(cards.All(card => card.Suit == expectedSuit), Is.True);
            Assert.That(cards.Select(card => card.Rank), Is.EqualTo(originalRanks));
            Assert.That(cards[0].Enhancement, Is.EqualTo(EnhancePokerCard.MultCards));
            Assert.That(cards[0].Edition, Is.EqualTo(JokerEdition.Foil));
            Assert.That(message, Does.Contain($"Converted 3 card(s) to {expectedSuit}"));
        });
    }

    [TestCase(TarotType.TheStar)]
    [TestCase(TarotType.TheMoon)]
    [TestCase(TarotType.TheSun)]
    [TestCase(TarotType.TheWorld)]
    public void UseTarot_SuitConversionWithInvalidCount_ReturnsFalse(TarotType tarotType)
    {
        var cards = new[]
        {
            new PlayingCard(Suit.Hearts, Rank.Ace),
            new PlayingCard(Suit.Spades, Rank.King),
            new PlayingCard(Suit.Clubs, Rank.Two),
            new PlayingCard(Suit.Diamonds, Rank.Three)
        };
        _controller.Hand.AddRange(cards);
        var originalSuits = cards.Select(card => card.Suit).ToList();
        var tarot = new TarotCard(tarotType.ToString(), 3, tarotType);

        var zeroResult = _handler.UseTarot(_controller, tarot, new List<string>(), out var zeroMessage);
        var fourResult = _handler.UseTarot(
            _controller, tarot, cards.Select(card => card.Id).ToList(), out var fourMessage);

        Assert.Multiple(() =>
        {
            Assert.That(zeroResult, Is.False);
            Assert.That(fourResult, Is.False);
            Assert.That(zeroMessage, Does.StartWith("Select 1 to 3 cards"));
            Assert.That(fourMessage, Does.StartWith("Select 1 to 3 cards"));
            Assert.That(cards.Select(card => card.Suit), Is.EqualTo(originalSuits));
        });
    }

    [Test]
    public void UseTarot_Strength_IncrementsRanksAndRecalculatesBaseChips()
    {
        var nine = new PlayingCard(Suit.Hearts, Rank.Nine);
        var king = new PlayingCard(Suit.Spades, Rank.King);
        _controller.Hand.AddRange(new[] { nine, king });
        var strength = new TarotCard("Strength", 3, TarotType.Strength);

        var result = _handler.UseTarot(
            _controller, strength, new List<string> { nine.Id, king.Id }, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(nine.Rank, Is.EqualTo(Rank.Ten));
            Assert.That(nine.BaseChips, Is.EqualTo(10));
            Assert.That(king.Rank, Is.EqualTo(Rank.Ace));
            Assert.That(king.BaseChips, Is.EqualTo(11));
            Assert.That(message, Is.EqualTo("Increased rank of 2 card(s)."));
        });
    }

    [Test]
    public void UseTarot_StrengthOnAce_WrapsToTwo()
    {
        var ace = new PlayingCard(Suit.Hearts, Rank.Ace);
        _controller.Hand.Add(ace);
        var strength = new TarotCard("Strength", 3, TarotType.Strength);

        var result = _handler.UseTarot(_controller, strength, new List<string> { ace.Id }, out _);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(ace.Rank, Is.EqualTo(Rank.Two));
            Assert.That(ace.BaseChips, Is.EqualTo(2));
        });
    }

    [Test]
    public void UseTarot_HangedMan_RemovesSelectedCardsFromHand()
    {
        var first = new PlayingCard(Suit.Hearts, Rank.Ace);
        var second = new PlayingCard(Suit.Spades, Rank.King);
        var remaining = new PlayingCard(Suit.Clubs, Rank.Two);
        _controller.Hand.AddRange(new[] { first, second, remaining });
        var hangedMan = new TarotCard("The Hanged Man", 3, TarotType.TheHangedMan);

        var result = _handler.UseTarot(
            _controller, hangedMan, new List<string> { first.Id, second.Id }, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Hand, Does.Not.Contain(first));
            Assert.That(_controller.Hand, Does.Not.Contain(second));
            Assert.That(_controller.Hand, Does.Contain(remaining));
            Assert.That(message, Is.EqualTo("Destroyed 2 card(s)."));
        });
    }

    [Test]
    public void UseTarot_HangedManWithInvalidCount_ReturnsFalse()
    {
        var cards = new[]
        {
            new PlayingCard(Suit.Hearts, Rank.Ace),
            new PlayingCard(Suit.Spades, Rank.King),
            new PlayingCard(Suit.Clubs, Rank.Two)
        };
        _controller.Hand.AddRange(cards);
        var hangedMan = new TarotCard("The Hanged Man", 3, TarotType.TheHangedMan);

        var zeroResult = _handler.UseTarot(
            _controller, hangedMan, new List<string>(), out var zeroMessage);
        var threeResult = _handler.UseTarot(
            _controller, hangedMan, cards.Select(card => card.Id).ToList(), out var threeMessage);

        Assert.Multiple(() =>
        {
            Assert.That(zeroResult, Is.False);
            Assert.That(threeResult, Is.False);
            Assert.That(zeroMessage, Is.EqualTo("Select 1 or 2 cards to destroy."));
            Assert.That(threeMessage, Is.EqualTo("Select 1 or 2 cards to destroy."));
            Assert.That(_controller.Hand, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void UseTarot_Death_CopiesRightCardPropertiesToLeftCard()
    {
        var left = new PlayingCard(Suit.Hearts, Rank.Two, EnhancePokerCard.None)
        {
            Edition = JokerEdition.Base, BaseChips = 2, BaseMult = 1f, BaseXMult = 1f
        };
        var right = new PlayingCard(Suit.Spades, Rank.Ace, EnhancePokerCard.GlassCards)
        {
            Edition = JokerEdition.Polychrome, BaseChips = 11, BaseMult = 4f, BaseXMult = 1.5f
        };
        _controller.Hand.AddRange(new[] { left, right });
        var originalLeftId = left.Id;
        var death = new TarotCard("Death", 3, TarotType.Death);

        var result = _handler.UseTarot(
            _controller, death, new List<string> { left.Id, right.Id }, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(left.Id, Is.EqualTo(originalLeftId));
            Assert.That(left.Rank, Is.EqualTo(right.Rank));
            Assert.That(left.Suit, Is.EqualTo(right.Suit));
            Assert.That(left.Enhancement, Is.EqualTo(right.Enhancement));
            Assert.That(left.Edition, Is.EqualTo(right.Edition));
            Assert.That(left.BaseChips, Is.EqualTo(right.BaseChips));
            Assert.That(left.BaseMult, Is.EqualTo(right.BaseMult));
            Assert.That(left.BaseXMult, Is.EqualTo(right.BaseXMult));
            Assert.That(message, Is.EqualTo($"Converted {left.Name} to match {right.Name}."));
        });
    }

    [TestCase(10, 20)]
    [TestCase(30, 50)]
    public void UseTarot_Hermit_DoublesMoneyWithTwentyDollarGainCap(int initialMoney, int expectedMoney)
    {
        _controller.Money = initialMoney;
        var hermit = new TarotCard("The Hermit", 3, TarotType.TheHermit);

        var result = _handler.UseTarot(_controller, hermit, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Money, Is.EqualTo(expectedMoney));
            Assert.That(message, Is.EqualTo($"The Hermit doubled your money! (Gained ${expectedMoney - initialMoney})"));
        });
    }

    [TestCase(4, 4)]
    [TestCase(20, 50)]
    public void UseTarot_Temperance_AddsJokerSellValueWithFiftyDollarCap(int jokerPrice, int expectedGain)
    {
        var jokers = Enumerable.Range(0, jokerPrice == 4 ? 2 : 6)
            .Select(index => new JokerCard(
                $"Joker {index}", JokerEdition.Base, JokerRarity.Common,
                JokerModifierType.AdditionMultiplier, 0, jokerPrice))
            .ToList();
        _controller.Deck.JokerCards.AddRange(jokers);
        _controller.Money = 0;
        var temperance = new TarotCard("Temperance", 3, TarotType.TheTemperance);

        var result = _handler.UseTarot(_controller, temperance, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Money, Is.EqualTo(expectedGain));
            Assert.That(_controller.Deck.JokerCards, Is.EqualTo(jokers));
            Assert.That(message, Is.EqualTo($"Temperance gave ${expectedGain} from Joker sell values!"));
        });
    }

    [Test]
    public void UseTarot_WheelOfFortuneWithoutJokers_ReturnsFalse()
    {
        var wheel = new TarotCard("The Wheel of Fortune", 3, TarotType.TheWheelFortune);

        var result = _handler.UseTarot(_controller, wheel, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("No Jokers available to upgrade!"));
        });
    }

    [Test]
    public void UseTarot_WheelOfFortuneOnHit_UpgradesOneBaseJokerEdition()
    {
        var joker = new JokerCard("Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 4, 4);
        _controller.Deck.JokerCards.Add(joker);
        var wheel = new TarotCard("The Wheel of Fortune", 3, TarotType.TheWheelFortune);
        var upgraded = false;
        var message = string.Empty;

        for (var attempt = 0; attempt < 1000 && !upgraded; attempt++)
        {
            var result = _handler.UseTarot(_controller, wheel, new List<string>(), out message);
            upgraded = result && joker.Edition != JokerEdition.Base;
        }

        Assert.Multiple(() =>
        {
            Assert.That(upgraded, Is.True, "Wheel of Fortune did not produce a hit after 1000 attempts.");
            Assert.That(joker.Edition, Is.AnyOf(JokerEdition.Foil, JokerEdition.Holographic, JokerEdition.Polychrome));
            Assert.That(message, Does.Contain(joker.Name));
            Assert.That(message, Does.Contain(joker.Edition.ToString()));
        });
    }

    [Test]
    public void UseTarot_WheelOfFortuneOnMiss_ReturnsTrueWithoutMutation()
    {
        var joker = new JokerCard("Foil Joker", JokerEdition.Foil, JokerRarity.Common,
            JokerModifierType.AdditionMultiplier, 4, 4);
        _controller.Deck.JokerCards.Add(joker);
        var wheel = new TarotCard("The Wheel of Fortune", 3, TarotType.TheWheelFortune);

        var result = _handler.UseTarot(_controller, wheel, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(message, Is.EqualTo("Nope! Wheel of Fortune gave nothing."));
            Assert.That(joker.Edition, Is.EqualTo(JokerEdition.Foil));
            Assert.That(_controller.Deck.JokerCards, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void UseTarot_JudgementWithFreeSlot_AddsRandomJoker()
    {
        var judgement = new TarotCard("Judgement", 3, TarotType.Judgement);

        var result = _handler.UseTarot(_controller, judgement, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Deck.JokerCards, Has.Count.EqualTo(1));
            Assert.That(_controller.Deck.JokerCards[0].Name, Is.Not.Empty);
            Assert.That(message, Is.EqualTo($"Judgement spawned {_controller.Deck.JokerCards[0].Name}!"));
        });
    }

    [Test]
    public void UseTarot_JudgementWhenJokerSlotsFull_ReturnsFalse()
    {
        for (var index = 0; index < _controller.Deck.MaxJokerContainer; index++)
        {
            _controller.Deck.JokerCards.Add(new JokerCard(
                $"Joker {index}", JokerEdition.Base, JokerRarity.Common,
                JokerModifierType.AdditionMultiplier, 0, 4));
        }
        var judgement = new TarotCard("Judgement", 3, TarotType.Judgement);

        var result = _handler.UseTarot(_controller, judgement, new List<string>(), out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("Joker slots are full!"));
            Assert.That(_controller.Deck.JokerCards, Has.Count.EqualTo(_controller.Deck.MaxJokerContainer));
        });
    }

    [Test]
    public void UsePlanet_ExistingHandType_IncrementsLevelAndReturnsMessage()
    {
        _controller.PokerHandLevels[PokerHandType.Flush] = 2;
        var planet = PlanetCard.CreateForHand(PokerHandType.Flush);

        var result = _handler.UsePlanet(_controller, planet, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.PokerHandLevels[PokerHandType.Flush], Is.EqualTo(3));
            Assert.That(message, Is.EqualTo("Upgraded Flush to Level 3!"));
        });
    }

    [Test]
    public void UsePlanet_MissingHandType_InitializesLevelTwo()
    {
        _controller.PokerHandLevels.Remove(PokerHandType.Flush);
        var planet = PlanetCard.CreateForHand(PokerHandType.Flush);

        var result = _handler.UsePlanet(_controller, planet, out _);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.PokerHandLevels[PokerHandType.Flush], Is.EqualTo(2));
        });
    }

    [Test]
    public void UsePlanet_WithConstellation_IncrementsXMultByPointOne()
    {
        var constellation = new JokerCard(
            JokerId.Constellation, "Constellation", JokerEdition.Base, JokerRarity.Uncommon,
            JokerModifierType.MultiplierMultiplier, 1.0f, 6);
        _controller.Deck.JokerCards.Add(constellation);
        var initialXMult = constellation.XMultValue;
        var initialLevel = _controller.PokerHandLevels[PokerHandType.Flush];
        var planet = PlanetCard.CreateForHand(PokerHandType.Flush);

        var result = _handler.UsePlanet(_controller, planet, out _);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(constellation.XMultValue, Is.EqualTo(initialXMult + 0.1f).Within(0.0001f));
            Assert.That(_controller.PokerHandLevels[PokerHandType.Flush], Is.EqualTo(initialLevel + 1));
        });
    }

    [Test]
    public void UsePlanet_WithoutConstellation_DoesNotMutateOtherJokers()
    {
        var joker = new JokerCard(
            JokerId.GoldenJoker, "Golden Joker", JokerEdition.Base, JokerRarity.Common,
            JokerModifierType.Money, 4, 6);
        var initialSnapshot = new
        {
            joker.Edition,
            joker.ChipsValue,
            joker.MultValue,
            joker.XMultValue,
            joker.MoneyValue,
            joker.Price
        };
        _controller.Deck.JokerCards.Add(joker);
        var initialLevel = _controller.PokerHandLevels[PokerHandType.Flush];
        var planet = PlanetCard.CreateForHand(PokerHandType.Flush);

        var result = _handler.UsePlanet(_controller, planet, out _);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.PokerHandLevels[PokerHandType.Flush], Is.EqualTo(initialLevel + 1));
            Assert.That(joker.Edition, Is.EqualTo(initialSnapshot.Edition));
            Assert.That(joker.ChipsValue, Is.EqualTo(initialSnapshot.ChipsValue));
            Assert.That(joker.MultValue, Is.EqualTo(initialSnapshot.MultValue));
            Assert.That(joker.XMultValue, Is.EqualTo(initialSnapshot.XMultValue));
            Assert.That(joker.MoneyValue, Is.EqualTo(initialSnapshot.MoneyValue));
            Assert.That(joker.Price, Is.EqualTo(initialSnapshot.Price));
        });
    }

    [TestCase(SpectralType.Familiar, 2)]
    [TestCase(SpectralType.Grim, 1)]
    [TestCase(SpectralType.Incantation, 3)]
    public void UseSpectral_DestroyAndCreate_ReplacesOneCardWithExpectedCards(
        SpectralType spectralType, int expectedHandDelta)
    {
        var original = new PlayingCard(Suit.Hearts, Rank.King);
        _controller.Hand.Add(original);
        var spectral = new SpectralCard(spectralType.ToString(), 4, spectralType);

        var result = _handler.UseSpectral(_controller, spectral, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Hand, Has.Count.EqualTo(1 + expectedHandDelta));
            Assert.That(_controller.Hand, Does.Not.Contain(original));
            Assert.That(_controller.Hand.All(card => card.Enhancement != EnhancePokerCard.None), Is.True);
            Assert.That(message, Does.Contain(spectralType.ToString()));
        });

        var createdCards = _controller.Hand;
        switch (spectralType)
        {
            case SpectralType.Familiar:
                Assert.That(createdCards.All(card => card.Rank is Rank.Jack or Rank.Queen or Rank.King), Is.True);
                break;
            case SpectralType.Grim:
                Assert.That(createdCards.All(card => card.Rank == Rank.Ace), Is.True);
                break;
            case SpectralType.Incantation:
                Assert.That(createdCards.All(card => card.Rank >= Rank.Two && card.Rank <= Rank.Ten), Is.True);
                break;
        }
    }

    [TestCase(SpectralType.Familiar)]
    [TestCase(SpectralType.Grim)]
    [TestCase(SpectralType.Incantation)]
    public void UseSpectral_DestroyAndCreateWithEmptyHand_ReturnsFalse(SpectralType spectralType)
    {
        var spectral = new SpectralCard(spectralType.ToString(), 4, spectralType);

        var result = _handler.UseSpectral(_controller, spectral, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("Hand is empty!"));
            Assert.That(_controller.Hand, Is.Empty);
        });
    }

    [Test]
    public void UseSpectral_WraithWithFreeSlot_AddsRareJokerAndResetsMoney()
    {
        _controller.Money = 25;
        var wraith = new SpectralCard("Wraith", 4, SpectralType.Wraith);

        var result = _handler.UseSpectral(_controller, wraith, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Deck.JokerCards, Has.Count.EqualTo(1));
            Assert.That(_controller.Deck.JokerCards[0].Rarity, Is.EqualTo(JokerRarity.Rare));
            Assert.That(_controller.Money, Is.EqualTo(0));
            Assert.That(message, Is.EqualTo(
                $"Wraith summoned Rare Joker {_controller.Deck.JokerCards[0].Name} and set money to $0!"));
        });
    }

    [Test]
    public void UseSpectral_WraithWhenJokerSlotsFull_ReturnsFalse()
    {
        _controller.Money = 25;
        for (var index = 0; index < _controller.Deck.MaxJokerContainer; index++)
        {
            _controller.Deck.JokerCards.Add(new JokerCard(
                $"Joker {index}", JokerEdition.Base, JokerRarity.Common,
                JokerModifierType.AdditionMultiplier, 0, 4));
        }
        var wraith = new SpectralCard("Wraith", 4, SpectralType.Wraith);

        var result = _handler.UseSpectral(_controller, wraith, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("Joker slots are full!"));
            Assert.That(_controller.Money, Is.EqualTo(25));
            Assert.That(_controller.Deck.JokerCards, Has.Count.EqualTo(_controller.Deck.MaxJokerContainer));
        });
    }

    [Test]
    public void UseSpectral_Sigil_ConvertsEntireHandToOneSuit()
    {
        var cards = new[]
        {
            new PlayingCard(Suit.Hearts, Rank.Ace, EnhancePokerCard.GoldCards),
            new PlayingCard(Suit.Spades, Rank.Seven, EnhancePokerCard.GlassCards),
            new PlayingCard(Suit.Clubs, Rank.Queen, EnhancePokerCard.BonusCards)
        };
        var originalRanks = cards.Select(card => card.Rank).ToArray();
        var originalEnhancements = cards.Select(card => card.Enhancement).ToArray();
        _controller.Hand.AddRange(cards);
        var sigil = new SpectralCard("Sigil", 4, SpectralType.Sigil);

        var result = _handler.UseSpectral(_controller, sigil, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_controller.Hand.Select(card => card.Suit).Distinct().Count(), Is.EqualTo(1));
            Assert.That(_controller.Hand.Select(card => card.Rank), Is.EqualTo(originalRanks));
            Assert.That(_controller.Hand.Select(card => card.Enhancement), Is.EqualTo(originalEnhancements));
            Assert.That(message, Does.StartWith("Sigil converted all cards in hand to "));
        });
    }

    [Test]
    public void UseSpectral_SigilWithEmptyHand_ReturnsFalse()
    {
        var sigil = new SpectralCard("Sigil", 4, SpectralType.Sigil);

        var result = _handler.UseSpectral(_controller, sigil, out var message);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(message, Is.EqualTo("Hand is empty!"));
        });
    }
}
