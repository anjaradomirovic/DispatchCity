using DispatchCity.Simulation;

namespace DispatchCity.Simulation.State;

// State obrazac: ponašanje vozila zavisi od stanja u kome je, a svako stanje
// zna samo svoje pravilo i u koje stanje sledeće prelazi.
public interface IAmbulanceState
{
    string Name { get; }

    // Poziva se jednom, kad vozilo uđe u stanje.
    void Enter(Ambulance ambulance);

    // Poziva se svaki frame dok je vozilo u stanju.
    void Update(Ambulance ambulance, float deltaSeconds);
}