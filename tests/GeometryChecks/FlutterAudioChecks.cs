// Checks audible flutter onset, reef attenuation and invalid wind/deployment inputs.
using System;
using MoreSailwindSails.Sails.Audio;

namespace MoreSailwindSails.Tests.GeometryChecks;

internal static class FlutterAudioChecks
{
    internal static void Run()
    {
        foreach (float strength in new[] { -1f, 0f, 0.25f, 0.5f })
            Near(actual: Volume(strength: strength, deployment: 1), expected: 0);
        Near(actual: Volume(strength: 0.75f, deployment: 1), expected: 0.5f);
        Near(actual: Volume(strength: 1, deployment: 1), expected: 1);
        Near(actual: Volume(strength: 2, deployment: 2), expected: 1);
        foreach (float deployment in new[] { 0f, 0.1f, 0.25f, 0.5f, 1f })
        {
            float previous = 0;
            for (int i = 0; i <= 100; i++)
            {
                float volume = Volume(strength: i / 100f, deployment: deployment);
                if (volume < previous || volume > deployment)
                    throw new Exception(
                        "Flutter volume must rise monotonically within deployment."
                    );
                previous = volume;
            }
            Near(actual: Volume(strength: 1, deployment: deployment), expected: deployment);
            Near(actual: Volume(strength: 0.75f, deployment: deployment), expected: deployment / 2);
        }
        Near(actual: Volume(strength: 1, deployment: -1), expected: 0);
        foreach (
            float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }
        )
        {
            Near(actual: Volume(strength: invalid, deployment: 1), expected: 0);
            Near(actual: Volume(strength: 1, deployment: invalid), expected: 0);
        }
        Console.WriteLine(
            "PASS: Flutter audio onset, monotonic fade, reef attenuation and finite bounds."
        );
    }

    private static float Volume(float strength, float deployment) =>
        SailFlutterAudioResponse.Volume(strength: strength, deployment: deployment);

    private static void Near(float actual, float expected)
    {
        if (float.IsNaN(actual) || Math.Abs(actual - expected) > 0.00001f)
            throw new Exception($"Flutter volume {actual}, expected {expected}.");
    }
}
