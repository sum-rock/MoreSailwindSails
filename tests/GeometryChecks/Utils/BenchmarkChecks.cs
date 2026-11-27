using System;
using System.IO;
using System.Text.Json;
using SailwindBenchmark;

namespace MoreSailwindSails.Tests.GeometryChecks.Utils;

// Exercises capture boundaries and export integrity with synthetic clocks, never a running game.
internal static class BenchmarkChecks
{
    internal static void Run()
    {
        Events();
        var capture = new BenchmarkCapture(capacity: 8);
        capture.Start(now: 0, warmupSeconds: 2, durationSeconds: 1);
        capture.Observe(now: 0, kind: BenchmarkEventKind.Focused, value: 1);
        Tick(capture, 1, 0);
        Tick(capture, 2, 1);
        Check(
            capture.Count == 0 && capture.State == CaptureState.Measuring,
            "Warm-up boundary excluded"
        );
        Tick(capture, 2.5, 1); // Duplicate Unity frame.
        Check(capture.Count == 0, "Duplicate frame ignored");
        capture.Tick(now: 2.75, frame: 2, collections0: 1, collections1: 0, collections2: 0);
        capture.Tick(now: 3.5, frame: 3, collections0: 2, collections1: 1, collections2: 0);
        Check(
            capture.State == CaptureState.Complete && capture.Count == 2 && capture.Elapsed == 1.5,
            "Long frames retained and completion includes last whole interval"
        );
        Check(capture.Samples[0].gc0 == 1 && capture.Samples[1].gc1 == 1, "GC deltas retained");
        Tick(capture, 9, 4);
        Check(capture.Count == 2, "Completed capture immutable");
        Export(capture);

        capture.Start(now: 0, warmupSeconds: 0, durationSeconds: 10);
        Tick(capture, 0, 0);
        Tick(capture, 1, 1);
        capture.Cancel(reason: "user-cancelled");
        Check(
            capture.Count == 1
                && capture.State == CaptureState.Cancelled
                && capture.Reason == "user-cancelled",
            "Cancel preserves partial data"
        );
        capture.Start(now: 10, warmupSeconds: 0, durationSeconds: 1);
        Tick(capture, 10, 0);
        Tick(capture, 11, 1);
        Check(
            capture.Count == 1 && capture.Elapsed == 1 && capture.Reason == "",
            "Restart resets all boundaries"
        );
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, -1.0 })
        {
            capture.Start(now: 0, warmupSeconds: 0, durationSeconds: 1);
            Tick(capture, 0, 0);
            Tick(capture, invalid, 1);
            Check(capture.State == CaptureState.Cancelled, "Invalid clocks cancel");
        }
        capture.Start(now: 0, warmupSeconds: 0, durationSeconds: 1);
        capture.Tick(now: 0, frame: 0, collections0: 3, collections1: 0, collections2: 0);
        Tick(capture, 0.5, 1);
        Check(capture.Reason == "invalid-counter", "Reset GC counter rejected");
        var bounded = new BenchmarkCapture(capacity: 1);
        bounded.Start(now: 0, warmupSeconds: 0, durationSeconds: 10);
        Tick(bounded, 0, 0);
        Tick(bounded, 1, 1);
        Tick(bounded, 2, 2);
        Check(
            bounded.Count == 1 && bounded.Reason == "buffer-exhausted",
            "No buffer growth or silent sample loss"
        );
        var warmed = new BenchmarkCapture(capacity: 10001);
        warmed.Start(now: 0, warmupSeconds: 0, durationSeconds: 20000);
        Tick(warmed, 0, 0);
        Tick(warmed, 1, 1);
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 2; i <= 10000; i++)
            Tick(warmed, i, i);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        Check(allocated == 0, "Warmed capture allocates zero bytes");
        Console.WriteLine(
            "PASS: independent benchmark boundaries, cancellation, GC counters, buffer limits, exports and allocation-free sampling."
        );
    }

    private static void Events()
    {
        var capture = new BenchmarkCapture(capacity: 10, eventCapacity: 64);
        capture.Start(now: 100, warmupSeconds: 1, durationSeconds: 60);
        capture.Observe(now: 100, kind: BenchmarkEventKind.Focused, value: 1);
        capture.Observe(now: 100.5, kind: BenchmarkEventKind.Focused, value: 1);
        Check(capture.Events.Count == 1, "Unchanged state does not repeat");
        Tick(capture: capture, now: 101, frame: 0);
        foreach (
            var kind in new[]
            {
                BenchmarkEventKind.LoadingBoatLocalItems,
                BenchmarkEventKind.LoadingScenes,
                BenchmarkEventKind.CurrentlyLoading,
                BenchmarkEventKind.ApplicationPaused,
                BenchmarkEventKind.CursorMenu,
                BenchmarkEventKind.Sleeping,
                BenchmarkEventKind.Recovering,
                BenchmarkEventKind.ActiveScene,
                BenchmarkEventKind.BoatInstance,
                BenchmarkEventKind.Quality,
                BenchmarkEventKind.ConfigurationChanged,
            }
        )
        {
            capture.Observe(now: 102, kind: kind, value: 1);
            capture.Observe(now: 103, kind: kind, value: 0);
        }
        capture.Observe(now: 103, kind: BenchmarkEventKind.Focused, value: 0);
        Tick(capture: capture, now: 103, frame: 1);
        Check(capture.Active && capture.Count == 1, "Gameplay transitions never interrupt capture");
        capture.Observe(now: 180, kind: BenchmarkEventKind.Focused, value: 1);
        Tick(capture: capture, now: 180, frame: 2);
        Check(
            capture.State == CaptureState.Complete
                && capture.Elapsed == 79
                && capture.Samples[1].seconds == 77,
            "Suspended callbacks retain entire pause gap and finish on resumption"
        );
        Check(
            capture.Events.Entries[0].Seconds == 0
                && !capture.Events.Entries[0].Measuring
                && capture.Events.Entries[1].Seconds == 2
                && capture.Events.Entries[1].Measuring,
            "Events distinguish warm-up and measured time relative to capture start"
        );
        int count = capture.Events.Count;
        capture.Observe(now: 181, kind: BenchmarkEventKind.Focused, value: 0);
        Check(capture.Events.Count == count, "Completed event history stays fixed");
        capture.Start(now: 200, warmupSeconds: 0, durationSeconds: 1);
        Check(
            capture.Events.Count == 0 && capture.Events.Dropped == 0,
            "Restart resets event history"
        );
        capture.Observe(now: 200, kind: BenchmarkEventKind.Focused, value: 1);
        Check(capture.Events.Count == 1, "Restart records initial state again");

        var bounded = new BenchmarkCapture(capacity: 4, eventCapacity: 1);
        bounded.Start(now: 0, warmupSeconds: 0, durationSeconds: 2);
        bounded.Observe(now: 0, kind: BenchmarkEventKind.Focused, value: 1);
        Tick(capture: bounded, now: 0, frame: 0);
        bounded.Observe(now: 1, kind: BenchmarkEventKind.Focused, value: 0);
        bounded.Observe(now: 1, kind: BenchmarkEventKind.Focused, value: 0);
        Tick(capture: bounded, now: 2, frame: 1);
        Check(
            bounded.Events.Count == 1
                && bounded.Events.Dropped == 1
                && bounded.State == CaptureState.Complete,
            "Event overflow counts dropped transitions without stopping samples"
        );

        var warmed = new BenchmarkCapture(eventCapacity: 32);
        warmed.Start(now: 0, warmupSeconds: 0, durationSeconds: 100);
        warmed.Observe(now: 0, kind: BenchmarkEventKind.TimeScale, value: 1);
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
            warmed.Observe(now: i, kind: BenchmarkEventKind.TimeScale, value: i % 2);
        Check(
            GC.GetAllocatedBytesForCurrentThread() - start == 0,
            "Event collection and overflow allocate no memory"
        );
    }

    private static void Export(BenchmarkCapture capture)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "mss-benchmark-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            var metadata = new BenchmarkMetadata
            {
                captureId = "fixture",
                createdUtc = "2026-01-01T00:00:00Z",
                scenario = "anchored",
                variant = "native",
                notes = "fixture",
                recorderVersion = "0.1.0",
                gameVersion = "0.40.1",
                unityVersion = "2019.1.10f1",
                mssVersion = "absent",
                plugins = new[] { "benchmark@0.1.0" },
                cpu = "cpu",
                gpu = "gpu",
                graphicsApi = "api",
                operatingSystem = "os",
                width = 1920,
                height = 1080,
                quality = 1,
                vSyncCount = 0,
                targetFrameRate = -1,
                fullscreen = true,
                shipyard = false,
                scene = "scene",
                timeScale = 1,
                fixedDeltaTime = 0.02f,
                warmupSeconds = 2,
                durationSeconds = 1,
            };
            string path = BenchmarkExport.Write(
                directory: directory,
                metadata: metadata,
                capture: capture,
                serialize: value =>
                    JsonSerializer.Serialize(
                        value,
                        new JsonSerializerOptions { IncludeFields = true }
                    )
            );
            var json = JsonDocument.Parse(File.ReadAllText(path + ".json"));
            Check(json.RootElement.GetProperty("complete").GetBoolean(), "Complete export status");
            Check(
                json.RootElement.GetProperty("frames").GetInt32() == 2
                    && metadata.gc0 == 2
                    && metadata.gc1 == 1,
                "Export totals"
            );
            Check(File.ReadAllLines(path + ".csv").Length == 3, "Raw samples retained");
            var events = json.RootElement.GetProperty("events");
            Check(
                events.GetArrayLength() == 1
                    && events[0].GetProperty("kind").GetString() == "Focused"
                    && events[0].GetProperty("phase").GetString() == "warmup"
                    && events[0].GetProperty("seconds").GetDouble() == 0
                    && events[0].GetProperty("value").GetDouble() == 1,
                "JSON retains named events and their timing/value"
            );
            bool refused = false;
            try
            {
                BenchmarkExport.Write(
                    directory: directory,
                    metadata: metadata,
                    capture: capture,
                    serialize: _ => "{}"
                );
            }
            catch (IOException)
            {
                refused = true;
            }
            Check(refused, "Existing artifacts never overwritten");
            capture.Start(now: 0, warmupSeconds: 0, durationSeconds: 1);
            capture.Cancel(reason: "user-cancelled");
            metadata.captureId = "cancelled";
            BenchmarkExport.Write(
                directory: directory,
                metadata: metadata,
                capture: capture,
                serialize: _ => "{}"
            );
            Check(
                !metadata.complete && metadata.reason == "user-cancelled" && metadata.gc0 == 0,
                "Incomplete export and totals reset"
            );
        }
        finally
        {
            Directory.Delete(path: directory, recursive: true);
        }
    }

    private static void Tick(BenchmarkCapture capture, double now, int frame) =>
        capture.Tick(now: now, frame: frame, collections0: 0, collections1: 0, collections2: 0);

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
