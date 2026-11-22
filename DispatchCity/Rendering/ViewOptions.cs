namespace DispatchCity.Rendering;

// Podešavanja prikaza (ne pravila igre): panel i tasteri ih menjaju, slojevi ih čitaju.
public class ViewOptions
{
    // Ulice obojene vremenom dolaska vozila iz bolnice.
    public bool ShowHeatMap { get; set; }

    // Ulice obojene gužvom (bledožuta do narandžasta).
    public bool ShowTraffic { get; set; } = true;
}