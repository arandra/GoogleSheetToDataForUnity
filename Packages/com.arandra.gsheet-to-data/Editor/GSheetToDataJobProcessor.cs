#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace GSheetToDataForUnity.Editor
{
    [InitializeOnLoad]
    internal static class GSheetToDataJobProcessor
    {
        static GSheetToDataJobProcessor()
        {
            EditorApplication.delayCall += ProcessPendingJobs;
        }

        private static void ProcessPendingJobs()
        {
            var jobs = GSheetToDataJobStore.LoadAll();
            if (jobs.Count == 0)
            {
                return;
            }

            var completed = new List<GSheetToDataGenerationJob>();
            foreach (var job in jobs)
            {
                var created = GSheetToDataAssetBuilder.TryCreate(job);
                if (created)
                {
                    completed.Add(job);
                }
            }

            if (completed.Count > 0)
            {
                GSheetToDataJobStore.Remove(completed);
            }

            if (GSheetToDataJobStore.HasJobs())
            {
                // Retry on the next update in case assemblies are not ready yet.
                EditorApplication.delayCall += ProcessPendingJobs;
            }
        }
    }
}
#endif
