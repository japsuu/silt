using System.Globalization;
using Serilog;

namespace Silt.Metrics;

public readonly struct BenchmarkConfig(string outputFilePath, Action? onComplete, int warmUpFrameCount = 20_000, int sampleFrameCount = 100_000)
{
    public readonly string OutputFilePath = outputFilePath;
    public readonly Action? OnComplete = onComplete;
    public readonly int WarmUpFrameCount = warmUpFrameCount;
    public readonly int SampleFrameCount = sampleFrameCount;
}

public class BenchmarkRun
{
    public BenchmarkConfig Config { get; set; }
    public bool IsComplete => SampleFrameCount >= Config.SampleFrameCount;
    public bool IsWarmingUp => WarmUpFrameCount < Config.WarmUpFrameCount;
    public double FrameMsAvg { get; private set; }
    public double FrameMsMin { get; private set; }
    public double FrameMsMax { get; private set; }
    public double TotalTimeMs { get; private set; }
    public int WarmUpFrameCount { get; private set; }
    public int SampleFrameCount { get; private set; }

    // Preallocated storage for benchmark samples and scratch for percentile.
    private readonly double[] _samplesMs;
    private readonly double[] _scratchMs;


    public BenchmarkRun(BenchmarkConfig config)
    {
        Config = config;
        FrameMsAvg = 0;
        FrameMsMin = double.MaxValue;
        FrameMsMax = double.MinValue;
        TotalTimeMs = 0;
        WarmUpFrameCount = 0;
        SampleFrameCount = 0;

        _samplesMs = new double[Math.Max(1, config.SampleFrameCount)];
        _scratchMs = new double[_samplesMs.Length];
    }


    public void Update(double frameMs)
    {
        if (IsWarmingUp)
        {
            WarmUpFrameCount++;
            return;
        }

        if (IsComplete)
            return;

        // Record sample
        _samplesMs[SampleFrameCount] = frameMs;
        TotalTimeMs += frameMs;
        SampleFrameCount++;

        // Update aggregates
        FrameMsMin = Math.Min(FrameMsMin, frameMs);
        FrameMsMax = Math.Max(FrameMsMax, frameMs);
        FrameMsAvg = SampleFrameCount > 0 ? TotalTimeMs / SampleFrameCount : 0;

        if (IsComplete)
        {
            double frameMsP99 = PercentileHelper.P99FromSamples(_samplesMs, _scratchMs, SampleFrameCount);
            
            string output = $"mode=benchmark\n" +
                            $"warmup_frames={Config.WarmUpFrameCount}\n" +
                            $"sample_frames={Config.SampleFrameCount}\n" +
                            $"frame_ms_avg={FrameMsAvg.ToString("F4", CultureInfo.InvariantCulture)}\n" +
                            $"frame_ms_min={FrameMsMin.ToString("F4", CultureInfo.InvariantCulture)}\n" +
                            $"frame_ms_max={FrameMsMax.ToString("F4", CultureInfo.InvariantCulture)}\n" +
                            $"frame_ms_p99={frameMsP99.ToString("F4", CultureInfo.InvariantCulture)}\n" +
                            $"total_time_ms={TotalTimeMs.ToString("F4", CultureInfo.InvariantCulture)}\n";

            File.WriteAllText(Config.OutputFilePath, output);
            string fullPath = Path.GetFullPath(Config.OutputFilePath);
            Log.Information("Benchmark complete. Results written to {OutputFilePath}", fullPath);
            Config.OnComplete?.Invoke();
        }
    }
}