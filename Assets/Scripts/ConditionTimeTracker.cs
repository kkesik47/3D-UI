using System;
using System.IO;
using UnityEngine;

public class ConditionTimeTracker : MonoBehaviour
{
    [Header("Participant info")]
    [Tooltip("Set a unique ID before each participant: e.g. P01, P02...")]
    public string participantId = "P01";

    [Header("File settings")]
    [Tooltip("Name of the CSV file that will be created/appended")]
    public string fileName = "user_study_results.csv";

    // Full path where the file will be stored (on Quest: Application.persistentDataPath)
    string FilePath => Path.Combine(Application.persistentDataPath, fileName);

    /// <summary>
    /// Call this when a condition is successfully completed.
    /// </summary>
    public void RecordCompletionTime(int conditionIndex, float snapDistance, float timeSeconds)
    {
        if (conditionIndex < 1 || conditionIndex > 4)
        {
            Debug.LogError($"[ConditionTimeTracker] Invalid condition index {conditionIndex}. Must be 1–4.");
            return;
        }

        bool fileExists = File.Exists(FilePath);

        try
        {
            using (var writer = new StreamWriter(FilePath, append: true))
            {
                if (!fileExists)
                {
                    writer.WriteLine("timestamp,participant,condition,snapDistance,timeSeconds");
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                writer.WriteLine($"{timestamp},{participantId},{conditionIndex},{snapDistance:F3},{timeSeconds:F2}");
            }

            Debug.Log($"[Study] Logged: cond {conditionIndex}, time {timeSeconds:F2}s, snapDist {snapDistance:F3} → {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConditionTimeTracker] Failed to write file: {e}");
        }
    }
}
