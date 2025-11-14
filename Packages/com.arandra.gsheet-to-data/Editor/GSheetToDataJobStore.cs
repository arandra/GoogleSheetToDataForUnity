#if UNITY_EDITOR
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;

namespace GSheetToDataForUnity.Editor
{
    internal static class GSheetToDataJobStore
    {
        private const string JobsKey = "GSheetToDataForUnity.PendingJobs";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None
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
            var json = SessionState.GetString(JobsKey, "[]");
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
            var json = JsonConvert.SerializeObject(jobs, SerializerSettings);
            SessionState.SetString(JobsKey, json);
        }
    }
}
#endif
