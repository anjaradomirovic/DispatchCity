namespace DispatchCity.Rendering;

// GLSL kod programa koji se izvršavaju na grafičkoj kartici.
public static class ShaderSources
{
    // Shader za ulice. Geometrija je statična, a ono što se menja (toplotna mapa i gužva)
    // GPU čita iz dve male teksture: vrelina po raskrsnici i gužva po ulici.
    // Svaki vrh zna samo "svoju" raskrsnicu (aNode) i "svoju" ulicu (aEdge), a vrednosti
    // se očitavaju (texelFetch) i interpoliše ih GPU duž ulice. -1 znači "ne učestvuje".
    public const string FlatVertex =
        "#version 330 core\n" +
        "layout(location = 0) in vec2 aPos;\n" +
        "layout(location = 1) in vec3 aColor;\n" +
        "layout(location = 2) in float aNode;\n" +
        "layout(location = 3) in float aEdge;\n" +
        "uniform mat4 uViewProj;\n" +
        "uniform sampler1D uHeatTex;\n" +
        "uniform sampler1D uCongestionTex;\n" +
        "out vec3 vColor;\n" +
        "out float vHeat;\n" +
        "out float vCongestion;\n" +
        "void main()\n" +
        "{\n" +
        "    gl_Position = uViewProj * vec4(aPos, 0.0, 1.0);\n" +
        "    vColor = aColor;\n" +
        "    vHeat = aNode >= 0.0 ? texelFetch(uHeatTex, int(aNode + 0.5), 0).r : -1.0;\n" +
        "    vCongestion = aEdge >= 0.0 ? texelFetch(uCongestionTex, int(aEdge + 0.5), 0).r : 0.0;\n" +
        "}\n";

    // Boju za svaki piksel određuje ovaj shader: osnovna boja ulice, ili (kad je uključena)
    // gužva koja je boji ka bledožutoj i narandžastoj, ili toplotna mapa (rampa od tri boje).
    // Boje rampi šalje CPU kao uniforme, a mešanje radi GPU.
    public const string RoadFragment =
        "#version 330 core\n" +
        "in vec3 vColor;\n" +
        "in float vHeat;\n" +
        "in float vCongestion;\n" +
        "uniform float uHeatMode;\n" +
        "uniform float uTrafficMode;\n" +
        "uniform vec3 uNear;\n" +
        "uniform vec3 uMid;\n" +
        "uniform vec3 uFar;\n" +
        "uniform vec3 uJamMid;\n" +
        "uniform vec3 uJamHigh;\n" +
        "out vec4 FragColor;\n" +
        "vec3 heatRamp(float t)\n" +
        "{\n" +
        "    return t < 0.5 ? mix(uNear, uMid, t * 2.0) : mix(uMid, uFar, (t - 0.5) * 2.0);\n" +
        "}\n" +
        "vec3 jamRamp(vec3 baseColor, float c)\n" +
        "{\n" +
        "    return c < 0.5 ? mix(baseColor, uJamMid, c * 2.0) : mix(uJamMid, uJamHigh, (c - 0.5) * 2.0);\n" +
        "}\n" +
        "void main()\n" +
        "{\n" +
        "    vec3 color = vColor;\n" +
        "    if (uHeatMode > 0.5 && vHeat >= 0.0)\n" +
        "    {\n" +
        "        color = heatRamp(clamp(vHeat, 0.0, 1.0));\n" +
        "    }\n" +
        "    else if (uTrafficMode > 0.5)\n" +
        "    {\n" +
        "        color = jamRamp(vColor, clamp(vCongestion, 0.0, 1.0));\n" +
        "    }\n" +
        "    FragColor = vec4(color, 1.0);\n" +
        "}\n";

    // Instanciranje: jedan mali krug (aCorner) se crta mnogo puta u jednom pozivu.
    // Svaka instanca ima svoj centar, poluprečnik i boju, pa GPU sam pomera i skalira krug.
    public const string InstancedVertex =
        "#version 330 core\n" +
        "layout(location = 0) in vec2 aCorner;\n" +
        "layout(location = 1) in vec3 aInstance;\n" +   // x, y, poluprečnik
        "layout(location = 2) in vec3 aColor;\n" +
        "uniform mat4 uViewProj;\n" +
        "out vec3 vColor;\n" +
        "void main()\n" +
        "{\n" +
        "    vec2 world = aInstance.xy + aCorner * aInstance.z;\n" +
        "    gl_Position = uViewProj * vec4(world, 0.0, 1.0);\n" +
        "    vColor = aColor;\n" +
        "}\n";

    public const string Fragment =
        "#version 330 core\n" +
        "in vec3 vColor;\n" +
        "out vec4 FragColor;\n" +
        "void main()\n" +
        "{\n" +
        "    FragColor = vec4(vColor, 1.0);\n" +
        "}\n";
}