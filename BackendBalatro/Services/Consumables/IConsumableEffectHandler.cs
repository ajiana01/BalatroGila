using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Core;

namespace BackendBalatro.Services.Consumables;

public interface IConsumableEffectHandler
{
    bool UseTarot(GameController controller, TarotCard tarot, List<string> targetCardIds, out string message);
    bool UsePlanet(GameController controller, PlanetCard planet, out string message);
    bool UseSpectral(GameController controller, SpectralCard spectral, out string message);
}
