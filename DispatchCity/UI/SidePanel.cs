using DispatchCity.Rendering;
using DispatchCity.Simulation;
using DispatchCity.Simulation.Dispatch;
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

    // Ponudjene brzine igre.
    private static readonly float[] SpeedOptions = { 1f, 2f, 4f };

    // Izabrani poziv (UI stanje, svet za njega ne zna). Biće potreban za ručnu dodelu vozila.
    public int? SelectedCallId { get; private set; }

    public void Draw(GameWorld world, ViewOptions options, float x, float width, float height)
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

            DrawTimeControls(world);
            DrawViewOptions(options);
            ImGui.Spacing();
            DrawScore(world);
            ImGui.Spacing();
            DrawControlMode(world);
            ImGui.Spacing();
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

    // Pauza i brzina igre. Space na tastaturi radi isto kao dugme Pauza.
    private static void DrawTimeControls(GameWorld world)
    {
        if (ImGui.Button(world.IsPaused ? "Nastavi" : "Pauza"))
        {
            world.IsPaused = !world.IsPaused;
        }

        for (int i = 0; i < SpeedOptions.Length; i++)
        {
            ImGui.SameLine();
            float speed = SpeedOptions[i];
            if (ImGui.RadioButton((int)speed + "x", world.SpeedMultiplier == speed))
            {
                world.SpeedMultiplier = speed;
            }
        }

        if (world.IsPaused)
        {
            ImGui.TextColored(UiTheme.FromHex(0xE5634F), "PAUZA");
        }
    }

    // Toplotna mapa: ulice obojene vremenom dolaska iz bolnice (taster H), sa legendom rampe.
    private static void DrawViewOptions(ViewOptions options)
    {
        bool showTraffic = options.ShowTraffic;
        if (ImGui.Checkbox("Guzva (G)", ref showTraffic))
        {
            options.ShowTraffic = showTraffic;
        }

        bool showHeat = options.ShowHeatMap;
        if (ImGui.Checkbox("Toplotna mapa (H)", ref showHeat))
        {
            options.ShowHeatMap = showHeat;
        }

        if (!showHeat)
        {
            return;
        }

        // Legenda: traka od boje "blizu" preko "srednje" do "daleko", ista rampa kao na mapi.
        float font = ImGui.GetFontSize();
        System.Numerics.Vector2 start = ImGui.GetCursorScreenPos();
        float barWidth = ImGui.GetContentRegionAvail().X;
        float barHeight = font * 0.8f;
        float half = barWidth / 2f;

        uint near = ColorOf(Palette.HeatNear);
        uint mid = ColorOf(Palette.HeatMid);
        uint far = ColorOf(Palette.HeatFar);

        ImDrawListPtr draw = ImGui.GetWindowDrawList();
        draw.AddRectFilledMultiColor(start,
            new System.Numerics.Vector2(start.X + half, start.Y + barHeight), near, mid, mid, near);
        draw.AddRectFilledMultiColor(new System.Numerics.Vector2(start.X + half, start.Y),
            new System.Numerics.Vector2(start.X + barWidth, start.Y + barHeight), mid, far, far, mid);

        ImGui.Dummy(new System.Numerics.Vector2(barWidth, barHeight));
        ImGui.TextDisabled("blizu bolnice  ->  daleko");
    }

    private static uint ColorOf(System.Numerics.Vector3 color)
    {
        return ImGui.GetColorU32(new System.Numerics.Vector4(color.X, color.Y, color.Z, 1f));
    }

    // Rezultat uzivo. Kad je nivo gotov, isti brojevi su konacan rezultat.
    private static void DrawScore(GameWorld world)
    {
        ScoreBoard score = world.Score;

        if (world.IsFinished)
        {
            ImGui.TextColored(UiTheme.FromHex(0xE5634F), "KRAJ NIVOA");
        }
        else
        {
            ImGui.Text("Poziv " + world.CallsCreated + " / " + world.Config.CallsPerLevel);
        }

        ImGui.Text("Bodovi: " + score.Points);
        ImGui.Text("Spaseno: " + score.Saved + "   Izgubljeno: " + score.Lost);
        ImGui.Text("Prosecan odziv: " + score.AverageResponseSeconds.ToString("0.0") + " s");
    }

    // Ko dodeljuje vozila: igrac (rucno) ili jedna od strategija.
    private static void DrawControlMode(GameWorld world)
    {
        ImGui.Text("Upravljanje");

        if (ImGui.RadioButton("Rucno", world.AutoDispatch == null))
        {
            world.AutoDispatch = null;
        }

        foreach (IDispatchStrategy strategy in world.Strategies)
        {
            if (ImGui.RadioButton("Auto: " + strategy.Name, world.AutoDispatch == strategy))
            {
                world.AutoDispatch = strategy;
            }
        }
    }

    // Klik na slobodno vozilo salje ga na izabrani poziv. Zauzeta vozila su zasivljena.
    private void DrawAmbulances(GameWorld world)
    {
        ImGui.Text("Vozila (" + world.Ambulances.Count + ")");
        ImGui.TextDisabled("Izaberi poziv, pa vozilo");

        for (int i = 0; i < world.Ambulances.Count; i++)
        {
            Ambulance ambulance = world.Ambulances[i];

            string label = "Vozilo " + (i + 1) + ": " + ambulance.State.Name;
            if (ambulance.Call != null)
            {
                label += "  (poziv #" + ambulance.Call.Id + ")";
            }

            ImGuiSelectableFlags flags = ambulance.IsAvailable
                ? ImGuiSelectableFlags.None
                : ImGuiSelectableFlags.Disabled;

            if (ImGui.Selectable(label + "##vozilo" + i, false, flags))
            {
                SendToSelectedCall(world, ambulance);
            }
        }
    }

    // Isti Command koji koristi i automatska strategija, samo ga ovde pravi igrac.
    private void SendToSelectedCall(GameWorld world, Ambulance ambulance)
    {
        EmergencyCall? call = FindSelectedCall(world);
        if (call == null)
        {
            return;
        }

        if (world.Execute(new AssignAmbulanceCommand(ambulance, call)))
        {
            SelectedCallId = null;
        }
    }

    private EmergencyCall? FindSelectedCall(GameWorld world)
    {
        if (SelectedCallId == null)
        {
            return null;
        }

        foreach (EmergencyCall call in world.Calls)
        {
            if (call.Id == SelectedCallId.Value)
            {
                return call;
            }
        }
        return null;
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