namespace Silt;

/// <summary>
/// Parsed application options from command line args.
/// </summary>
public sealed class AppOptions
{
    public bool BenchmarkEnabled { get; init; }
    public string? BenchmarkSceneId { get; init; }
    public string? BenchmarkOutputFilePath { get; init; }
    public int BenchmarkWarmUpFrameCount { get; init; }
    public int BenchmarkSampleFrameCount { get; init; }
}
