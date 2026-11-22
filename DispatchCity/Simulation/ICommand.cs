namespace DispatchCity.Simulation;

// Command obrazac: radnja igre kao objekat. Isti objekat pravi i klik igrača (ručno)
// i strategija raspoređivanja (automatski), pa se pravilo "može li se ovo uraditi" piše jednom.
public interface ICommand
{
    bool CanExecute();
    void Execute();
}