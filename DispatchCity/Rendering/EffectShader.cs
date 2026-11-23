namespace DispatchCity.Rendering;

// Shaderi za pozadinu (šum) i za završnu obradu slike (sivo, vinjeta, sjaj).
public static class EffectShaders
{
    // Pozadina: jedan pravougaonik preko celog sveta. Boju svakog piksela računa fragment shader.
    public const string GroundVertex = """
        #version 330 core
        layout(location = 0) in vec2 aPos;
        uniform mat4 uViewProj;
        out vec2 vWorld;
        void main()
        {
            vWorld = aPos;
            gl_Position = uViewProj * vec4(aPos, 0.0, 1.0);
        }
        """;

    // Šum vrednosti (value noise) sa više slojeva (fbm). Visoka vrednost = šuma, niska = voda.
    // Voda se pojavljuje samo u pojasu oko grada (uMargin), da ulice i pozivi ne završe u jezeru.
    public const string GroundFragment = """
        #version 330 core
        in vec2 vWorld;
        uniform vec2 uWorldMin;
        uniform vec2 uWorldMax;
        uniform float uMargin;
        uniform float uNoiseScale;
        uniform vec2 uSeedOffset;
        uniform float uWaterLevel;
        uniform float uForestLevel;
        uniform float uSoftness;
        uniform float uShoreWidth;
        uniform float uEdgeWaterBias;
        uniform vec3 uWater;
        uniform vec3 uShore;
        uniform vec3 uGrass;
        uniform vec3 uForest;
        out vec4 FragColor;

        float hash(vec2 p)
        {
            return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
        }

        float valueNoise(vec2 p)
        {
            vec2 cell = floor(p);
            vec2 f = fract(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = hash(cell);
            float b = hash(cell + vec2(1.0, 0.0));
            float c = hash(cell + vec2(0.0, 1.0));
            float d = hash(cell + vec2(1.0, 1.0));
            return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
        }

        // Cetiri sloja suma: svaki sledeci je duplo sitniji i duplo slabiji.
        float fbm(vec2 p)
        {
            float sum = 0.0;
            float amplitude = 0.5;
            float total = 0.0;
            for (int i = 0; i < 4; i++)
            {
                sum += amplitude * valueNoise(p);
                total += amplitude;
                p = p * 2.0 + vec2(19.7, 7.3);
                amplitude *= 0.5;
            }
            return sum / total;
        }

        void main()
        {
            float n = fbm(vWorld * uNoiseScale + uSeedOffset);

            // 0 unutar grada, raste do 1 na spoljnoj ivici pojasa.
            vec2 outside = max(max(uWorldMin - vWorld, vWorld - uWorldMax), vec2(0.0));
            float edge = clamp(length(outside) / uMargin, 0.0, 1.0);

            vec3 land = mix(uGrass, uForest, smoothstep(uForestLevel - uSoftness, uForestLevel + uSoftness, n));

            float h = n - edge * uEdgeWaterBias;
            float allowed = smoothstep(0.0, uSoftness, edge);
            float water = (1.0 - smoothstep(uWaterLevel - uSoftness, uWaterLevel + uSoftness, h)) * allowed;
            float shore = (1.0 - smoothstep(0.0, uShoreWidth, abs(h - uWaterLevel))) * allowed;

            vec3 color = mix(land, uShore, shore);
            color = mix(color, uWater, water);
            FragColor = vec4(color, 1.0);
        }
        """;

    // Završna obrada: ceo kadar je već nacrtan u teksturu, ovde se čita piksel po piksel.
    public const string PostVertex = """
        #version 330 core
        layout(location = 0) in vec2 aPos;
        out vec2 vUV;
        void main()
        {
            vUV = aPos * 0.5 + 0.5;
            gl_Position = vec4(aPos, 0.0, 1.0);
        }
        """;

    public const string PostFragment = """
        #version 330 core
        in vec2 vUV;
        uniform sampler2D uScene;
        uniform float uAspect;
        uniform float uGray;
        uniform float uVignette;
        uniform vec3 uVignetteColor;
        uniform float uGlow;
        uniform vec2 uGlowCenter;
        uniform float uGlowRadius;
        uniform vec3 uGlowColor;
        out vec4 FragColor;

        // Vinjeta: od koje udaljenosti od sredine pocinje i koliko jako boji ivice.
        const float VignetteInner = 0.45;
        const float VignetteOuter = 1.1;
        const float VignetteStrength = 0.85;

        void main()
        {
            vec3 color = texture(uScene, vUV).rgb;

            // Sivo: boja se mesa sa svojom osvetljenoscu.
            float luma = dot(color, vec3(0.299, 0.587, 0.114));
            color = mix(color, vec3(luma), uGray);

            // Crvena vinjeta: ivice kadra tonu u crveno.
            float distanceFromCenter = length((vUV - 0.5) * 2.0);
            float edge = smoothstep(VignetteInner, VignetteOuter, distanceFromCenter);
            color = mix(color, uVignetteColor, edge * uVignette * VignetteStrength);

            // Sjaj: meka svetla mrlja oko mesta spasenog pacijenta (Gausova kriva).
            vec2 offset = vec2(vUV.x - uGlowCenter.x, (vUV.y - uGlowCenter.y) / uAspect);
            float falloff = exp(-dot(offset, offset) / (uGlowRadius * uGlowRadius));
            color += uGlowColor * falloff * uGlow;

            FragColor = vec4(color, 1.0);
        }
        """;
}