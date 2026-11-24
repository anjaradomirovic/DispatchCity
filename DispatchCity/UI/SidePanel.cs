using DispatchCity.Core;
using DispatchCity.Rendering;
using DispatchCity.Simulation;
using DispatchCity.Simulation.Dispatch;
using ImGuiNET;

namespace DispatchCity.UI;

// Kontrolni panel sa strane mape. Samo cita stanje sveta (CPU) i crta ga preko ImGui-ja.
// Gore je uvek ono sto treba stalno da se vidi (rezultat i vreme), a ispod su dva jezicka:
// "Igra" (rezim, pozivi, vozila, prikaz) i "Pomoc" (komande i legenda).
// Tekst je bez dijakritika jer ih osnovni ImGui font nema.
public class SidePanel
{
    // Velicine su razmeri velicine slova, pa se panel skalira zajedno sa prozorom.
    private const float CardWidthRatio = 10.5f;
    private const float CardHeightRatio = 7.2f;
    private const float VehicleCardHeightRatio = 6.6f;
    private const float HeaderHeightRatio = 2.6f;
    private const float BigNumberScale = 1.5f;
    private const float TitleScale = 1.3f;

    private static readonly float[] SpeedOptions = { 1f, 2f, 4f };

    // Izabrani poziv (UI stanje, svet za njega ne zna).
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
            DrawHeader(world);
            DrawScore(world);
            ImGui.Spacing();
            DrawTimeControls(world);
            ImGui.Spacing();

