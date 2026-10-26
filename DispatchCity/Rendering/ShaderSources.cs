namespace DispatchCity.Rendering;

// GLSL kod programa koji se izvršavaju na grafičkoj kartici.
public static class ShaderSources
{
    // Običan shader: vrhovi već imaju svoju svetsku poziciju i boju (ulice).
    public const string FlatVertex =
        "#version 330 core\n" +
        "layout(location = 0) in vec2 aPos;\n" +
        "layout(location = 1) in vec3 aColor;\n" +
        "uniform mat4 uViewProj;\n" +
        "out vec3 vColor;\n" +
        "void main()\n" +
        "{\n" +
        "    gl_Position = uViewProj * vec4(aPos, 0.0, 1.0);\n" +
        "    vColor = aColor;\n" +
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