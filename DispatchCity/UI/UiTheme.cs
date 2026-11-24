using ImGuiNET;

namespace DispatchCity.UI;

// Izgled panela: svetla tema sa zaobljenim ivicama. Zaobljenost i razmaci su razmeri
// veličine slova, pa se skaliraju zajedno sa tekstom.
public static class UiTheme
{
    private const float WindowRoundingRatio = 0.6f;
    private const float WidgetRoundingRatio = 0.4f;
    private const float PaddingRatio = 0.7f;

    // Kartice poziva (pastelne, tople boje).
    public static System.Numerics.Vector4 Card
    {
        get { return FromHex(0xF3EEE2); }
    }

    public static System.Numerics.Vector4 CardHovered
    {
        get { return FromHex(0xF8E9D2); }
    }

    public static System.Numerics.Vector4 CardSelected
    {
        get { return FromHex(0xFBD9AE); }
    }

    // Veličina ugrađenog ImGui fonta (u pikselima). Sve ostalo se računa iz nje.
    private const float DefaultFontSize = 13f;

    // scale = koliko je prozor veći od početnog (1 = početna veličina). Poziva se pri pokretanju
    // i pri svakoj promeni veličine prozora, pa se tekst, razmaci i zaobljenost skaliraju zajedno.
    public static void Apply(float scale)
    {
        ImGui.StyleColorsLight();

        ImGuiStylePtr style = ImGui.GetStyle();
        float font = DefaultFontSize * scale;

        style.WindowRounding = font * WindowRoundingRatio;
        style.FrameRounding = font * WidgetRoundingRatio;
        style.ChildRounding = font * WidgetRoundingRatio;
        style.GrabRounding = font * WidgetRoundingRatio;
        style.ScrollbarRounding = font * WidgetRoundingRatio;
        style.WindowPadding = new System.Numerics.Vector2(font * PaddingRatio, font * PaddingRatio);
        style.FramePadding = new System.Numerics.Vector2(font * 0.35f, font * 0.25f);
        style.ItemSpacing = new System.Numerics.Vector2(font * 0.5f, font * 0.5f);
        style.ScrollbarSize = font * 0.9f;
        style.GrabMinSize = font * 0.8f;
        style.TabRounding = font * WidgetRoundingRatio;

        // Boje panela: kremasta podloga, bordo i zuta (iste kao na uvodnom ekranu).
        var colors = style.Colors;
        colors[(int)ImGuiCol.WindowBg] = FromHex(0xF7F1E3);
        colors[(int)ImGuiCol.Text] = FromHex(0x3A2A2E);
        colors[(int)ImGuiCol.Button] = FromHex(0xF2C230);
        colors[(int)ImGuiCol.ButtonHovered] = FromHex(0xF8D65A);
        colors[(int)ImGuiCol.ButtonActive] = FromHex(0xE0AE1A);
        colors[(int)ImGuiCol.Header] = FromHex(0xE9D9B8);
        colors[(int)ImGuiCol.HeaderHovered] = FromHex(0xF2C230);
        colors[(int)ImGuiCol.HeaderActive] = FromHex(0xE0AE1A);
        colors[(int)ImGuiCol.CheckMark] = FromHex(0x6D1A2E);
        colors[(int)ImGuiCol.FrameBg] = FromHex(0xEADFC8);
        colors[(int)ImGuiCol.Tab] = FromHex(0xE9D9B8);
        colors[(int)ImGuiCol.TabHovered] = FromHex(0xF8D65A);
        colors[(int)ImGuiCol.TabSelected] = FromHex(0xF2C230);
    }

    public static System.Numerics.Vector4 FromHex(int hex)
    {
        float r = ((hex >> 16) & 0xFF) / 255f;
        float g = ((hex >> 8) & 0xFF) / 255f;
        float b = (hex & 0xFF) / 255f;
        return new System.Numerics.Vector4(r, g, b, 1f);
    }
}