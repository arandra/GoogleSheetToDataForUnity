#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GSheetToDataForUnity.Editor
{
    internal static class GSheetToDataJobStore
    {
        private static readonly string JobsPath = Path.Combine(
            GSheetToDataPathUtility.ProjectRoot,
            "Library",
            "GSheetToDataJobs.json");

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        internal static bool HasJobs()
        {
            return LoadAll().Count > 0;
        }

        internal static void Enqueue(GSheetToDataGenerationJob job)
        {
            var jobs = LoadAll();
            jobs.Add(job);
            SaveAll(jobs);
        }

        internal static List<GSheetToDataGenerationJob> LoadAll()
        {
            if (!File.Exists(JobsPath))
            {
                return new List<GSheetToDataGenerationJob>();
            }

            var json = File.ReadAllText(JobsPath);
            var jobs = JsonConvert.DeserializeObject<List<GSheetToDataGenerationJob>>(json);
            return jobs ?? new List<GSheetToDataGenerationJob>();
        }

        internal static void Remove(IEnumerable<GSheetToDataGenerationJob> completedJobs)
        {
            var remaining = LoadAll();
            foreach (var completed in completedJobs)
            {
                remaining.RemoveAll(job =>
                    job.EnqueuedAtUtc == completed.EnqueuedAtUtc &&
                    job.ScriptableObjectFullName == completed.ScriptableObjectFullName);
            }

            SaveAll(remaining);
        }

        private static void SaveAll(List<GSheetToDataGenerationJob> jobs)
        {
            var directory = Path.GetDirectoryName(JobsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(jobs, SerializerSettings);
            File.WriteAllText(JobsPath, json);
        }
    }
}
#endif
