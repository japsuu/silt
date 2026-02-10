namespace Silt;

/// <summary>
/// Parsed application options from command line args.
/// </summary>
public sealed class AppOptions
{
    public bool BenchmarkEnabled { get; init; }
    public string? BenchmarkSceneId { get; init; }
    public string? BenchmarkOutputFilePath { get; init; }
    public int BenchmarkWarmUpFrameCount { get; init; } = 5_000;
    public int BenchmarkSampleFrameCount { get; init; } = 20_000;
}
