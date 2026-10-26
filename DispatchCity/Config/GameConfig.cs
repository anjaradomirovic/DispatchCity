namespace DispatchCity.Config;

// Svi parametri igre na jednom mestu. Nivo je kasnije samo jedan skup ovih vrednosti.
public class GameConfig
{
    public float WorldWidth { get; set; } = 1000f;
    public float WorldHeight { get; set; } = 800f;
    public int Seed { get; set; } = 12345;

    // Najmanje rastojanje između raskrsnica (manje = gušći grad).
    public float Spacing { get; set; } = 75f;

    // Verovatnoća da se odbačena ulica vrati kao petlja.
    public float ExtraEdgeChance { get; set; } = 0.35f;

    // Deo sporednih ulica koji postaje jednosmeran (plan: 15-20%).
    public float OneWayFraction { get; set; } = 0.18f;

    public static GameConfig Level1()
    {
        return new GameConfig();
    }
}