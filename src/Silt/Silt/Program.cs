using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Silt;

using Serilog;
using Serilog.Events;

internal static class Program
{
    private static IWindow _window = null!;


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

            _window.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Silt terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }


    private static void OnLoad()
    {
        Log.Information("Window loaded");
        
        // Setup input
        IInputContext input = _window.CreateInput();
        foreach (IKeyboard k in input.Keyboards)
            k.KeyDown += KeyDown;
    }


    private static void OnUpdate(double deltaTime)
    {
    }


    private static void OnRender(double deltaTime)
    {
    }


    private static void KeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
            _window.Close();
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


    private static IWindow CreateWindow()
    {
        WindowOptions options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(720, 720),
            Title = "Silt"
        };

        return Window.Create(options);
    }
}