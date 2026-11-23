using System.Diagnostics;
using System.Numerics;
using DispatchCity.Config;
using DispatchCity.Simulation;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Crta slike: bakice na mestima poziva i ambulante sa treperavim svetlom.
// Jedan isti kvadrat se crta za svaki objekat; položaj, veličina, ugao i sličica
// iz atlasa šalju se kao uniforme (kao u mini igri iz termina 5).
public class SpriteLayer : IRenderLayer
{
    // Veličina slike u odnosu na poluprečnik kruga (menjaj dok ne izgleda dobro).
    private const float AmbulanceScale = 3.0f;
    private const float PatientScale = 2.2f;

    // Svaki lik (patient_1.png, patient_2.png...) je atlas sa ovim brojem kolona i redova.
    // Veličina slike u pikselima je nebitna, bitno je samo koliko je sličica u mreži.
    // Red 0 = stabilan pacijent, red 1 = kritičan. Brzina animacije je broj sličica u sekundi.
    // Tvoji crteži imaju jednu sličicu po liku, pa su oba broja 1. Tada se pokret (ljuljanje i
    // poskakivanje) dodaje iz koda. Ako jednog dana nacrtaš više sličica, stavi 4 i 2.
    private const int PatientColumns = 1;
    private const int PatientRows = 1;
    private const float FramesPerSecond = 6f;

    // Svetlo sirene: brzina rotacije (radijani u sekundi, 6 = skoro jedan krug u sekundi)
    // i veličina u odnosu na ambulantu (veće = zraci duži).
    private const float SirenSpeed = 6f;
    private const float SirenSize = 2.2f;

    // Drveće oko grada: razmak između stabala i prazan pojas između grada i prvog drveta (razmeri jedinice mere).
    private const float TreeSpacing = 0.4f;
    private const float TreeClearance = 0.2f;

    // Puls oko poziva: koliko je kvadrat veći od kruga poziva i koliko pulsa u sekundi (zdrav, najlošiji).
    private const float PulseScale = 4.5f;
    private const float PulseSlowest = 0.5f;
    private const float PulseFastest = 2.5f;

    private readonly record struct Tree(Vector2 Position, float Size);

    // Vreme za animacije teče i kad je igra pauzirana.
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private GameWorld _world = null!;       // postavlja se u Initialize
    private Shader _spriteShader = null!;
    private Shader _sirenShader = null!;
    private Shader _pulseShader = null!;
    private readonly Dictionary<int, float> _pulsePhases = new Dictionary<int, float>();
    private float _previousTime;
    private readonly List<Texture2D> _patients = new List<Texture2D>();
    private Texture2D _ambulance = null!;
    private Texture2D _tree = null!;
    private readonly List<Tree> _trees = new List<Tree>();
    private int _vao;
    private int _vbo;
    private int _ebo;

    public void Initialize(GameWorld world)
    {
        _world = world;

        _spriteShader = new Shader(SpriteShaders.Vertex, SpriteShaders.SpriteFragment);
        _sirenShader = new Shader(SpriteShaders.Vertex, SpriteShaders.SirenFragment);
        _pulseShader = new Shader(SpriteShaders.Vertex, SpriteShaders.PulseFragment);

        // Ako postoje tvoji crteži u assets/, koriste se oni; ako ne, privremeni iz koda.
        string folder = Path.Combine(AppContext.BaseDirectory, "assets");
        LoadPatients(folder);
        _tree = Texture2D.FromFile(Path.Combine(folder, "tree.png")) ?? PlaceholderSprites.CreateTree();
        CreateTrees();
        _ambulance = Texture2D.FromFile(Path.Combine(folder, "ambulance.png")) ?? PlaceholderSprites.CreateAmbulance();

        CreateQuad();
    }

    // Učitava patient_1.png, patient_2.png... dok ima fajlova. Ako nema nijednog, uzima privremeni crtež.
    private void LoadPatients(string folder)
    {
        for (int i = 1; ; i++)
        {
            Texture2D? texture = Texture2D.FromFile(Path.Combine(folder, "patient_" + i + ".png"));
            if (texture == null)
            {
                break;
            }
            _patients.Add(texture);
        }

        if (_patients.Count == 0)
        {
            _patients.Add(PlaceholderSprites.CreateGrandmaAtlas());
        }
    }

    // Sve se čita pri crtanju, pa ovde nema šta da se šalje.
    public void Update(GameWorld world)
    {
    }

    public void Render(Camera2D camera)
    {
        float time = (float)_clock.Elapsed.TotalSeconds;
        WorldMetrics metrics = _world.Config.Metrics;

        // Koliko je prošlo od prošlog crtanja (ograničeno, da pulsevi ne skoče posle zastoja).
        float deltaSeconds = Math.Min(time - _previousTime, 0.1f);
        _previousTime = time;
        AdvancePulses(deltaSeconds);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindVertexArray(_vao);

        _spriteShader.Use();
        _spriteShader.SetMatrix("uViewProj", camera.ViewProjection);
        _spriteShader.SetInt("uTexture", 0);

        DrawTrees();
        DrawPulses(camera, metrics);
        DrawPatients(metrics, time);
        DrawAmbulances(metrics);
        DrawSirenLights(camera, metrics, time);

        GL.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
        foreach (Texture2D patient in _patients)
        {
            patient.Dispose();
        }
        _ambulance.Dispose();
        _tree.Dispose();
    }

