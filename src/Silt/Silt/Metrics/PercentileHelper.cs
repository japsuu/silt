namespace Silt.Metrics;

internal static class PercentileHelper
{
    /// <returns>p99 frame time in ms ("1% low FPS").</returns>
    /// <exception cref="ArgumentException">Thrown if count is negative, or if scratch buffer is too small.</exception>
    public static double P99FromSamples(double[] samples, double[] scratch, int count)
    {
        if (count <= 0)
            return 0;
        if (count > samples.Length)
            count = samples.Length;
        if (scratch.Length < count)
            throw new ArgumentException("Scratch buffer too small", nameof(scratch));

        Array.Copy(samples, scratch, count);
        return P99InPlace(scratch, count);
    }


    public static double P99InPlace(double[] data, int count)
    {
        if (count <= 0)
            return 0;

        Array.Sort(data, 0, count);

        int k = (int)Math.Ceiling(0.99 * count) - 1;
        if (k < 0)
            k = 0;
        if (k >= count)
            k = count - 1;
        return data[k];
    }
}