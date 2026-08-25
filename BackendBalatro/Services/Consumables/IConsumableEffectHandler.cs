using BackendBalatro.Models.Entities;
using BackendBalatro.Services.Core;

namespace BackendBalatro.Services.Consumables;

public interface IConsumableEffectHandler
{
    bool UseTarot(GameEngine engine, TarotCard tarot, List<string> targetCardIds, out string message);
    bool UsePlanet(GameEngine engine, PlanetCard planet, out string message);
    bool UseSpectral(GameEngine engine, SpectralCard spectral, out string message);
}
