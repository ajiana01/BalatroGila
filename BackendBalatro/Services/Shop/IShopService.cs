using BackendBalatro.Models.Entities;

namespace BackendBalatro.Services.Shop;

public interface IShopService
{
    void PopulateShop(BackendBalatro.Models.Entities.Shop shop, int ante, List<Voucher> purchasedVouchers);
    void RerollShop(BackendBalatro.Models.Entities.Shop shop, int ante, List<Voucher> purchasedVouchers);
    BoosterPack OpenBoosterPack(BoosterPack pack);
}
