namespace Silt.Metrics;

internal sealed class FrameTimeRingBuffer
{
    private readonly double[] _samplesMs;
    private readonly double[] _scratchMs;

    private int _writeIndex;

    private double _sumMs;
    private double _minMs;
    private double _maxMs;


    public FrameTimeRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _samplesMs = new double[capacity];
        _scratchMs = new double[capacity];
        Reset();
    }


    public int Count { get; private set; }

    public int Capacity => _samplesMs.Length;
    public double MinMs => Count > 0 ? _minMs : 0;
    public double MaxMs => Count > 0 ? _maxMs : 0;
    public double AvgMs => Count > 0 ? _sumMs / Count : 0;


    public void Reset()
    {
        _writeIndex = 0;
        Count = 0;
        _sumMs = 0;
        _minMs = double.MaxValue;
        _maxMs = double.MinValue;
    }


    public void Add(double frameMs)
    {
        // Keep invariants even if called with NaN/Inf.
        if (double.IsNaN(frameMs) || double.IsInfinity(frameMs) || frameMs <= 0)
            return;

        if (Count < _samplesMs.Length)
        {
            _samplesMs[_writeIndex] = frameMs;
            _sumMs += frameMs;
            _minMs = Math.Min(_minMs, frameMs);
            _maxMs = Math.Max(_maxMs, frameMs);
            Count++;
            _writeIndex = (_writeIndex + 1) % _samplesMs.Length;
            return;
        }

        // Buffer full: overwrite oldest, keep rolling sum.
        double overwritten = _samplesMs[_writeIndex];
        _samplesMs[_writeIndex] = frameMs;
        _sumMs += frameMs - overwritten;
        _writeIndex = (_writeIndex + 1) % _samplesMs.Length;

        // Min/max might have been overwritten, recompute lazily if necessary.
        if (frameMs < _minMs)
            _minMs = frameMs;
        if (frameMs > _maxMs)
            _maxMs = frameMs;
        if (Math.Abs(overwritten - _minMs) < 0.1d || Math.Abs(overwritten - _maxMs) < 0.1d)
            RecomputeMinMax();
    }


    public double ComputeP99()
    {
        int count = Count;
        if (count <= 0)
            return 0;

        // Linearize ring into scratch, oldest -> newest.
        int capacity = _samplesMs.Length;
        int start = Count < capacity ? 0 : _writeIndex; // _writeIndex points to oldest when full.
        for (int i = 0; i < count; i++)
        {
            int idx = (start + i) % capacity;
            _scratchMs[i] = _samplesMs[idx];
        }

        return PercentileHelper.P99InPlace(_scratchMs, count);
    }


    private void RecomputeMinMax()
    {
        if (Count <= 0)
        {
            _minMs = double.MaxValue;
            _maxMs = double.MinValue;
            return;
        }

        double min = double.MaxValue;
        double max = double.MinValue;
        int capacity = _samplesMs.Length;
        int start = Count < capacity ? 0 : _writeIndex;
        for (int i = 0; i < Count; i++)
        {
            double v = _samplesMs[(start + i) % capacity];
            min = Math.Min(min, v);
            max = Math.Max(max, v);
        }

        _minMs = min;
        _maxMs = max;
    }
}