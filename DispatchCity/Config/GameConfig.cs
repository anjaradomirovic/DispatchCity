namespace DispatchCity.Config;

// Svi parametri igre na jednom mestu. Nivo je kasnije samo jedan skup ovih vrednosti.
public class GameConfig
{
    public float WorldWidth { get; set; } = 1000f;
    public float WorldHeight { get; set; } = 800f;
    public int Seed { get; set; } = 12345;

    // Širina kontrolnog panela kao razmer širine prozora (ostatak je mapa).
    public float PanelWidthRatio { get; set; } = 0.26f;

    // Razmeri svih veličina u odnosu na Spacing (širine ulica, brzine, poluprečnici...).
    public WorldMetrics Metrics
    {
        get { return new WorldMetrics(Spacing); }
    }

    // Pojas okruženja oko mape (kasnije: livade, krave...). Kamera ne izlazi izvan njega.
    public float WorldMargin
    {
        get { return Metrics.WorldMargin; }
    }

    // Najmanje rastojanje između raskrsnica (manje = gušći grad).
    public float Spacing { get; set; } = 75f;

    // Verovatnoća da se odbačena ulica vrati kao petlja.
    public float ExtraEdgeChance { get; set; } = 0.35f;

    // Deo sporednih ulica koji postaje jednosmeran (plan: 15-20%).
    public float OneWayFraction { get; set; } = 0.18f;

    // Pozivi. Vremena su u sekundama igre.
    public float FirstCallDelaySeconds { get; set; } = 2f;
    public float CallIntervalSeconds { get; set; } = 8f;      // prosečan razmak između poziva
    public int MaxActiveCalls { get; set; } = 6;

    // Za koliko sekundi bez pomoći pacijent padne sa pune na nulu zdravlja.
    public float HighPrioritySurvivalSeconds { get; set; } = 60f;
    public float MediumPrioritySurvivalSeconds { get; set; } = 120f;
    public float LowPrioritySurvivalSeconds { get; set; } = 240f;

    // Verovatnoće (zbir visokog i srednjeg je manji od 1, ostalo je nizak prioritet).
    public float HighPriorityChance { get; set; } = 0.2f;
    public float MediumPriorityChance { get; set; } = 0.3f;
    public float HeavyInjuryChance { get; set; } = 0.4f;

    // Zadržavanje vozila na licu mesta (sekunde igre).
    public float LightSceneSeconds { get; set; } = 4f;
    public float HeavyLoadSeconds { get; set; } = 3f;

    public static GameConfig Level1()
    {
        return new GameConfig();
    }
}