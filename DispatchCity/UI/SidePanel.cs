using DispatchCity.Rendering;
using DispatchCity.Simulation;
using ImGuiNET;

namespace DispatchCity.UI;

// Kontrolni panel sa strane mape. Samo čita stanje sveta (CPU) i crta ga preko ImGui-ja.
// Pozivi su kartice u horizontalnom nizu (traka zdravlja menja boju kako pacijent slabi),
// a klik na karticu bira poziv. Tekst je bez dijakritika jer ih osnovni ImGui font nema.
public class SidePanel
{
    // Veličine kartice su razmeri veličine slova.
    private const float CardWidthRatio = 9f;
    private const float CardHeightRatio = 8f;

    // Izabrani poziv (UI stanje, svet za njega ne zna). Biće potreban za ručnu dodelu vozila.
    public int? SelectedCallId { get; private set; }

    public void Draw(GameWorld world, float x, float width, float height)
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(x, 0f));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(width, height));

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoMove
                               | ImGuiWindowFlags.NoResize
                               | ImGuiWindowFlags.NoCollapse
                               | ImGuiWindowFlags.NoTitleBar
                               | ImGuiWindowFlags.NoBringToFrontOnFocus;

        if (ImGui.Begin("Kontrolni panel", flags))
        {
            ImGui.Text("Dispatch City");
            ImGui.Separator();

            DrawCalls(world);
            ImGui.Spacing();
            DrawAmbulances(world);
        }
        ImGui.End();
    }

    private void DrawCalls(GameWorld world)
    {
        ImGui.Text("Pozivi (" + world.Calls.Count + ")");

        // Ako izabrani poziv više ne postoji (spasen ili izgubljen), izbor se poništava.
        if (SelectedCallId != null && !Contains(world, SelectedCallId.Value))
        {
            SelectedCallId = null;
        }

        float font = ImGui.GetFontSize();
        var cardSize = new System.Numerics.Vector2(font * CardWidthRatio, font * CardHeightRatio);

        // Spoljni okvir se pomera horizontalno kad kartica ima više nego što staje.
        var rowSize = new System.Numerics.Vector2(0f, cardSize.Y + font * 1.6f);
        ImGui.BeginChild("Pozivi", rowSize, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        if (world.Calls.Count == 0)
        {
            ImGui.TextDisabled("Nema aktivnih poziva");
        }

        for (int i = 0; i < world.Calls.Count; i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
            }
            DrawCard(world.Calls[i], cardSize);
        }

        ImGui.EndChild();

        if (SelectedCallId != null)
        {
            ImGui.Text("Izabran poziv: #" + SelectedCallId.Value);
        }
    }

    private void DrawCard(EmergencyCall call, System.Numerics.Vector2 size)
    {
        bool selected = SelectedCallId == call.Id;
        System.Numerics.Vector4 background = selected ? UiTheme.CardSelected : UiTheme.Card;

        ImGui.PushStyleColor(ImGuiCol.ChildBg, background);
        ImGui.BeginChild("poziv" + call.Id, size, ImGuiChildFlags.None,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        ImGui.Text("#" + call.Id + "  " + PriorityText(call.Priority));
        ImGui.Text(SeverityText(call.Severity) + " povreda");
        ImGui.TextDisabled(StatusText(call.Status));

        // Traka zdravlja: boja ide od bledozute do narandzaste, iste boje kao ispuna na mapi.
        float health = call.Patient.Health;
        System.Numerics.Vector3 color = Palette.HealthColor(health);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new System.Numerics.Vector4(color.X, color.Y, color.Z, 1f));
        ImGui.ProgressBar(health, new System.Numerics.Vector2(-1f, 0f), (int)(health * 100f) + "%");
        ImGui.PopStyleColor();

        // Klik bilo gde na kartici bira poziv.
        if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            SelectedCallId = call.Id;
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private static void DrawAmbulances(GameWorld world)
    {
        ImGui.Text("Vozila (" + world.Ambulances.Count + ")");
        for (int i = 0; i < world.Ambulances.Count; i++)
        {
            Ambulance ambulance = world.Ambulances[i];
            ImGui.BulletText("Vozilo " + (i + 1) + ": " + ambulance.State.Name);
        }
    }

    private static bool Contains(GameWorld world, int callId)
    {
        foreach (EmergencyCall call in world.Calls)
        {
            if (call.Id == callId)
            {
                return true;
            }
        }
        return false;
    }

    private static string PriorityText(Priority priority)
    {
        switch (priority)
        {
            case Priority.High:
                return "Visok";
            case Priority.Medium:
                return "Srednji";
            default:
                return "Nizak";
        }
    }

    private static string SeverityText(InjurySeverity severity)
    {
        return severity == InjurySeverity.Heavy ? "Teska" : "Laka";
    }

    private static string StatusText(CallStatus status)
    {
        switch (status)
        {
            case CallStatus.Assigned:
                return "Vozilo na putu";
            case CallStatus.Attended:
                return "Zbrinjava se";
            default:
                return "Ceka vozilo";
        }
    }
}