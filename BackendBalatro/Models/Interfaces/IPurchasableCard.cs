namespace BackendBalatro.Models.Interfaces;

public interface IPurchasableCard
{
    string Id { get; set; }
    string Name { get; set; }
    int Price { get; set; }
}
