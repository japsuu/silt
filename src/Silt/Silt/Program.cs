using System.Drawing;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Silt;

using Serilog;
using Serilog.Events;

internal static class Program
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static uint _vao;
    private static uint _vbo;
    private static uint _ebo;
    private static uint _program;

    private static readonly float[] QuadVertices =
    [
        // Top-right
        0.5f, 0.5f, 0.0f,

        // Bottom-right
        0.5f, -0.5f, 0.0f,

        // Bottom-left
        -0.5f, -0.5f, 0.0f,

        // Top-left
        -0.5f, 0.5f, 0.0f
    ];

    private static readonly uint[] QuadIndices =
    [
        0, 1, 3,
        1, 2, 3
    ];

    private const string VERTEX_SOURCE = @"
#version 330 core

layout (location = 0) in vec3 aPosition;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
}";

    private const string FRAGMENT_SOURCE = @"
#version 330 core

out vec4 out_color;

void main()
{
    out_color = vec4(1.0, 0.5, 0.2, 1.0);
}";


    private static void Main(string[] args)
    {
        SetupLogging();

        try
        {
            Log.Information("Starting Silt engine...");
            Log.Debug("Args: {Args}", args);

            _window = CreateWindow();
            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.FramebufferResize += OnFramebufferResize;

            _window.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Silt terminated unexpectedly");
        }
        finally
        {
            _window.Dispose();
            Log.CloseAndFlush();
        }
    }


    private static unsafe void OnLoad()
    {
        Log.Information("Window loaded");

        // Setup OpenGL
        _gl = _window.CreateOpenGL();
#if DEBUG
        SetupOpenGlLogging(_gl);
#endif
        _gl.ClearColor(Color.Magenta);

        // Setup input
        IInputContext input = _window.CreateInput();
        foreach (IKeyboard k in input.Keyboards)
            k.KeyDown += KeyDown;

        // Setup VAO
        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        // Setup VBO
        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        // Upload vertex data
        fixed (float* buf = QuadVertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(QuadVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }
        
        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        // Upload index data
        fixed (uint* buf = QuadIndices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(QuadIndices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }
        
        // Setup vertex shader
        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, VERTEX_SOURCE);
        
        _gl.CompileShader(vertexShader);

        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int) GLEnum.True)
            throw new Exception("Vertex shader failed to compile: " + _gl.GetShaderInfoLog(vertexShader));
        
        // Setup fragment shader
        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, FRAGMENT_SOURCE);

        _gl.CompileShader(fragmentShader);

        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus != (int) GLEnum.True)
            throw new Exception("Fragment shader failed to compile: " + _gl.GetShaderInfoLog(fragmentShader));
        
        _program = _gl.CreateProgram();
        
        _gl.AttachShader(_program, vertexShader);
        _gl.AttachShader(_program, fragmentShader);

        _gl.LinkProgram(_program);

        _gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int) GLEnum.True)
            throw new Exception("Program failed to link: " + _gl.GetProgramInfoLog(_program));
        
        _gl.DetachShader(_program, vertexShader);
        _gl.DetachShader(_program, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
        
        const uint positionLoc = 0;
        _gl.EnableVertexAttribArray(positionLoc);
        _gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*) 0);
        
        // Cleanup bindings
        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }


    private static void OnUpdate(double deltaTime)
    {
    }


    private static unsafe void OnRender(double deltaTime)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        
        _gl.BindVertexArray(_vao);
        _gl.UseProgram(_program);
        _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*) 0);
    }
    

    private static void OnFramebufferResize(Vector2D<int> newSize)
    {
        _gl.Viewport(newSize);
    }


    private static void KeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
            _window.Close();
    }


    private static IWindow CreateWindow()
    {
        ContextFlags flags = ContextFlags.ForwardCompatible;
#if DEBUG
        flags |= ContextFlags.Debug;
#endif
        WindowOptions options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(720, 720),
            Title = "Silt",
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, flags, new APIVersion(3, 3))
        };

        return Window.Create(options);
    }


    private static unsafe void SetupOpenGlLogging(GL gl)
    {
        // Enable debug output
        gl.Enable(GLEnum.DebugOutput);
        gl.Enable(GLEnum.DebugOutputSynchronous);

        // Filter noise (notifications etc.)
        gl.DebugMessageControl(
            GLEnum.DontCare,
            GLEnum.DontCare,
            GLEnum.DebugSeverityNotification,
            0,
            null,
            false);

        // Register debug callback
        gl.DebugMessageCallback(
            (
                GLEnum source,
                GLEnum type,
                int id,
                GLEnum severity,
                int length,
                nint message,
                nint userParam) =>
            {
                string msg = SilkMarshal.PtrToString(message, NativeStringEncoding.UTF8) ?? string.Empty;

                // Map severity to Serilog levels
                LogEventLevel level = severity switch
                {
                    GLEnum.DebugSeverityHigh => LogEventLevel.Error,
                    GLEnum.DebugSeverityMedium => LogEventLevel.Warning,
                    GLEnum.DebugSeverityLow => LogEventLevel.Information,
                    _ => LogEventLevel.Debug
                };

                string src = source.ToString();
                string typ = type.ToString();

                Log.Write(
                    level,
                    "OpenGL [{Source}] [{Type}] Id={Id}: {Message}",
                    src,
                    typ,
                    id,
                    msg);
            },
            0);
    }


    private static void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}