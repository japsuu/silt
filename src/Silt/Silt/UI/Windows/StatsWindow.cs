using ImGuiNET;
using Silt.Platform;

namespace Silt.UI.Windows;

public sealed class StatsWindow : IUiWindow
{
    public string Title => "Stats";
    public ImGuiWindowFlags Flags => ImGuiWindowFlags.None;
    public bool IsOpen { get; set; } = true;

    private float _smoothedFps;
    private float _smoothedTps;


    public void Initialize()
    {
        _smoothedFps = 0;
        _smoothedTps = 0;
    }


    public void Update(double deltaTime)
    {
        if (deltaTime > 0)
            _smoothedTps = CheapExponentialSmooth(_smoothedTps, (float)(1.0 / deltaTime), 0.1f);
    }


    public void Draw(double deltaTime)
    {
        if (deltaTime > 0)
            _smoothedFps = CheapExponentialSmooth(_smoothedFps, (float)(1.0 / deltaTime), 0.1f);
        
        ImGui.Text($"FPS (smoothed): {_smoothedFps:F1}");
        ImGui.Text($"TPS (smoothed): {_smoothedTps:F1}");
        ImGui.Separator();
        ImGui.Text($"Processors: {SystemInfo.ProcessorCount}");
        ImGui.Text($"Main thread id: {SystemInfo.MainThreadId}");
    }
    
    
    private static float CheapExponentialSmooth(float previous, float current, float alpha)
    {
        return previous * (1 - alpha) + current * alpha;
    }
}