namespace DispatchCity.Rendering;

// Shader za uvodni ekran: nebo, oblaci, trava i sina po kojoj idu pacijenti.
// Sve se racuna u fragment shaderu iz nekoliko brojeva (nema nikakvih slika).
// Napomena: u GLSL kodu nema slova sa dijakriticima, jer ih drajveri ne podnose.
public static class MenuShaders
{
    public const string Vertex = """
        #version 330 core
        layout(location = 0) in vec2 aPos;
        out vec2 vUV;
        void main()
        {
            vUV = aPos * 0.5 + 0.5;
            gl_Position = vec4(aPos, 0.0, 1.0);
        }
        """;

    public const string Fragment = """
        #version 330 core
        in vec2 vUV;
        uniform float uAspect;
        uniform float uTime;
        uniform float uHorizon;
        uniform float uTrackY;
        uniform float uTrackHalfHeight;
        uniform float uTrackScroll;
        uniform float uCloudDrift;
        uniform vec3 uSkyTop;
        uniform vec3 uSkyBottom;
        uniform vec3 uGrassNear;
        uniform vec3 uGrassFar;
        uniform vec3 uTrackColor;
        uniform vec3 uTrackEdge;
        uniform vec3 uCloudColor;
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
            return mix(mix(hash(cell), hash(cell + vec2(1.0, 0.0)), f.x),
                       mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0, 1.0)), f.x), f.y);
        }

        // Meko spajanje dva oblika (smooth minimum), da se krugovi oblaka slivaju u jedan.
        float smoothMin(float a, float b, float k)
        {
            float h = max(k - abs(a - b), 0.0) / k;
            return min(a, b) - h * h * k * 0.25;
        }

        // Rastojanje do oblaka (negativno unutra): tri kruga na ravnoj osnovi.
        float cloudDistance(vec2 p, vec2 center, float size)
        {
            vec2 q = (p - center) / size;
            float body = length(q - vec2(0.0, 0.15)) - 0.55;
            body = smoothMin(body, length(q - vec2(-0.55, 0.0)) - 0.4, 0.4);
            body = smoothMin(body, length(q - vec2(0.55, 0.0)) - 0.42, 0.4);
            float flatBase = q.y + 0.35;
            return max(body, -flatBase) * size;
        }

        // Oblak se polako vuce preko neba i vraca s druge strane (x je u visini ekrana).
        float cloudAlpha(vec2 p, float baseX, float y, float size, float speedFactor)
        {
            float span = uAspect + 2.0 * size * 2.0;
            float x = mod(baseX + uCloudDrift * speedFactor + size * 2.0, span) - size * 2.0;
            float d = cloudDistance(p, vec2(x, y), size);
            return 1.0 - smoothstep(0.0, 0.01, d);
        }

        void main()
        {
            vec2 p = vec2(vUV.x * uAspect, vUV.y);
            vec3 color;

            if (vUV.y > uHorizon)
            {
                // Nebo: prelaz od tamnije plave gore do blede kod horizonta.
                float t = (vUV.y - uHorizon) / (1.0 - uHorizon);
                color = mix(uSkyBottom, uSkyTop, t);

                float cloud = max(cloudAlpha(p, 0.30, 0.78, 0.10, 1.0),
                                  cloudAlpha(p, 1.05, 0.64, 0.07, 0.7));
                color = mix(color, uCloudColor, cloud);
            }
            else
            {
                // Trava: svetlija uz horizont, tamnija ka dnu, sa krupnim sumom i prugama kao pokosena.
                float t = (uHorizon - vUV.y) / uHorizon;
                color = mix(uGrassFar, uGrassNear, t);
                float stripes = 0.5 + 0.5 * sin(p.x * 18.0 + t * 6.0);
                float patches = valueNoise(p * 14.0);
                color *= 0.94 + 0.04 * stripes + 0.04 * patches;
            }

            // Sina: traka sa tamnim ivicama i poprecnim letvicama koje se krecu zajedno sa figurama.
            float fromCenter = abs(vUV.y - uTrackY);
            if (fromCenter < uTrackHalfHeight)
            {
                float slat = step(0.5, fract(p.x * 14.0 + uTrackScroll));
                vec3 body = mix(uTrackColor, uTrackColor * 0.88, slat);
                float edge = smoothstep(uTrackHalfHeight * 0.65, uTrackHalfHeight, fromCenter);
                color = mix(body, uTrackEdge, edge);
            }

            FragColor = vec4(color, 1.0);
        }
        """;
}