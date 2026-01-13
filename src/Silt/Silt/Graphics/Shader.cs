using System.Text;
using Silk.NET.OpenGL;

namespace Silt.Graphics;

public class ShaderCompilationException(string message) : Exception(message);
public class ShaderLinkingException(string message) : Exception(message);

/// <summary>
/// Represents a shader program.
/// Handles loading, compiling, and linking of vertex and fragment shaders.
/// </summary>
public sealed class Shader : GraphicsResource
{
    /// <summary>
    /// Creates a new shader program from the specified vertex and fragment shader file paths.
    /// </summary>
    /// <param name="gl">The OpenGL context.</param>
    /// <param name="vertexPath">The file path to the vertex shader source.</param>
    /// <param name="fragmentPath">The file path to the fragment shader source.</param>
    /// <exception cref="FileNotFoundException">Thrown if a shader file cannot be found.</exception>
    /// <exception cref="ShaderCompilationException">Thrown if a shader fails to compile.</exception>
    /// <exception cref="ShaderLinkingException">Thrown if the shader program fails to link.</exception>
    public Shader(GL gl, string vertexPath, string fragmentPath) : base(gl)
    {
        string vertexSource = LoadAndPreprocessShader(vertexPath);
        string fragmentSource = LoadAndPreprocessShader(fragmentPath);

        uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        Handle = Gl.CreateProgram();
        Gl.AttachShader(Handle, vertexShader);
        Gl.AttachShader(Handle, fragmentShader);
        Gl.LinkProgram(Handle);

        Gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out int status);
        if (status != (int)GLEnum.True)
        {
            string infoLog = Gl.GetProgramInfoLog(Handle);
            throw new ShaderLinkingException($"Failed to link shader program: {infoLog}");
        }

        // Shaders are now linked into the program so we can delete them.
        Gl.DetachShader(Handle, vertexShader);
        Gl.DetachShader(Handle, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);
    }


    /// <summary>
    /// Activates this shader program for use in rendering.
    /// </summary>
    public void Use()
    {
        Gl.UseProgram(Handle);
    }


    protected override void DisposeResources(bool manual)
    {
        Gl.DeleteProgram(Handle);
    }


    private static string LoadAndPreprocessShader(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Shader file not found: '{path}'");

        StringBuilder source = new();

        // Inject the GLSL version and common definitions.
        source.AppendLine("#version 330 core");
#if DEBUG
        source.AppendLine("#define DEBUG 1");
#endif
        source.AppendLine();

        source.Append(File.ReadAllText(path));

        return source.ToString();
    }


    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = Gl.CreateShader(type);
        Gl.ShaderSource(shader, source);
        Gl.CompileShader(shader);

        Gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status != (int)GLEnum.True)
        {
            string infoLog = Gl.GetShaderInfoLog(shader);
            Gl.DeleteShader(shader); // Don't leak the shader.
            throw new ShaderCompilationException($"Failed to compile {type} shader: {infoLog}");
        }

        return shader;
    }
}