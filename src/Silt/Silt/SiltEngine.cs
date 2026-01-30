using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Serilog;
using Serilog.Events;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Silt.CameraManagement;
using Silt.InputManagement;
using Silt.Platform;
using Silt.UI;
using Silt.UI.Windows;

namespace Silt;

public sealed class SiltEngine
{
    private string[] _args = null!;
    private IWindow _window = null!;
    private GL _gl = null!;
    private ImGuiController _imguiController = null!;
    private UiManager _uiManager = null!;
    private Scene _currentScene = null!;
    private double _fixedFrameAccumulator;


    public void Run(string[] args)
    {
        try
        {
            _args = args;
            Log.Information("Starting Silt engine...");
            Log.Debug("Args: {Args}", _args);

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


    private void OnLoad()
    {
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
        _imguiController = new ImGuiController(_gl, _window, input, () =>
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        });
        _uiManager = new UiManager();
        _uiManager.Register(new StatsWindow());
        _uiManager.Initialize();

        // Setup camera
        CameraManager.Initialize(new Camera(new Vector3(0, 0, 0)));
        
        _currentScene = new TestScene(_gl, _window);
        _currentScene.Load();
    }


    private void OnUpdate(double deltaTime)
    {
        _fixedFrameAccumulator += deltaTime;
        
        while (_fixedFrameAccumulator >= SiltConstants.FIXED_DELTA_TIME)
        {
            InternalFixedUpdate(SiltConstants.FIXED_DELTA_TIME);
            _fixedFrameAccumulator -= SiltConstants.FIXED_DELTA_TIME;
        }
        
        InternalUpdate(deltaTime);
    }


    private void OnRender(double deltaTime)
    {
        InternalRender(deltaTime);
    }


    private void OnClose()
    {
        _uiManager.Dispose();
        _imguiController.Dispose();
        
        _currentScene.Unload();
    }


    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        // Keep GL viewport in sync with the framebuffer.
        _gl.Viewport(newSize);
    }


    private void InternalUpdate(double deltaTime)
    {
        _currentScene.Update(deltaTime);

        if (Input.GetKeyHoldTime(Key.Escape) > 3)
            _window.Close();

        if (Input.WasKeyPressed(Key.F1))
            UiManager.ToggleUiVisibility();

        _uiManager.Update(deltaTime);
        _imguiController.Update((float)deltaTime);

        CameraManager.Update(deltaTime);
        Input.Update(deltaTime);
    }


    private void InternalFixedUpdate(double fixedDeltaTime)
    {
        _currentScene.FixedUpdate(fixedDeltaTime);
    }


    private void InternalRender(double deltaTime)
    {
        _uiManager.Draw(deltaTime);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _currentScene.Render(deltaTime);

        // Render ImGui on top of the scene.
        _imguiController.Render();
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
            }, null);
    }
}