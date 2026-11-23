namespace DispatchCity.Rendering;

// Shaderi za slike (sprite-ove) i svetlo sirene.
// Isti kvadrat se crta za svaki objekat; položaj, veličina, ugao i sličica stižu kao uniforme.
public static class SpriteShaders
{
    // Kvadrat je od -0.5 do 0.5, a uAspect (širina / visina sličice) ga razvlači na oblik slike. Y osa ide nadole kao u mapi i u slici, pa se slika ne obrće.
    // uCell je (kolona, red) sličice u atlasu, a uGrid je (broj kolona, broj redova) atlasa.
    public const string Vertex = """
        #version 330 core
        layout(location = 0) in vec2 aPos;
        layout(location = 1) in vec2 aUV;
        uniform mat4 uViewProj;
        uniform vec2 uPosition;
        uniform float uSize;
        uniform float uAspect;
        uniform float uAngle;
        uniform vec2 uCell;
        uniform vec2 uGrid;
        out vec2 vUV;
        out vec2 vLocal;
        void main()
        {
            vec2 p = vec2(aPos.x * uAspect, aPos.y) * uSize;
            float c = cos(uAngle);
            float s = sin(uAngle);
            vec2 rotated = vec2(p.x * c - p.y * s, p.x * s + p.y * c);
            gl_Position = uViewProj * vec4(uPosition + rotated, 0.0, 1.0);
            vUV = (uCell + aUV) / uGrid;
            vLocal = aUV;
        }
        """;

    // Uzme boju iz slike. Providni pikseli se odbacuju, a ivice se mešaju sa pozadinom (blending).
    public const string SpriteFragment = """
        #version 330 core
        in vec2 vUV;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        void main()
        {
            vec4 color = texture(uTexture, vUV);
            if (color.a < 0.02)
            {
                discard;
            }
            FragColor = color;
        }
        """;

    // Puls: prsten koji raste od sredine do ivice kvadrata i usput bledi.
    // uPhase ide od 0 do 1 (CPU ga povećava, brže kad je pacijent lošije).
    public const string PulseFragment = """
        #version 330 core
        in vec2 vLocal;
        out vec4 FragColor;
        uniform vec3 uColor;
        uniform float uPhase;
        void main()
        {
            float distanceFromCenter = length(vLocal - vec2(0.5)) * 2.0;
            float ring = 1.0 - smoothstep(0.0, 0.12, abs(distanceFromCenter - uPhase));
            float fade = 1.0 - uPhase;
            FragColor = vec4(uColor, ring * fade);
        }
        """;

    // Rotirajuće svetlo sirene: crveni zrak i plavi zrak (jedan naspram drugog) kruže oko sredine,
    // a u sredini je jezgro koje menja boju. Ugao zraka zavisi od vremena (uTime).
    public const string SirenFragment = """
        #version 330 core
        in vec2 vLocal;
        out vec4 FragColor;
        uniform float uTime;
        uniform float uSpeed;
        void main()
        {
            vec2 p = vLocal - vec2(0.5);
            float distanceFromCenter = length(p) * 2.0;
            float angle = atan(p.y, p.x);
            float rotation = uTime * uSpeed;

            // Zrak je najjači u pravcu rotacije, a širina mu se podešava stepenom (veći = uži).
            float redBeam = pow(max(cos(angle - rotation), 0.0), 8.0);
            float blueBeam = pow(max(cos(angle - rotation - 3.14159265), 0.0), 8.0);
            float falloff = 1.0 - smoothstep(0.05, 1.0, distanceFromCenter);

            vec3 red = vec3(1.0, 0.1, 0.1);
            vec3 blue = vec3(0.1, 0.35, 1.0);
            vec3 beamColor = (red * redBeam + blue * blueBeam) / max(redBeam + blueBeam, 0.0001);
            float beamAlpha = max(redBeam, blueBeam) * falloff * 0.9;

            float core = 1.0 - smoothstep(0.0, 0.22, distanceFromCenter);
            vec3 coreColor = mix(red, blue, 0.5 + 0.5 * sin(uTime * uSpeed * 2.0));

            FragColor = vec4(mix(beamColor, coreColor, core), max(beamAlpha, core));
        }
        """;
}