            if (ImGui.BeginTabBar("tabovi"))
            {
                if (ImGui.BeginTabItem("Igra"))
                {
                    DrawControlMode(world);
                    ImGui.Spacing();
                    DrawCalls(world);
                    ImGui.Spacing();
                    DrawAmbulances(world);
                    ImGui.Spacing();
                    DrawViewOptions(options);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Pomoc"))
                {
                    DrawCommands();
                    ImGui.Spacing();
                    DrawLegend();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
    }

    // ---------- Zaglavlje, rezultat, vreme ----------

    private static void DrawHeader(GameWorld world)
    {
        float font = ImGui.GetFontSize();
        ImGui.PushStyleColor(ImGuiCol.ChildBg, UiTheme.FromHex(0x6D1A2E));
        ImGui.BeginChild("zaglavlje", new System.Numerics.Vector2(0f, font * HeaderHeightRatio),
            ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);

        ImGui.SetWindowFontScale(TitleScale);
        ImGui.TextColored(UiTheme.FromHex(0xF2C230), "DISPATCH CITY");
        ImGui.SetWindowFontScale(1f);

        string progress = world.IsFinished
            ? "KRAJ NIVOA"
            : "Poziv " + world.CallsCreated + " / " + world.Config.CallsPerLevel;
        ImGui.TextColored(UiTheme.FromHex(0xF7F1E3), progress);

        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    // Cetiri velika broja u redu: bodovi, spaseni, izgubljeni, prosecan odziv.
    private static void DrawScore(GameWorld world)
    {
        ScoreBoard score = world.Score;

        if (!ImGui.BeginTable("rezultat", 4))
        {
            return;
        }

        ScoreCell(score.Points.ToString(), "bodovi", UiTheme.FromHex(0x3A2A2E));
        ScoreCell(score.Saved.ToString(), "spaseno", UiTheme.FromHex(0x3F8F4A));
        ScoreCell(score.Lost.ToString(), "izgubljeno", UiTheme.FromHex(0xC8402F));
        ScoreCell(score.AverageResponseSeconds.ToString("0") + " s", "odziv", UiTheme.FromHex(0x3A2A2E));

        ImGui.EndTable();
    }

    private static void ScoreCell(string value, string label, System.Numerics.Vector4 color)
    {
        ImGui.TableNextColumn();
        ImGui.SetWindowFontScale(BigNumberScale);
        ImGui.TextColored(color, value);
        ImGui.SetWindowFontScale(1f);
        ImGui.TextDisabled(label);
    }

    // Pauza i brzina: pauza je jedno dugme, a brzine su tri dugmeta od kojih je aktivno oznaceno.
    private static void DrawTimeControls(GameWorld world)
    {
        float font = ImGui.GetFontSize();
        float available = ImGui.GetContentRegionAvail().X;
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        int buttons = 1 + SpeedOptions.Length;
        float buttonWidth = (available - spacing * (buttons - 1)) / buttons;
        var size = new System.Numerics.Vector2(buttonWidth, font * 1.9f);

        if (Toggle(world.IsPaused ? "Nastavi" : "Pauza", world.IsPaused, size))
        {
            world.IsPaused = !world.IsPaused;
        }

        for (int i = 0; i < SpeedOptions.Length; i++)
        {
            ImGui.SameLine();
            float speed = SpeedOptions[i];
            if (Toggle((int)speed + "x", world.SpeedMultiplier == speed, size))
            {
                world.SpeedMultiplier = speed;
            }
        }

        if (world.IsPaused)
        {
            ImGui.TextColored(UiTheme.FromHex(0xC8402F), "PAUZA");
        }
    }

    // Dugme koje je kad je ukljuceno bordo (jasno se vidi sta je izabrano), a inace zuto.
    private static bool Toggle(string label, bool active, System.Numerics.Vector2 size)
    {
        if (active)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.FromHex(0x6D1A2E));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiTheme.FromHex(0x85263C));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, UiTheme.FromHex(0x55121F));
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.FromHex(0xF7F1E3));
        }

        bool clicked = ImGui.Button(label, size);

        if (active)
        {
            ImGui.PopStyleColor(4);
        }
        return clicked;
    }

    // ---------- Rezim: rucno / automatski ----------

    private static void DrawControlMode(GameWorld world)
    {
        ImGui.Text("Ko salje vozila");

        float font = ImGui.GetFontSize();
        float available = ImGui.GetContentRegionAvail().X;
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        var half = new System.Numerics.Vector2((available - spacing) / 2f, font * 1.9f);

        bool manual = world.AutoDispatch == null;
        if (Toggle("Ja (rucno)", manual, half))
        {
            world.AutoDispatch = null;
        }

        ImGui.SameLine();
        if (Toggle("Racunar (auto)", !manual, half) && manual && world.Strategies.Count > 0)
        {
            world.AutoDispatch = world.Strategies[0];
        }

        if (manual)
        {
            ImGui.TextDisabled("Izaberi poziv, pa klikni Posalji na vozilu.");
            return;
        }

        ImGui.TextDisabled("Pravilo racunara:");
        foreach (IDispatchStrategy strategy in world.Strategies)
        {
            bool selected = world.AutoDispatch == strategy;
            if (Toggle(strategy.Name, selected, new System.Numerics.Vector2(available, font * 1.7f)))
            {
                world.AutoDispatch = strategy;
            }
        }
    }

    // ---------- Pozivi ----------

    private void DrawCalls(GameWorld world)
    {
        ImGui.Text("Pozivi (" + world.Calls.Count + ")");

        // Ako izabrani poziv vise ne postoji (spasen ili izgubljen), izbor se ponistava.
        if (SelectedCallId != null && FindCall(world, SelectedCallId.Value) == null)
        {
            SelectedCallId = null;
        }

        float font = ImGui.GetFontSize();
        var cardSize = new System.Numerics.Vector2(font * CardWidthRatio, font * CardHeightRatio);

        // Spoljni okvir se pomera horizontalno kad kartica ima vise nego sto staje.
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
            DrawCard(world, world.Calls[i], cardSize);
        }

        ImGui.EndChild();
    }

    private void DrawCard(GameWorld world, EmergencyCall call, System.Numerics.Vector2 size)
    {
        bool selected = SelectedCallId == call.Id;
        System.Numerics.Vector4 background = selected ? UiTheme.CardSelected : UiTheme.Card;

        ImGui.PushStyleColor(ImGuiCol.ChildBg, background);
        ImGui.BeginChild("poziv" + call.Id, size, ImGuiChildFlags.None,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        ImGui.Text("#" + call.Id + "  " + PriorityText(call.Priority));
        ImGui.Text(SeverityText(call.Severity) + " povreda");

        // Jasno ko dolazi i za koliko: vozilo i preostalo vreme, ili poruka da niko ne dolazi.
        DrawArrivalLine(world, call);

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

    private static void DrawArrivalLine(GameWorld world, EmergencyCall call)
    {
        Ambulance? ambulance = call.AssignedAmbulance;

        if (call.Status == CallStatus.Waiting || ambulance == null)
        {
            ImGui.TextColored(UiTheme.FromHex(0xC8402F), "Ceka vozilo");
            ImGui.TextDisabled("niko ne dolazi");
            return;
        }

        int number = IndexOf(world, ambulance) + 1;
        if (call.Status == CallStatus.Attended)
        {
            ImGui.TextColored(UiTheme.FromHex(0x3F8F4A), "Vozilo " + number);
            ImGui.TextDisabled("zbrinjava se");
            return;
        }

        ImGui.TextColored(UiTheme.FromHex(0x9A6A00), "Vozilo " + number + " dolazi");
        ImGui.Text("za " + SecondsText(ambulance.RemainingSeconds));
    }

    // ---------- Vozila ----------

    private void DrawAmbulances(GameWorld world)
    {
        ImGui.Text("Vozila (" + world.Ambulances.Count + ")");

        EmergencyCall? selected = SelectedCallId == null ? null : FindCall(world, SelectedCallId.Value);
        bool manual = world.AutoDispatch == null;
        float font = ImGui.GetFontSize();

        for (int i = 0; i < world.Ambulances.Count; i++)
        {
            Ambulance ambulance = world.Ambulances[i];

            ImGui.PushStyleColor(ImGuiCol.ChildBg, UiTheme.Card);
            ImGui.BeginChild("vozilo" + i, new System.Numerics.Vector2(0f, font * VehicleCardHeightRatio),
                ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            ImGui.Text("Vozilo " + (i + 1));
            ImGui.SameLine();
            ImGui.TextColored(StateColor(ambulance), StateText(ambulance));

            DrawTrip(ambulance);
            DrawSendButton(world, ambulance, selected, manual);

            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
    }

    // Za vozilo koje vozi: za koji poziv i traka putovanja sa preostalim vremenom.
    private static void DrawTrip(Ambulance ambulance)
    {
        if (ambulance.IsAvailable)
        {
            ImGui.TextDisabled("Spremno u bolnici");
            return;
        }

        if (ambulance.Call != null)
        {
            ImGui.TextDisabled("Poziv #" + ambulance.Call.Id);
        }
        else
        {
            ImGui.TextDisabled("Bez poziva");
        }

        if (ambulance.State.Name == "OnScene")
        {
            ImGui.TextDisabled("Na licu mesta");
            return;
        }

        string overlay = SecondsText(ambulance.RemainingSeconds);
        ImGui.ProgressBar(ambulance.TripProgress, new System.Numerics.Vector2(-1f, 0f), overlay);
    }

    // U rucnom rezimu: dugme za slanje ovog vozila na izabrani poziv.
    private void DrawSendButton(GameWorld world, Ambulance ambulance, EmergencyCall? selected, bool manual)
    {
        if (!manual || !ambulance.IsAvailable)
        {
            return;
        }

        if (selected == null)
        {
            ImGui.TextDisabled("Izaberi poziv iznad");
            return;
        }

        if (ImGui.Button("Posalji na #" + selected.Id + "##posalji" + IndexOf(world, ambulance),
                new System.Numerics.Vector2(-1f, 0f)))
        {
            if (world.Execute(new AssignAmbulanceCommand(ambulance, selected)))
            {
                SelectedCallId = null;
            }
        }
    }

    // ---------- Prikaz ----------

    private static void DrawViewOptions(ViewOptions options)
    {
        ImGui.Text("Prikaz mape");

        bool showTraffic = options.ShowTraffic;
        if (ImGui.Checkbox("Guzva na ulicama (G)", ref showTraffic))
        {
            options.ShowTraffic = showTraffic;
        }

        bool showHeat = options.ShowHeatMap;
        if (ImGui.Checkbox("Toplotna mapa dolaska (H)", ref showHeat))
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

    // ---------- Pomoc: komande i legenda ----------

    private static void DrawCommands()
    {
        ImGui.Text("Komande");

        if (!ImGui.BeginTable("komande", 2))
        {
            return;
        }

        CommandRow("Space", "pauza / nastavak");
        CommandRow("1  2  3", "brzina 1x / 2x / 4x");
        CommandRow("Tab", "promena rezima (rucno / auto)");
        CommandRow("Enter", "dodeli vozila odmah");
        CommandRow("G", "guzva na ulicama");
        CommandRow("H", "toplotna mapa dolaska");
        CommandRow("W A S D", "pomeranje kamere");
        CommandRow("Q  E", "odzumiraj / zumiraj");
        CommandRow("Tockic", "zum ka kursoru");
        CommandRow("Prevlacenje", "pomeranje kamere misem");
        CommandRow("Esc", "izlaz iz igre");

        ImGui.EndTable();
    }

    private static void CommandRow(string key, string action)
    {
        ImGui.TableNextColumn();
        ImGui.TextColored(UiTheme.FromHex(0x6D1A2E), key);
        ImGui.TableNextColumn();
        ImGui.TextWrapped(action);
    }

    // Legenda: boje se uzimaju iz Palette, iste kao na mapi, a velicine su razmeri velicine slova.
    private static void DrawLegend()
    {
        ImGui.Text("Legenda");
        float font = ImGui.GetFontSize();

        LegendRow(font, Palette.CallRing(InjurySeverity.Heavy), "Prsten: teska povreda", CircleSwatch);
        LegendRow(font, Palette.CallRing(InjurySeverity.Light), "Prsten: laka povreda", CircleSwatch);
        LegendRow(font, Palette.HealthColor(1f), "Ispuna: pacijent stabilan", CircleSwatch);
        LegendRow(font, Palette.HealthColor(0f), "Ispuna: pacijent kritican", CircleSwatch);

        DrawPrioritySizes(font);

        LegendRow(font, Palette.HospitalRing, "Bolnica", CircleSwatch);
        LegendRow(font, Palette.Ambulance, "Vozilo", CircleSwatch);
        LegendRow(font, Palette.RoadColor(RoadType.Avenue), "Avenija (brza)", RoadSwatch);
        LegendRow(font, Palette.RoadColor(RoadType.Main), "Glavna ulica", RoadSwatch);
        LegendRow(font, Palette.RoadColor(RoadType.Local), "Lokalna ulica (spora)", RoadSwatch);
        LegendRow(font, Palette.JamMid, "Guzva: umerena", RoadSwatch);
        LegendRow(font, Palette.JamHigh, "Guzva: jaka", RoadSwatch);
    }

    private delegate void SwatchDrawer(ImDrawListPtr draw, System.Numerics.Vector2 start, float size, uint color);

    private static void CircleSwatch(ImDrawListPtr draw, System.Numerics.Vector2 start, float size, uint color)
    {
        draw.AddCircleFilled(new System.Numerics.Vector2(start.X + size / 2f, start.Y + size / 2f), size / 2f, color);
    }

    private static void RoadSwatch(ImDrawListPtr draw, System.Numerics.Vector2 start, float size, uint color)
    {
        float inset = size * 0.3f;
        draw.AddRectFilled(new System.Numerics.Vector2(start.X, start.Y + inset),
            new System.Numerics.Vector2(start.X + size, start.Y + size - inset), color);
    }

    private static void LegendRow(float font, System.Numerics.Vector3 color, string text, SwatchDrawer swatch)
    {
        System.Numerics.Vector2 start = ImGui.GetCursorScreenPos();
        swatch(ImGui.GetWindowDrawList(), start, font, ColorOf(color));
        ImGui.Dummy(new System.Numerics.Vector2(font, font));
        ImGui.SameLine();
        ImGui.Text(text);
    }

    private static void DrawPrioritySizes(float font)
    {
        // Najveci krug je visok prioritet; svaki sledeci je manji za isti razmer.
        float[] ratios = { 1f, 0.75f, 0.5f };
        ImDrawListPtr draw = ImGui.GetWindowDrawList();
        System.Numerics.Vector2 start = ImGui.GetCursorScreenPos();
        uint color = ColorOf(Palette.CallRing(InjurySeverity.Light));

        float x = start.X;
        for (int i = 0; i < ratios.Length; i++)
        {
            float diameter = font * ratios[i];
            draw.AddCircleFilled(new System.Numerics.Vector2(x + font / 2f, start.Y + font / 2f), diameter / 2f, color);
            x += font * 1.1f;
        }

        ImGui.Dummy(new System.Numerics.Vector2(x - start.X, font));
        ImGui.SameLine();
        ImGui.Text("Velicina: prioritet");
    }

    // ---------- Pomocne ----------

    private static uint ColorOf(System.Numerics.Vector3 color)
    {
        return ImGui.GetColorU32(new System.Numerics.Vector4(color.X, color.Y, color.Z, 1f));
    }

    private static string SecondsText(float seconds)
    {
        if (float.IsInfinity(seconds))
        {
            return "--";
        }
        return (int)MathF.Ceiling(seconds) + " s";
    }

    private static int IndexOf(GameWorld world, Ambulance ambulance)
    {
        for (int i = 0; i < world.Ambulances.Count; i++)
        {
            if (world.Ambulances[i] == ambulance)
            {
                return i;
            }
        }
        return -1;
    }

    private static EmergencyCall? FindCall(GameWorld world, int callId)
    {
        foreach (EmergencyCall call in world.Calls)
        {
            if (call.Id == callId)
            {
                return call;
            }
        }
        return null;
    }

    private static string StateText(Ambulance ambulance)
    {
        switch (ambulance.State.Name)
        {
            case "EnRoute":
                return "ide na poziv";
            case "OnScene":
                return "zbrinjava";
            case "Transporting":
                return "vozi pacijenta";
            case "Returning":
                return "vraca se";
            default:
                return "slobodno";
        }
    }

    private static System.Numerics.Vector4 StateColor(Ambulance ambulance)
    {
        switch (ambulance.State.Name)
        {
            case "EnRoute":
                return UiTheme.FromHex(0x9A6A00);
            case "OnScene":
            case "Transporting":
                return UiTheme.FromHex(0xC8402F);
            case "Returning":
                return UiTheme.FromHex(0x5A6E8C);
            default:
                return UiTheme.FromHex(0x3F8F4A);
        }
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
}