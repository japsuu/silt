using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Silt.CameraManagement;
using Silt.Graphics;
using Silt.InputManagement;
using Silt.Platform;
using Silt.UI;
using Silt.UI.Windows;
using Silt.Utils;
using Shader = Silt.Graphics.Shader;
using Texture = Silt.Graphics.Texture;

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
    private static Texture _texture = null!;
    private static Shader _shader = null!;
    private static readonly Transform[] _transforms = new Transform[3];

    private static ImGuiController _imgui = null!;
    private static UiManager _ui = null!;

    private static readonly float[] _cubeVertices =
    [
        // X, Y, Z, U, V
        // Z+ (back)
        -0.5f, -0.5f, 0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, 0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, 0.5f, 0.5f, 0.0f, 1.0f,

        // Z- (front)
        0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
        -0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
        0.5f, 0.5f, -0.5f, 0.0f, 1.0f,

        // X+ (right)
        0.5f, -0.5f, 0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
        0.5f, 0.5f, 0.5f, 0.0f, 1.0f,

        // X- (left)
        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, 0.5f, 1.0f, 0.0f,
        -0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, 0.5f, -0.5f, 0.0f, 1.0f,

        // Y+ (top)
        -0.5f, 0.5f, 0.5f, 0.0f, 0.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
        -0.5f, 0.5f, -0.5f, 0.0f, 1.0f,

        // Y- (bottom)
        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
        0.5f, -0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, -0.5f, 0.5f, 0.0f, 1.0f
    ];

    private static readonly uint[] _cubeIndices =
    [
        // Z+
        0, 1, 2,
        0, 2, 3,

        // Z-
        4, 5, 6,
        4, 6, 7,

        // X+
        8, 9, 10,
        8, 10, 11,

        // X-
        12, 13, 14,
        12, 14, 15,

        // Y+
        16, 17, 18,
        16, 18, 19,

        // Y-
        20, 21, 22,
        20, 22, 23
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
        _gl.Enable(GLEnum.DepthTest);

        // Setup platform info
        SystemInfo.Initialize();
        WindowInfo.Initialize(_window);

        // Setup input
        IInputContext input = _window.CreateInput();
        Input.Initialize(input);

        // Setup ImGui + UI
        _imgui = new ImGuiController(_gl, _window, input, () =>
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        });
        _ui = new UiManager();
        _ui.Register(new StatsWindow());
        _ui.Initialize();

        // Create buffers
        _vbo = new BufferObject<float>(_gl, _cubeVertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _cubeIndices, BufferTargetARB.ElementArrayBuffer);

        // Create VAO
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        _vao.SetVertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.SetVertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);

        // Create shader
        _shader = new Shader(_gl, "base", "assets/base.vert", "assets/base.frag");

        // Create texture
        _texture = new Texture(_gl, "assets/tex_proto_wall.png");

        // Cleanup
        _vao.Unbind();
        _vbo.Unbind();
        _ebo.Unbind();

        // Setup camera
        CameraManager.Initialize(new Camera(new Vector3(0f, 0f, 3f)), new FreeCameraController());

        // Translation
        _transforms[0] = new Transform();
        _transforms[0].Position = new Vector3(0.5f, 0.5f, 0f);

        // Scaling
        _transforms[1] = new Transform();
        _transforms[1].Scale = 0.5f;

        // Mixed transformation
        _transforms[2] = new Transform();
        _transforms[2].Position = new Vector3(-0.5f, 0.5f, 0f);
        _transforms[2].Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1f);
        _transforms[2].Scale = 0.5f;
    }


    private static void OnUpdate(double deltaTime)
    {
        foreach (Transform t in _transforms)
        {
            // Convert time to radians for rotation
            float rotDegrees = (float)(_window.Time * 100);
            t.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathUtil.DegreesToRadians(rotDegrees)) *
                         Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathUtil.DegreesToRadians(rotDegrees * 0.6f));
        }

        if (Input.GetKeyHoldTime(Key.Escape) > 3)
            _window.Close();

        if (Input.WasKeyPressed(Key.F1))
            UiManager.ToggleUiVisibility();

        _ui.Update(deltaTime);

        CameraManager.Update(deltaTime);
        Input.Update(deltaTime);
    }


    private static unsafe void OnRender(double deltaTime)
    {
        _imgui.Update((float)deltaTime);
        _ui.Draw(deltaTime);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _vao.Bind();

        _shader.Use();
        _shader.SetUniform("u_texture", _texture, TextureUnit.Texture0);

        Matrix4x4 view = CameraManager.MainCamera.GetViewMatrix();
        Matrix4x4 projection = CameraManager.MainCamera.GetProjectionMatrix();

        foreach (Transform t in _transforms)
        {
            Matrix4x4 mv = t.ModelMatrix * view;
            Matrix4x4 mvp = mv * projection;
            _shader.SetUniform("u_mat_m", t.ModelMatrix);
            _shader.SetUniform("u_mat_mv", mv);
            _shader.SetUniform("u_mat_mvp", mvp);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_cubeIndices.Length, DrawElementsType.UnsignedInt, null);
        }

        // Render ImGui on top of the scene.
        _imgui.Render();
    }


    private static void OnClose()
    {
        _ui.Dispose();
        _imgui.Dispose();

        _shader.Dispose();
        _texture.Dispose();
        _vbo.Dispose();
        _ebo.Dispose();
        _vao.Dispose();
    }


    private static void OnFramebufferResize(Vector2D<int> newSize)
    {
        // Keep GL viewport in sync with the framebuffer.
        _gl.Viewport(newSize);
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
            VSync = false,
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