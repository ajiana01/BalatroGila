namespace BackendBalatro.Models.Interfaces;

public interface IUsableCard : IPurchasableCard
{
    string Description { get; set; }
}
