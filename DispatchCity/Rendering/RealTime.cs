using System.Diagnostics;

namespace DispatchCity.Rendering;

// Jedan sat za sve što teče u pravom vremenu (animacije, efekti), pa i kad je igra pauzirana.
// Ranije su tri klase imale svoju štopericu i svoj izračun razmaka između frejmova.
public static class RealTime
{
	// Posle zastoja (npr. pomeranje prozora) razmak se ograničava, da animacije ne skoče.
	private const float MaxDeltaSeconds = 0.1f;

	private static readonly Stopwatch Clock = Stopwatch.StartNew();
	private static float _lastFrameSeconds;

	// Sekunde od pokretanja programa.
	public static float Seconds
	{
		get { return (float)Clock.Elapsed.TotalSeconds; }
	}

	// Razmak između ovog i prošlog frejma (dt iz termina 4).
	public static float Delta { get; private set; }

	// Poziva se jednom na početku crtanja svakog frejma.
	public static void BeginFrame()
	{
		float now = Seconds;
		Delta = MathF.Min(now - _lastFrameSeconds, MaxDeltaSeconds);
		_lastFrameSeconds = now;
	}
}