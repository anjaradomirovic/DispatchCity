namespace DispatchCity.Rendering;

// Pacijenti: slike i mreža sličica u njima.
public record PatientSet(List<Texture2D> Textures, int Columns, int Rows);

// Učitavanje slika iz foldera assets/. Ako slike nema, uzima se privremeni crtež iz koda,
// pa igra radi i bez njih. Ranije su igra i meni imali svaki svoj kod za ovo.
public static class SpriteAssets
{
    private static string Folder
    {
        get { return Path.Combine(AppContext.BaseDirectory, "assets"); }
    }

    // patient_1.png, patient_2.png... dok ima fajlova. Tvoji crteži imaju jednu sličicu po liku
    // (mreža 1 x 1), pa se pokret dodaje iz koda. Privremeni crtež je atlas od 4 x 2 sličice
    // (red 0 = stabilan pacijent, red 1 = kritičan).
    public static PatientSet LoadPatients()
    {
        var textures = new List<Texture2D>();
        for (int i = 1; ; i++)
        {
            Texture2D? texture = Texture2D.FromFile(Path.Combine(Folder, "patient_" + i + ".png"));
            if (texture == null)
            {
                break;
            }
            textures.Add(texture);
        }

        if (textures.Count == 0)
        {
            textures.Add(PlaceholderSprites.CreateGrandmaAtlas());
            return new PatientSet(textures, 4, 2);
        }

        return new PatientSet(textures, 1, 1);
    }

    public static Texture2D LoadAmbulance()
    {
        return Texture2D.FromFile(Path.Combine(Folder, "ambulance.png")) ?? PlaceholderSprites.CreateAmbulance();
    }

    public static Texture2D LoadTree()
    {
        return Texture2D.FromFile(Path.Combine(Folder, "tree.png")) ?? PlaceholderSprites.CreateTree();
    }
}