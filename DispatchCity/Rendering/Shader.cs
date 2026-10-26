using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Shader je mali program koji se izvršava na grafičkoj kartici i kaže kako se crta.
public class Shader
{
    public int Handle { get; private set; }

    public Shader(string vertexSource, string fragmentSource)
    {
        int vertex = Compile(ShaderType.VertexShader, vertexSource);
        int fragment = Compile(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertex);
        GL.AttachShader(Handle, fragment);
        GL.LinkProgram(Handle);

        int status;
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out status);
        if (status == 0)
        {
            throw new Exception("Greska pri povezivanju shadera: " + GL.GetProgramInfoLog(Handle));
        }

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
    }

    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public void SetVector2(string name, float x, float y)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform2(location, x, y);
    }

    public void SetColor(string name, float r, float g, float b, float a)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform4(location, r, g, b, a);
    }

    private int Compile(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        int status;
        GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
        if (status == 0)
        {
            throw new Exception("Greska pri kompajliranju shadera: " + GL.GetShaderInfoLog(shader));
        }
        return shader;
    }
}