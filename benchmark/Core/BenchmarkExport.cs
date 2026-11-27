using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SailwindBenchmark
{
    // Writes completed or cancelled captures only after sampling has stopped; JSON is the completion marker.
    internal static class BenchmarkExport
    {
        internal static string Write(
            string directory,
            BenchmarkMetadata metadata,
            BenchmarkCapture capture,
            Func<BenchmarkMetadata, string> serialize
        )
        {
            if (capture.Active || capture.State == CaptureState.Idle)
                throw new InvalidOperationException("Capture must be stopped before export.");
            metadata.events = new BenchmarkEventData[capture.Events.Count];
            metadata.droppedEvents = capture.Events.Dropped;
            for (int i = 0; i < capture.Events.Count; i++)
            {
                var entry = capture.Events.Entries[i];
                metadata.events[i] = new BenchmarkEventData
                {
                    seconds = entry.Seconds,
                    phase = entry.Measuring ? "measured" : "warmup",
                    kind = entry.Kind.ToString(),
                    value = entry.Value,
                };
            }
            metadata.complete = capture.State == CaptureState.Complete;
            metadata.reason = capture.Reason;
            metadata.frames = capture.Count;
            metadata.elapsedSeconds = capture.Elapsed;
            metadata.gc0 = metadata.gc1 = metadata.gc2 = 0;
            Directory.CreateDirectory(path: directory);
            string stem = Path.Combine(path1: directory, path2: metadata.captureId);
            using (
                var stream = new FileStream(
                    path: stem + ".csv",
                    mode: FileMode.CreateNew,
                    access: FileAccess.Write
                )
            )
            using (var writer = new StreamWriter(stream: stream, encoding: new UTF8Encoding(false)))
            {
                writer.WriteLine("frame,seconds,gc0,gc1,gc2");
                for (int i = 0; i < capture.Count; i++)
                {
                    var sample = capture.Samples[i];
                    writer.WriteLine(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1:R},{2},{3},{4}",
                            i,
                            sample.seconds,
                            sample.gc0,
                            sample.gc1,
                            sample.gc2
                        )
                    );
                    metadata.gc0 += sample.gc0;
                    metadata.gc1 += sample.gc1;
                    metadata.gc2 += sample.gc2;
                }
            }
            using (
                var stream = new FileStream(
                    path: stem + ".json",
                    mode: FileMode.CreateNew,
                    access: FileAccess.Write
                )
            )
            using (var writer = new StreamWriter(stream: stream, encoding: new UTF8Encoding(false)))
                writer.Write(serialize(metadata));
            return stem;
        }
    }

    [Serializable]
    internal struct BenchmarkEventData
    {
        public double seconds,
            value;
        public string phase,
            kind;
    }

    [Serializable]
    internal sealed class BenchmarkMetadata
    {
        public int schemaVersion = 1;
        public BenchmarkEventData[] events;
        public int droppedEvents;
        public string captureId,
            createdUtc,
            scenario,
            variant,
            notes;
        public string measurement = "late-update-monotonic-frame-intervals";
        public string recorderVersion,
            gameVersion,
            unityVersion,
            mssVersion;
        public string[] plugins;
        public string cpu,
            gpu,
            graphicsApi,
            operatingSystem;
        public int width,
            height,
            quality,
            vSyncCount,
            targetFrameRate;
        public bool fullscreen,
            shipyard;
        public string scene;
        public float timeScale,
            fixedDeltaTime;
        public double warmupSeconds,
            durationSeconds;
        public bool complete;
        public string reason;
        public int frames,
            gc0,
            gc1,
            gc2;
        public double elapsedSeconds;
    }
}
