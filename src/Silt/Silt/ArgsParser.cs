using System.Globalization;

namespace Silt;

/// <summary>
/// Parses command line args into <see cref="AppOptions"/>.
/// Supports:
/// - Flag presence: --benchmark
/// - Key/value: --benchmark-out &lt;path&gt; OR --benchmark-out=&lt;path&gt;
/// - Numeric: --benchmark-warmup &lt;int&gt;, --benchmark-samples &lt;int&gt;
///
/// Unknown args are preserved in <see cref="AppOptions.RemainingArgs"/>.
/// </summary>
public static class ArgsParser
{
    public static AppOptions Parse(string[] args)
    {
        bool benchmark = false;
        string? outPath = null;
        int warmup = 5_000;
        int samples = 20_000;
        List<string> remaining = new(args.Length);

        for (int i = 0; i < args.Length; i++)
        {
            if (TryConsumeKnown(args, ref i, ref benchmark, ref outPath, ref warmup, ref samples))
                continue;

            remaining.Add(args[i]);
        }

        return new AppOptions
        {
            BenchmarkEnabled = benchmark,
            BenchmarkOutputFilePath = outPath,
            BenchmarkWarmUpFrameCount = warmup,
            BenchmarkSampleFrameCount = samples,
            RemainingArgs = remaining.ToArray()
        };
    }


    private static bool TryConsumeKnown(
        string[] args,
        ref int i,
        ref bool benchmark,
        ref string? outPath,
        ref int warmup,
        ref int samples)
    {
        string a = args[i];

        if (IsFlag(a, "--benchmark"))
        {
            benchmark = true;
            return true;
        }

        if (TryReadStringOption(args, ref i, "--benchmark-out", out string? s))
        {
            outPath = s;
            return true;
        }

        if (TryReadIntOption(args, ref i, "--benchmark-warmup", out int w))
        {
            warmup = w;
            if (warmup < 0)
                warmup = 0;
            return true;
        }

        if (TryReadIntOption(args, ref i, "--benchmark-samples", out int n))
        {
            samples = n;
            if (samples < 1)
                samples = 1;
            return true;
        }

        return false;
    }


    private static bool IsFlag(string arg, string flag) => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase);


    private static bool TryReadStringOption(string[] args, ref int i, string name, out string? value)
    {
        string a = args[i];
        if (TrySplitEqualsOption(a, name, out string? eqValue))
        {
            value = eqValue;
            return true;
        }

        if (!string.Equals(a, name, StringComparison.OrdinalIgnoreCase))
        {
            value = null;
            return false;
        }

        if (i + 1 >= args.Length)
            throw new ArgumentException($"Missing value for {name}");

        value = args[++i];
        return true;
    }


    private static bool TryReadIntOption(string[] args, ref int i, string name, out int value)
    {
        if (TryReadStringOption(args, ref i, name, out string? s))
        {
            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                throw new ArgumentException($"Invalid integer for {name}: '{s}'");
            return true;
        }

        value = 0;
        return false;
    }


    private static bool TrySplitEqualsOption(string arg, string name, out string? value)
    {
        value = null;

        // --opt=value
        if (!arg.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            return false;

        if (arg.Length == name.Length)
            return false;

        if (arg[name.Length] != '=')
            return false;

        value = arg[(name.Length + 1)..];
        return true;
    }
}