using DispatchCity.Rendering;
using ImGuiNET;

namespace DispatchCity.UI;

// Uvodni ekran: naslov i dugme START GAME na bordo podlozi. Razmaci i velicine su razmeri
// velicine slova, pa se skaliraju sa prozorom. Vraca true kad igrac klikne dugme.
public class MenuPanel
{
    private const string Title = "DISPATCH CITY";
    private const string ButtonLabel = "START GAME";

    // Razmeri velicine slova.
    private const float ButtonFontScale = 2.2f;
    private const float TitleFontScale = 4.2f;
    private const float ButtonPaddingX = 1.6f;
    private const float ButtonPaddingY = 0.8f;
    private const float PlatePadding = 0.9f;
    private const float PlateRounding = 0.6f;
    private const float ButtonRounding = 0.4f;
    private const float TitleGap = 1.2f;

    public bool Draw(float width, float height)
    {
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(width, height));

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration
                               | ImGuiWindowFlags.NoBackground
                               | ImGuiWindowFlags.NoMove
                               | ImGuiWindowFlags.NoSavedSettings
                               | ImGuiWindowFlags.NoBringToFrontOnFocus;

        bool clicked = false;
        if (ImGui.Begin("Uvod", flags))
        {
            ImGui.SetWindowFontScale(ButtonFontScale);
            float font = ImGui.GetFontSize();

            System.Numerics.Vector2 text = ImGui.CalcTextSize(ButtonLabel);
            var padding = new System.Numerics.Vector2(font * ButtonPaddingX, font * ButtonPaddingY);
            var buttonSize = new System.Numerics.Vector2(text.X + 2f * padding.X, text.Y + 2f * padding.Y);
            float platePad = font * PlatePadding;
            var plateSize = new System.Numerics.Vector2(buttonSize.X + 2f * platePad, buttonSize.Y + 2f * platePad);

            // Podloga je tacno na sredini ekrana (gde se spajaju nebo i trava).
            var plateMin = new System.Numerics.Vector2((width - plateSize.X) / 2f, (height - plateSize.Y) / 2f);
            var plateMax = plateMin + plateSize;

            ImDrawListPtr draw = ImGui.GetWindowDrawList();
            draw.AddRectFilled(plateMin, plateMax, ColorOf(Palette.MenuPlate), font * PlateRounding);

            DrawTitle(draw, width, plateMin.Y, font);

            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(plateMin.X + platePad, plateMin.Y + platePad));
            ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.FromHex(0xF2C230));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiTheme.FromHex(0xF8D65A));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, UiTheme.FromHex(0xE0AE1A));
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.FromHex(0x5A1426));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, font * ButtonRounding);
            clicked = ImGui.Button(ButtonLabel, buttonSize);
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(4);
        }
        ImGui.End();
        return clicked;
    }

    // Naslov iznad podloge, centriran. Crta se direktno u draw listu, vecim slovima.
    private static void DrawTitle(ImDrawListPtr draw, float width, float plateTop, float font)
    {
        float titleSize = ImGui.GetFontSize() / ButtonFontScale * TitleFontScale;
        System.Numerics.Vector2 size = ImGui.CalcTextSize(Title) * (TitleFontScale / ButtonFontScale);
        var position = new System.Numerics.Vector2((width - size.X) / 2f, plateTop - size.Y - font * TitleGap);

        // Senka ispod, pa naslov, da se cita i preko neba i preko oblaka.
        float shadow = titleSize * 0.05f;
        draw.AddText(ImGui.GetFont(), titleSize, position + new System.Numerics.Vector2(shadow, shadow),
            ColorOf(Palette.MenuPlate), Title);
        draw.AddText(ImGui.GetFont(), titleSize, position, ColorOf(new System.Numerics.Vector3(1f, 1f, 1f)), Title);
    }

    private static uint ColorOf(System.Numerics.Vector3 color)
    {
        return ImGui.GetColorU32(new System.Numerics.Vector4(color.X, color.Y, color.Z, 1f));
    }
}