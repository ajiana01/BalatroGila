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
}