    // Pacijent je "pokupljen" kad je vozilo stiglo (počelo zbrinjavanje) ili kad mu je došlo dovoljno
    // blizu da ga slika ambulante prekrije. Rastojanje je pola dužine slike ambulante.
    private bool IsPickedUp(EmergencyCall call, WorldMetrics metrics)
    {
        if (call.Patient.IsBeingTreated)
        {
            return true;
        }

        Ambulance? ambulance = call.AssignedAmbulance;
        if (ambulance == null)
        {
            return false;
        }

        float coverDistance = metrics.AmbulanceRadius * AmbulanceScale * 0.5f;
        return Vector2.Distance(ambulance.Position, call.Location.Position) < coverDistance;
    }

    // Faza pulsa (0 do 1) se čuva za svaki poziv i povećava se brzinom koja zavisi od zdravlja.
    // Pauza je zaustavlja, a brzina igre je ubrzava, isto kao i ostalo u svetu.
    private void AdvancePulses(float deltaSeconds)
    {
        if (_world.IsPaused)
        {
            return;
        }

        foreach (EmergencyCall call in _world.Calls)
        {
            if (call.Patient.IsBeingTreated)
            {
                continue;
            }

            float speed = PulseSlowest + (PulseFastest - PulseSlowest) * (1f - call.Patient.Health);
            _pulsePhases.TryGetValue(call.Id, out float phase);
            _pulsePhases[call.Id] = (phase + deltaSeconds * _world.SpeedMultiplier * speed) % 1f;
        }
    }

    // Prsten oko svakog pacijenta koji čeka pomoć: žut kad je zdrav, crven kad je kritičan.
    private void DrawPulses(Camera2D camera, WorldMetrics metrics)
    {
        _pulseShader.Use();
        _pulseShader.SetMatrix("uViewProj", camera.ViewProjection);

        foreach (EmergencyCall call in _world.Calls)
        {
            if (IsPickedUp(call, metrics) || !_pulsePhases.TryGetValue(call.Id, out float phase))
            {
                continue;
            }

            Vector3 color = Vector3.Lerp(new Vector3(1f, 0.2f, 0.2f), new Vector3(1f, 0.85f, 0.2f), call.Patient.Health);
            float size = metrics.CallBaseRadius * ScaleOf(call.Priority) * PulseScale;

            _pulseShader.SetVector3("uColor", color);
            _pulseShader.SetFloat("uPhase", phase);
            DrawQuad(_pulseShader, call.Location.Position, size, 1f, 0f, 0, 0, 1, 1);
        }

        // Vraćamo shader za slike, jer ga ostalo crtanje očekuje.
        _spriteShader.Use();
    }

    // Stabla se postavljaju jednom, u pojasu između grada i ivice kamere (taj pojas je WorldMargin).
    // Mreža ćelija sa malim slučajnim pomakom daje ravnomeran, a ipak prirodan raspored.
    private void CreateTrees()
    {
        GameConfig config = _world.Config;
        float unit = config.Metrics.Unit;
        float margin = config.WorldMargin;
        float clearance = TreeClearance * unit;
        float cell = TreeSpacing * unit;
        var random = new Random(config.Seed + 11);   // isti seed = isto drveće

        for (float y = -margin; y < config.WorldHeight + margin; y += cell)
        {
            for (float x = -margin; x < config.WorldWidth + margin; x += cell)
            {
                float px = x + (float)random.NextDouble() * cell * 0.8f;
                float py = y + (float)random.NextDouble() * cell * 0.8f;
                float size = unit * (0.4f + 0.2f * (float)random.NextDouble());

                bool insideCity = px > -clearance && px < config.WorldWidth + clearance
                               && py > -clearance && py < config.WorldHeight + clearance;
                bool insideBounds = px > -margin + size / 2f && px < config.WorldWidth + margin - size / 2f
                                 && py > -margin + size / 2f && py < config.WorldHeight + margin - size / 2f;

                if (!insideCity && insideBounds)
                {
                    _trees.Add(new Tree(new Vector2(px, py), size));
                }
            }
        }

        // Šta je niže na ekranu (veći Y), crta se kasnije, pa je ispred.
        _trees.Sort((a, b) => a.Position.Y.CompareTo(b.Position.Y));
    }

    private void DrawTrees()
    {
        _tree.Bind(TextureUnit.Texture0);
        float aspect = AspectOf(_tree, 1, 1);

        foreach (Tree tree in _trees)
        {
            DrawQuad(_spriteShader, tree.Position, tree.Size, aspect, 0f, 0, 0, 1, 1);
        }
    }

