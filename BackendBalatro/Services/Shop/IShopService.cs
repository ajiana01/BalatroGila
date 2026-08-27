using BackendBalatro.Enums;
using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Shop;

public interface IShopService
{
    void PopulateShop(
        BackendBalatro.Models.Entities.Shop shop,
        int ante,
        List<Voucher> purchasedVouchers,
        Voucher? currentAnteVoucher = null,
        bool isAnteVoucherPurchased = false);

    void RerollShop(BackendBalatro.Models.Entities.Shop shop, int ante, List<Voucher> purchasedVouchers);
    BoosterPack OpenBoosterPack(BoosterPack pack, List<Voucher>? purchasedVouchers = null, PokerHandType mostPlayedHand = PokerHandType.HighCard);
    Voucher? GenerateVoucherForAnte(int ante, List<Voucher> purchasedVouchers);
}
