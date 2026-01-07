namespace Silt;

using Serilog;
using Serilog.Events;

internal static class Program
{
    private static void Main(string[] args)
    {
        SetupLogging();

        try
        {
            Log.Information("Starting Silt engine...");
            Log.Debug("Args: {Args}", args);
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