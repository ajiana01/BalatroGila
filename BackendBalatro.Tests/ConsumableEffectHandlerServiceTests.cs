/*
 * ConsumableEffectHandlerServiceTests.cs - Unit Tests for Consumable Effects
 *
 * This fixture documents the behavior of Tarot, Planet, and Spectral cards.
 * It uses a real ConsumableEffectHandler with an isolated GameController so
 * each test exercises an effect while keeping scoring and shop dependencies
 * outside the scenario.
 *
 * Key testing practices demonstrated:
 * - Arrange-Act-Assert (AAA)
 * - Parameterized tests for related card effects and invalid inputs
 * - Inventory-capacity and target-count boundary cases
 * - Assertions for state changes, messages, and preserved state on failure
 *
 */

using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackendBalatro.Tests;

/// <summary>
/// Test fixture for <see cref="ConsumableEffectHandler"/>.
///
/// The fixture creates a fresh handler and controller for each test so card
/// inventory, hand state, money, and poker-hand levels remain isolated.
/// </summary>
[TestFixture]
public class ConsumableEffectHandlerServiceTests
{
    // System under test: applies Tarot, Planet, and Spectral card effects.
    private ConsumableEffectHandler _handler;

    // Isolated game state used as the target of each consumable effect.
    private GameController _controller;

    /// <summary>
    /// Creates fresh instances before each test to prevent state changes from
    /// one card effect from leaking into another scenario.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _handler = new ConsumableEffectHandler(NullLogger<ConsumableEffectHandler>.Instance);
        _controller = CreateController();
    }

    /// <summary>
    /// Creates an isolated controller with mocked scoring and shop services,
    /// because these tests focus only on consumable behavior.
    /// </summary>
    private GameController CreateController()
    {
        return new GameController(
            new Mock<IScoringService>().Object,
            new Mock<IShopService>().Object,
            _handler,
            NullLogger<GameController>.Instance);
    }

    /// <summary>
    /// Verifies that The Fool creates an independent copy of the most recently
    /// used Tarot card when inventory capacity is available.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Fool copies the most recently used Planet card when
    /// no prior Tarot card is available.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Fool fails without a previously used Tarot or Planet
    /// card and does not change the consumable inventory.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Fool cannot create a copy when the consumable
    /// inventory is already full.
    /// </summary>
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

    /// <summary>
    /// Verifies that The High Priestess fills available consumable capacity
    /// with up to two generated Planet cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that The High Priestess fails without changing inventory when
    /// no consumable slots remain.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Emperor fills available consumable capacity with up
    /// to two generated Tarot cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Emperor fails without changing inventory when no
    /// consumable slots remain.
    /// </summary>
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

    /// <summary>
    /// Verifies that Tarot cards allowing one or two targets apply their
    /// expected enhancement only to the selected cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that one-or-two-target enhancement Tarot cards reject zero or
    /// three targets without mutating any selected card.
    /// </summary>
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

    /// <summary>
    /// Verifies that single-target enhancement Tarot cards apply the correct
    /// enhancement to exactly one selected card.
    /// </summary>
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

    /// <summary>
    /// Verifies that single-target enhancement Tarot cards reject both missing
    /// and excessive target selections without changing card state.
    /// </summary>
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

    /// <summary>
    /// Verifies that suit-conversion Tarot cards change one to three selected
    /// cards to their designated suit while preserving other card properties.
    /// </summary>
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

    /// <summary>
    /// Verifies that suit-conversion Tarot cards reject zero or more than three
    /// targets without changing any suits.
    /// </summary>
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

    /// <summary>
    /// Verifies that Strength advances selected card ranks and recalculates
    /// their default base-chip values.
    /// </summary>
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

    /// <summary>
    /// Verifies that Strength wraps an Ace to a Two and assigns the correct
    /// base-chip value.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Hanged Man permanently removes one or two selected
    /// cards from the hand while preserving unselected cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Hanged Man rejects zero or more than two targets and
    /// leaves the hand unchanged.
    /// </summary>
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

    /// <summary>
    /// Verifies that Death copies the gameplay properties of the right card to
    /// the left card while retaining the left card's identity.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Hermit doubles money while limiting the gain to
    /// twenty dollars.
    /// </summary>
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

    /// <summary>
    /// Verifies that Temperance grants the combined sell value of Jokers while
    /// limiting the gain to fifty dollars and preserving the Joker collection.
    /// </summary>
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

    /// <summary>
    /// Verifies that The Wheel of Fortune fails when no Joker is available for
    /// an edition upgrade.
    /// </summary>
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

    /// <summary>
    /// Verifies that a successful Wheel of Fortune roll upgrades a base Joker
    /// to a special edition and reports the resulting edition.
    /// </summary>
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

    /// <summary>
    /// Verifies that a Wheel of Fortune miss succeeds without changing a Joker
    /// that already has a special edition.
    /// </summary>
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

    /// <summary>
    /// Verifies that Judgement creates a random Joker when a Joker slot is
    /// available.
    /// </summary>
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

    /// <summary>
    /// Verifies that Judgement fails and preserves the Joker collection when
    /// all Joker slots are occupied.
    /// </summary>
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

    /// <summary>
    /// Verifies that a Planet card increases the existing level of its poker
    /// hand and returns the new level in the message.
    /// </summary>
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

    /// <summary>
    /// Verifies that using a Planet card initializes a missing poker-hand
    /// level at level two.
    /// </summary>
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

    /// <summary>
    /// Verifies that a Planet card also increases Constellation's XMult value
    /// by 0.1 while upgrading the corresponding poker hand.
    /// </summary>
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

    /// <summary>
    /// Verifies that using a Planet card without Constellation does not mutate
    /// unrelated Joker properties.
    /// </summary>
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

    /// <summary>
    /// Verifies that Familiar, Grim, and Incantation destroy one hand card and
    /// replace it with the expected number and rank range of enhanced cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that destroy-and-create Spectral cards fail on an empty hand
    /// without adding any cards.
    /// </summary>
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

    /// <summary>
    /// Verifies that Wraith adds a rare Joker when capacity is available and
    /// resets the controller's money to zero.
    /// </summary>
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

    /// <summary>
    /// Verifies that Wraith fails with full Joker slots while preserving money
    /// and the existing Joker collection.
    /// </summary>
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

    /// <summary>
    /// Verifies that Sigil converts every card in the hand to one suit while
    /// preserving each card's rank and enhancement.
    /// </summary>
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

    /// <summary>
    /// Verifies that Sigil fails when the hand is empty.
    /// </summary>
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
