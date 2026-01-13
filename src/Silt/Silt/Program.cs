using System.Drawing;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silt.Graphics;
using Shader = Silt.Graphics.Shader;

namespace Silt;

using Serilog;
using Serilog.Events;

internal static class Program
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static VertexArrayObject<float, uint> _vao = null!;
    private static BufferObject<float> _vbo = null!;
    private static BufferObject<uint> _ebo = null!;
    private static Shader _shader = null!;

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
            _window.Closing += OnClose;

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


    private static void OnLoad()
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

        // Create shader
        _shader = new Shader(_gl, "assets/base.vert", "assets/base.frag");

        // Create buffers
        _vbo = new BufferObject<float>(_gl, QuadVertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, QuadIndices, BufferTargetARB.ElementArrayBuffer);

        // Create VAO
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        _vao.SetVertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
        
        // Cleanup
        _vao.Unbind();
        _vbo.Unbind();
        _ebo.Unbind();
    }


    private static void OnUpdate(double deltaTime)
    {
    }


    private static unsafe void OnRender(double deltaTime)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        
        _shader.Use();
        _vao.Bind();
        _gl.DrawElements(PrimitiveType.Triangles, _vao.IndexCount, DrawElementsType.UnsignedInt, (void*) 0);
    }
    

    private static void OnFramebufferResize(Vector2D<int> newSize)
    {
        _gl.Viewport(newSize);
    }

    private static void OnClose()
    {
        _shader.Dispose();
        _vbo.Dispose();
        _ebo.Dispose();
        _vao.Dispose();
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
                source,
                type,
                id,
                severity,
                length,
                message,
                userParam) =>
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