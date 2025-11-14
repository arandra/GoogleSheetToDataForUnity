#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GSheetToDataForUnity.Editor
{
    [InitializeOnLoad]
    internal static class GSheetToDataJobProcessor
    {
        private static bool isScheduled;

        static GSheetToDataJobProcessor()
        {
            ScheduleProcessing();
        }

        internal static void RequestProcessing()
        {
            ScheduleProcessing();
        }

        private static void ScheduleProcessing()
        {
            if (isScheduled)
            {
                return;
            }

            isScheduled = true;
            EditorApplication.delayCall += ProcessPendingJobs;
        }

        private static void ProcessPendingJobs()
        {
            isScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleProcessing();
                return;
            }

            var jobs = GSheetToDataJobStore.LoadAll();
            if (jobs.Count == 0)
            {
                return;
            }

            var completed = new List<GSheetToDataGenerationJob>();
            foreach (var job in jobs)
            {
                bool created;
                string error;
                try
                {
                    created = GSheetToDataAssetBuilder.TryCreate(job, out error);
                }
                catch (System.Exception ex)
                {
                    created = false;
                    error = ex.Message;
                    Debug.LogError($"[GSheetToData] Unexpected error while processing job for {job?.SheetName}: {ex}");
                }

                if (!created)
                {
                    var message = string.IsNullOrEmpty(error)
                        ? "Unknown error occurred while generating assets."
                        : error;
                    EditorUtility.DisplayDialog(
                        "GSheetToData",
                        $"Failed to generate assets for '{job?.SheetName}'.\n{message}",
                        "OK");
                }

                completed.Add(job);
            }

            if (completed.Count > 0)
            {
                GSheetToDataJobStore.Remove(completed);
            }

            if (GSheetToDataJobStore.HasJobs())
            {
                // Retry on the next update in case assemblies are not ready yet.
                ScheduleProcessing();
            }
        }
    }
}
#endif
