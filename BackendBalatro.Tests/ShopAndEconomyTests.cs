using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Core;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Shop;
using Xunit;

namespace BackendBalatro.Tests;

public class ShopAndEconomyTests
{
    private readonly GameEngine _engine;

    public ShopAndEconomyTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _engine = new GameEngine(scoring, shopService, consumableHandler);
    }

    [Fact]
    public void Shop_BuyJokerCard_DeductsMoneyAndAddsToJokers()
    {
        _engine.StartGame();
        _engine.Money = 20;

        // Force into shop
        _engine.SelectBlind(1);
        _engine.DefeatBlind();
        Assert.Equal(GameStatePhase.InShop, _engine.Phase);

        // Ensure there is a joker offer
        var joker = new JokerCard("Test Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 10, 4);
        _engine.Shop.JokerCardOffers.Add(joker);

        int initialMoney = _engine.Money;
        var (buySuccess, _) = _engine.BuyCardFromShop(joker.Id);

        Assert.True(buySuccess);
        Assert.Equal(initialMoney - 4, _engine.Money);
        Assert.Contains(_engine.Deck.JokerCards, j => j.Id == joker.Id);
    }

    [Fact]
    public void Shop_Reroll_IncreasesRerollCost()
    {
        _engine.StartGame();
        _engine.Money = 50;

        _engine.SelectBlind(1);
        _engine.DefeatBlind();

        int initialCost = _engine.Shop.RerollCost;
        Assert.Equal(5, initialCost);

        var (rerollSuccess, _) = _engine.RerollShop();
        Assert.True(rerollSuccess);
        Assert.Equal(6, _engine.Shop.RerollCost);

        var (reroll2, _) = _engine.RerollShop();
        Assert.True(reroll2);
        Assert.Equal(7, _engine.Shop.RerollCost);
    }

    [Fact]
    public void Shop_VoucherPurchase_AppliesPermanentEffect()
    {
        _engine.StartGame();
        _engine.Money = 30;

        _engine.SelectBlind(1);
        _engine.DefeatBlind();

        var voucher = new Voucher("Grabber", VoucherEffect.Grabber, 10);
        _engine.Shop.Voucher = voucher;

        int initialMaxHands = _engine.MaxHands;
        var (success, _) = _engine.BuyVoucher(voucher.Id);

        Assert.True(success);
        Assert.Equal(initialMaxHands + 1, _engine.MaxHands);
        Assert.Null(_engine.Shop.Voucher);
        Assert.Contains(_engine.PurchasedVouchers, v => v.Id == voucher.Id);
    }

    [Fact]
    public void Shop_MegaBoosterPack_AllowsPickingTwoCards()
    {
        _engine.StartGame();
        _engine.Money = 30;

        _engine.SelectBlind(1);
        _engine.DefeatBlind();
        Assert.Equal(GameStatePhase.InShop, _engine.Phase);

        var pack = new BoosterPack("Mega Arcana Pack", 8, 2, 5, BoosterType.Arcana, PackSize.Mega);
        var tarot1 = new TarotCard("The Fool", 0, TarotType.TheFool);
        var tarot2 = new TarotCard("The Magician", 0, TarotType.TheMagician);
        var tarot3 = new TarotCard("The High Priestess", 0, TarotType.TheHighPriestess);
        pack.TarotCards.AddRange(new[] { tarot1, tarot2, tarot3 });
        _engine.Shop.BoosterPacks.Add(pack);

        var (buySuccess, _, openedPack) = _engine.BuyBoosterPack(pack.Id);
        Assert.True(buySuccess);
        Assert.NotNull(_engine.Shop.OpenedBoosterPack);
        Assert.Equal(2, _engine.Shop.OpenedBoosterPack.MaxPick);
        Assert.True(openedPack!.TarotCards.Count >= 2);

        var firstTarotId = openedPack.TarotCards[0].Id;
        var secondTarotId = openedPack.TarotCards[1].Id;

        // Pick card 1
        var (pick1Success, _) = _engine.SelectBoosterCard(firstTarotId);
        Assert.True(pick1Success);
        Assert.Contains(_engine.Deck.UsableCards, c => c.Id == firstTarotId);
        Assert.NotNull(_engine.Shop.OpenedBoosterPack); // Still open!
        Assert.Equal(1, _engine.Shop.OpenedBoosterPack.MaxPick);
        Assert.DoesNotContain(_engine.Shop.OpenedBoosterPack.TarotCards, c => c.Id == firstTarotId);

        // Pick card 2
        var (pick2Success, _) = _engine.SelectBoosterCard(secondTarotId);
        Assert.True(pick2Success);
        Assert.Contains(_engine.Deck.UsableCards, c => c.Id == secondTarotId);
        Assert.Null(_engine.Shop.OpenedBoosterPack); // Closed after 2 picks!
    }

    [Fact]
    public void Shop_SkipBoosterPack_ClosesOpenedPack()
    {
        _engine.StartGame();
        _engine.Money = 30;

        _engine.SelectBlind(1);
        _engine.DefeatBlind();

        var pack = new BoosterPack("Mega Buffoon Pack", 8, 2, 4, BoosterType.Buffoon, PackSize.Mega);
        var joker1 = new JokerCard("Joker 1", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 10, 0);
        pack.JokerCards.Add(joker1);
        _engine.Shop.BoosterPacks.Add(pack);

        _engine.BuyBoosterPack(pack.Id);
        Assert.NotNull(_engine.Shop.OpenedBoosterPack);

        var (skipSuccess, _) = _engine.SkipBoosterPack();
        Assert.True(skipSuccess);
        Assert.Null(_engine.Shop.OpenedBoosterPack);
    }

    [Fact]
    public void JokersAndConsumables_Arrange_ReordersSuccessfully()
    {
        _engine.StartGame();

        var j1 = new JokerCard("Joker A", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 10, 0);
        var j2 = new JokerCard("Joker B", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 4, 0);
        _engine.Deck.JokerCards.Add(j1);
        _engine.Deck.JokerCards.Add(j2);

        var (jokerArrangeSuccess, _) = _engine.ArrangeJokers(new List<string> { j2.Id, j1.Id });
        Assert.True(jokerArrangeSuccess);
        Assert.Equal(j2.Id, _engine.Deck.JokerCards[0].Id);
        Assert.Equal(j1.Id, _engine.Deck.JokerCards[1].Id);

        var c1 = new TarotCard("Tarot 1", 0, TarotType.TheFool);
        var c2 = PlanetCard.CreateForHand(PokerHandType.Pair);
        _engine.Deck.UsableCards.Add(c1);
        _engine.Deck.UsableCards.Add(c2);

        var (consumableArrangeSuccess, _) = _engine.ArrangeConsumables(new List<string> { c2.Id, c1.Id });
        Assert.True(consumableArrangeSuccess);
        Assert.Equal(c2.Id, _engine.Deck.UsableCards[0].Id);
        Assert.Equal(c1.Id, _engine.Deck.UsableCards[1].Id);
    }
}
