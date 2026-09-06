using System;
using System.Collections.Generic;

namespace Smile.Language;

public enum SmileWebQuality { Full, Low, Medium, High }

// Shared project/deployment vocabulary; this does not change SMILE value types.
public static class SmileWebDeployment
{
    public static IReadOnlyList<string> Platforms { get; } = Array.AsReadOnly(new[]
    {
        "Web", "Web - Optimized Low", "Web - Optimized Medium", "Web - Optimized High"
    });

    public static bool TryParseQuality(string value, out SmileWebQuality quality)
    {
        foreach (SmileWebQuality candidate in Enum.GetValues(typeof(SmileWebQuality)))
        {
            if (string.Equals(value, candidate.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                quality = candidate;
                return true;
            }
        }
        quality = SmileWebQuality.Full;
        return false;
    }

    public static bool TryGetQuality(string platform, out SmileWebQuality quality)
    {
        for (var index = 0; index < Platforms.Count; index++)
        {
            if (string.Equals(platform, Platforms[index], StringComparison.OrdinalIgnoreCase))
            {
                quality = (SmileWebQuality)index;
                return true;
            }
        }
        quality = SmileWebQuality.Full;
        return false;
    }

    public static string OutputFolder(SmileWebQuality quality)
    {
        if (!Enum.IsDefined(typeof(SmileWebQuality), quality))
            throw new ArgumentOutOfRangeException(nameof(quality));
        return Platforms[(int)quality];
    }
}