    // Pacijent na svakom pozivu. Lik se bira po broju poziva, red atlasa po zdravlju, a kolona po vremenu.
    private void DrawPatients(WorldMetrics metrics, float time)
    {
        foreach (EmergencyCall call in _world.Calls)
        {
            // Pacijent nestaje čim ga ambulanta pokrije (ne tek kad stigne u središte raskrsnice).
            if (IsPickedUp(call, metrics))
            {
                continue;
            }

            Texture2D atlas = _patients[call.Id % _patients.Count];
            atlas.Bind(TextureUnit.Texture0);

            // Ako slika ima samo jedan red, kritični pacijent koristi isti red.
            int row = Math.Min(call.Patient.Health > 0.5f ? 0 : 1, PatientRows - 1);

            // Id poziva pomera početak animacije, da svi pacijenti ne mašu u isto vreme.
            int column = (int)(time * FramesPerSecond + call.Id) % PatientColumns;

            // Visina sličice je mera, a širina prati oblik tvoje slike (nije nužno kvadrat).
            float size = metrics.CallBaseRadius * ScaleOf(call.Priority) * PatientScale;
            float aspect = AspectOf(atlas, PatientColumns, PatientRows);

            Vector2 position = call.Location.Position;
            float angle = 0f;

            // Jedna slika po liku: pokret dolazi iz sinusa (uTime iz termina 3), ne iz crteža.
            // Stabilan pacijent se lagano ljulja, a kritičan brzo i jače drhti.
            if (PatientColumns == 1)
            {
                bool critical = call.Patient.Health <= 0.5f;
                float speed = critical ? 14f : 4f;
                float tilt = critical ? 0.12f : 0.05f;        // nagib u radijanima
                float phase = time * speed + call.Id;

                angle = MathF.Sin(phase) * tilt;
                position.Y -= MathF.Abs(MathF.Sin(phase)) * size * 0.06f;   // malo poskakuje (Y ide nadole)
            }

            DrawQuad(_spriteShader, position, size, aspect, angle, column, row, PatientColumns, PatientRows);
        }
    }

    // Ambulana okrenuta u smeru vožnje (ugao dolazi iz ulice kojom vozi).
    private void DrawAmbulances(WorldMetrics metrics)
    {
        _ambulance.Bind(TextureUnit.Texture0);

        foreach (Ambulance ambulance in _world.Ambulances)
        {
            float size = metrics.AmbulanceRadius * AmbulanceScale;
            DrawQuad(_spriteShader, ambulance.Position, size, AspectOf(_ambulance, 1, 1), ambulance.Heading, 0, 0, 1, 1);
        }
    }

    // Rotirajuće crveno i plavo svetlo iznad ambulante (jedan kvadrat po vozilu, ceo posao radi shader).
    private void DrawSirenLights(Camera2D camera, WorldMetrics metrics, float time)
    {
        _sirenShader.Use();
        _sirenShader.SetMatrix("uViewProj", camera.ViewProjection);
        _sirenShader.SetFloat("uTime", time);
        _sirenShader.SetFloat("uSpeed", SirenSpeed);

        foreach (Ambulance ambulance in _world.Ambulances)
        {
            // Svetla rade samo dok vozilo nije slobodno (vozi, radi na licu mesta ili vraća pacijenta).
            if (ambulance.IsAvailable)
            {
                continue;
            }

            float size = metrics.AmbulanceRadius * AmbulanceScale * SirenSize;
            DrawQuad(_sirenShader, ambulance.Position, size, 1f, 0f, 0, 0, 1, 1);
        }
    }

    // Jedan poziv crtanja: postavi uniforme za ovaj objekat i nacrtaj kvadrat (2 trougla).
    private void DrawQuad(Shader shader, Vector2 position, float size, float aspect, float angle, int column, int row, int columns, int rows)
    {
        shader.SetVector2("uPosition", position.X, position.Y);
        shader.SetFloat("uSize", size);
        shader.SetFloat("uAspect", aspect);
        shader.SetFloat("uAngle", angle);
        shader.SetVector2("uCell", column, row);
        shader.SetVector2("uGrid", columns, rows);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
    }

    // Širina / visina jedne sličice u pikselima. Tako slika bilo kog oblika ne ispadne razvučena.
    private static float AspectOf(Texture2D texture, int columns, int rows)
    {
        float cellWidth = texture.Width / (float)columns;
        float cellHeight = texture.Height / (float)rows;
        return cellWidth / cellHeight;
    }

    // Kvadrat od -0.5 do 0.5: x, y, u, v. (0, 0) u slici je gornji levi ugao.
    private void CreateQuad()
    {
        float[] vertices =
        {
            -0.5f, -0.5f, 0f, 0f,
             0.5f, -0.5f, 1f, 0f,
             0.5f,  0.5f, 1f, 1f,
            -0.5f,  0.5f, 0f, 1f,
        };
        uint[] indices = { 0, 1, 2, 0, 2, 3 };

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        int stride = 4 * sizeof(float);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    // Isti razmeri kao u CallLayer: visok prioritet je veći, nizak manji.
    private static float ScaleOf(Priority priority)
    {
        switch (priority)
        {
            case Priority.High:
                return 1.3f;
            case Priority.Low:
                return 0.8f;
            default:
                return 1.0f;
        }
    }
}