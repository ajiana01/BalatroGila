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
    private readonly GameController _controller;

    public ShopAndEconomyTests()
    {
        var evaluator = new PokerHandEvaluator();
        var scoring = new ScoringService(evaluator);
        var shopService = new ShopService();
        var consumableHandler = new ConsumableEffectHandler();
        _controller = new GameController(scoring, shopService, consumableHandler);
    }

    [Fact]
    public void Shop_BuyJokerCard_DeductsMoneyAndAddsToJokers()
    {
        _controller.StartGame();
        _controller.Money = 20;

        // Force into shop
        _controller.SelectBlind(1);
        _controller.DefeatBlind();
        Assert.Equal(GameStatePhase.InShop, _controller.Phase);

        // Ensure there is a joker offer
        var joker = new JokerCard("Test Joker", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 10, 4);
        _controller.Shop.JokerCardOffers.Add(joker);

        int initialMoney = _controller.Money;
        var (buySuccess, _) = _controller.BuyCardFromShop(joker.Id);

        Assert.True(buySuccess);
        Assert.Equal(initialMoney - 4, _controller.Money);
        Assert.Contains(_controller.Deck.JokerCards, j => j.Id == joker.Id);
    }

    [Fact]
    public void GoldenJoker_Adds4DollarsAtEndOfRound()
    {
        _controller.StartGame();
        _controller.Money = 0;
        _controller.Deck.JokerCards.Add(new JokerCard(
            JokerId.GoldenJoker,
            "Golden Joker",
            JokerEdition.Base,
            JokerRarity.Common,
            JokerModifierType.Money,
            4,
            6));

        Assert.True(_controller.SelectBlind(1));
        var blind = _controller.CurrentBlind!;
        int expectedCashoutWithoutGoldenJoker = blind.RewardMoney + _controller.HandsRemaining;

        Assert.True(_controller.DefeatBlind());

        Assert.Equal(expectedCashoutWithoutGoldenJoker + 4, _controller.Money);
    }

    [Fact]
    public void Shop_Reroll_IncreasesRerollCost()
    {
        _controller.StartGame();
        _controller.Money = 50;

        _controller.SelectBlind(1);
        _controller.DefeatBlind();

        int initialCost = _controller.Shop.RerollCost;
        Assert.Equal(5, initialCost);

        var (rerollSuccess, _) = _controller.RerollShop();
        Assert.True(rerollSuccess);
        Assert.Equal(6, _controller.Shop.RerollCost);

        var (reroll2, _) = _controller.RerollShop();
        Assert.True(reroll2);
        Assert.Equal(7, _controller.Shop.RerollCost);
    }

    [Fact]
    public void Shop_VoucherPurchase_AppliesPermanentEffect()
    {
        _controller.StartGame();
        _controller.Money = 30;

        _controller.SelectBlind(1);
        _controller.DefeatBlind();

        var voucher = new Voucher("Grabber", VoucherEffect.Grabber, 10);
        _controller.Shop.Voucher = voucher;

        int initialMaxHands = _controller.MaxHands;
        var (success, _) = _controller.BuyVoucher(voucher.Id);

        Assert.True(success);
        Assert.Equal(initialMaxHands + 1, _controller.MaxHands);
        Assert.Null(_controller.Shop.Voucher);
        Assert.Contains(_controller.PurchasedVouchers, v => v.Id == voucher.Id);
    }

    [Fact]
    public void Shop_MegaBoosterPack_AllowsPickingTwoCards()
    {
        _controller.StartGame();
        _controller.Money = 30;

        _controller.SelectBlind(1);
        _controller.DefeatBlind();
        Assert.Equal(GameStatePhase.InShop, _controller.Phase);

        var pack = new BoosterPack("Mega Arcana Pack", 8, 2, 5, BoosterType.Arcana, PackSize.Mega);
        var tarot1 = new TarotCard("The Fool", 0, TarotType.TheFool);
        var tarot2 = new TarotCard("The Magician", 0, TarotType.TheMagician);
        var tarot3 = new TarotCard("The High Priestess", 0, TarotType.TheHighPriestess);
        pack.TarotCards.AddRange(new[] { tarot1, tarot2, tarot3 });
        _controller.Shop.BoosterPacks.Add(pack);

        var (buySuccess, _, openedPack) = _controller.BuyBoosterPack(pack.Id);
        Assert.True(buySuccess);
        Assert.NotNull(_controller.Shop.OpenedBoosterPack);
        Assert.Equal(2, _controller.Shop.OpenedBoosterPack.MaxPick);
        Assert.True(openedPack!.TarotCards.Count >= 2);

        var firstTarotId = openedPack.TarotCards[0].Id;
        var secondTarotId = openedPack.TarotCards[1].Id;

        // Pick card 1
        var (pick1Success, _) = _controller.SelectBoosterCard(firstTarotId);
        Assert.True(pick1Success);
        Assert.Contains(_controller.Deck.UsableCards, c => c.Id == firstTarotId);
        Assert.NotNull(_controller.Shop.OpenedBoosterPack); // Still open!
        Assert.Equal(1, _controller.Shop.OpenedBoosterPack.MaxPick);
        Assert.DoesNotContain(_controller.Shop.OpenedBoosterPack.TarotCards, c => c.Id == firstTarotId);

        // Pick card 2
        var (pick2Success, _) = _controller.SelectBoosterCard(secondTarotId);
        Assert.True(pick2Success);
        Assert.Contains(_controller.Deck.UsableCards, c => c.Id == secondTarotId);
        Assert.Null(_controller.Shop.OpenedBoosterPack); // Closed after 2 picks!
    }

    [Fact]
    public void Shop_SkipBoosterPack_ClosesOpenedPack()
    {
        _controller.StartGame();
        _controller.Money = 30;

        _controller.SelectBlind(1);
        _controller.DefeatBlind();

        var pack = new BoosterPack("Mega Buffoon Pack", 8, 2, 4, BoosterType.Buffoon, PackSize.Mega);
        var joker1 = new JokerCard("Joker 1", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 10, 0);
        pack.JokerCards.Add(joker1);
        _controller.Shop.BoosterPacks.Add(pack);

        _controller.BuyBoosterPack(pack.Id);
        Assert.NotNull(_controller.Shop.OpenedBoosterPack);

        var (skipSuccess, _) = _controller.SkipBoosterPack();
        Assert.True(skipSuccess);
        Assert.Null(_controller.Shop.OpenedBoosterPack);
    }

    [Fact]
    public void JokersAndConsumables_Arrange_ReordersSuccessfully()
    {
        _controller.StartGame();

        var j1 = new JokerCard("Joker A", JokerEdition.Base, JokerRarity.Common, JokerModifierType.Chips, 10, 0);
        var j2 = new JokerCard("Joker B", JokerEdition.Base, JokerRarity.Common, JokerModifierType.AdditionMultiplier, 4, 0);
        _controller.Deck.JokerCards.Add(j1);
        _controller.Deck.JokerCards.Add(j2);

        var (jokerArrangeSuccess, _) = _controller.ArrangeJokers(new List<string> { j2.Id, j1.Id });
        Assert.True(jokerArrangeSuccess);
        Assert.Equal(j2.Id, _controller.Deck.JokerCards[0].Id);
        Assert.Equal(j1.Id, _controller.Deck.JokerCards[1].Id);

        var c1 = new TarotCard("Tarot 1", 0, TarotType.TheFool);
        var c2 = PlanetCard.CreateForHand(PokerHandType.Pair);
        _controller.Deck.UsableCards.Add(c1);
        _controller.Deck.UsableCards.Add(c2);

        var (consumableArrangeSuccess, _) = _controller.ArrangeConsumables(new List<string> { c2.Id, c1.Id });
        Assert.True(consumableArrangeSuccess);
        Assert.Equal(c2.Id, _controller.Deck.UsableCards[0].Id);
        Assert.Equal(c1.Id, _controller.Deck.UsableCards[1].Id);
    }

    [Fact]
    public void Voucher_PersistsAcrossRoundsInSameAnte_WhenNotPurchased()
    {
        _controller.StartGame();
        _controller.Money = 50;

        // Round 1 (Small Blind)
        _controller.SelectBlind(1);
        _controller.DefeatBlind();
        Assert.Equal(GameStatePhase.InShop, _controller.Phase);
        Assert.NotNull(_controller.Shop.Voucher);
        var initialVoucherId = _controller.Shop.Voucher.Id;
        var initialVoucherEffect = _controller.Shop.Voucher.Effect;

        // Leave shop and defeat Round 2 (Big Blind)
        _controller.LeaveShop();
        _controller.SelectBlind(2);
        _controller.DefeatBlind();
        Assert.Equal(GameStatePhase.InShop, _controller.Phase);

        // Voucher should be the EXACT same voucher
        Assert.NotNull(_controller.Shop.Voucher);
        Assert.Equal(initialVoucherId, _controller.Shop.Voucher.Id);
        Assert.Equal(initialVoucherEffect, _controller.Shop.Voucher.Effect);
    }

    [Fact]
    public void Voucher_DoesNotAppearInSameAnte_AfterBeingPurchased()
    {
        _controller.StartGame();
        _controller.Money = 50;

        // Round 1 (Small Blind)
        _controller.SelectBlind(1);
        _controller.DefeatBlind();
        Assert.NotNull(_controller.Shop.Voucher);

        // Buy the voucher
        var voucherId = _controller.Shop.Voucher.Id;
        var (buySuccess, _) = _controller.BuyVoucher(voucherId);
        Assert.True(buySuccess);
        Assert.Null(_controller.Shop.Voucher);

        // Leave shop and defeat Round 2 (Big Blind)
        _controller.LeaveShop();
        _controller.SelectBlind(2);
        _controller.DefeatBlind();

        // Shop voucher must be null in subsequent rounds of same Ante!
        Assert.Null(_controller.Shop.Voucher);
    }

    [Fact]
    public void Voucher_ChangesWhenAnteAdvances()
    {
        _controller.StartGame();
        _controller.Money = 100;

        // Round 1 (Small Blind) Ante 1
        _controller.SelectBlind(1);
        _controller.DefeatBlind();
        var ante1VoucherId = _controller.Shop.Voucher!.Id;

        // Defeat Big Blind & Boss Blind to advance Ante
        _controller.LeaveShop();
        _controller.SelectBlind(2);
        _controller.DefeatBlind();

        _controller.LeaveShop();
        _controller.SelectBlind(3);
        _controller.DefeatBlind();

        // Leave shop after boss -> advances to Ante 2
        _controller.LeaveShop();
        Assert.Equal(2, _controller.CurrentAnte);

        // Start Round 1 Ante 2
        _controller.SelectBlind(1);
        _controller.DefeatBlind();

        // New Ante must have a newly generated voucher
        Assert.NotNull(_controller.Shop.Voucher);
        Assert.NotEqual(ante1VoucherId, _controller.Shop.Voucher.Id);
    }

    [Fact]
    public void Voucher_SeedMoney_IncreasesInterestCapTo10()
    {
        _controller.StartGame();
        _controller.Money = 50; // $50 => default interest is $5 (cap 5)

        _controller.SelectBlind(1);
        _controller.DefeatBlind();

        var seedMoneyVoucher = new Voucher("Seed Money", VoucherEffect.SeedMoney, 10);
        _controller.Shop.Voucher = seedMoneyVoucher;
        _controller.BuyVoucher(seedMoneyVoucher.Id);

        _controller.Money = 50; // Set money to $50 again
        int cashout = _controller.Cashout(); // Base reward 3 + remaining hands + $10 interest (since $50/5 = 10)
        // Check interest cap: with Seed Money, 50 / 5 = 10 interest
        Assert.True(cashout >= 13);
    }

    [Fact]
    public void Voucher_DirectorsCut_AllowsOneRerollPerAnteFor10Dollars()
    {
        _controller.StartGame();
        _controller.Money = 50;

        _controller.SelectBlind(1);
        _controller.DefeatBlind();

        // Cannot reroll without Director's Cut
        var (failNoVoucher, _) = _controller.RerollBossBlind();
        Assert.False(failNoVoucher);

        // Buy Director's Cut
        var directorsCut = new Voucher("Director's Cut", VoucherEffect.DirectorsCut, 10);
        _controller.Shop.Voucher = directorsCut;
        _controller.BuyVoucher(directorsCut.Id);

        var initialBoss = _controller.GetAvailableBlinds().First(b => b.BlindType == BlindType.Boss);
        var initialMoney = _controller.Money;

        // Reroll Boss Blind
        var (rerollSuccess, _) = _controller.RerollBossBlind();
        Assert.True(rerollSuccess);
        Assert.Equal(initialMoney - 10, _controller.Money);

        // Cannot reroll twice in same Ante
        var (secondReroll, _) = _controller.RerollBossBlind();
        Assert.False(secondReroll);
    }
}
