using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public GridManager gridManager;      // assign in Inspector
    public TextMeshProUGUI timerText;    // optional UI label
    public StartMenuManager startMenu;   // assign your StartMenuManager here

    private bool timerRunning = false;
    private float elapsedTime = 0f;

    void Start()
    {
        if (timerText)
            timerText.text = "Time: 0.00s";

        // subscribe to Start button
        if (startMenu != null)
            startMenu.OnGameStarted += StartTimer;
    }

    void Update()
    {
        if (!timerRunning) return;

        // Stop automatically when grid full
        if (gridManager != null && gridManager.IsGridFull())
        {
            StopTimer();

            // NEW: log completion time
            var tracker = FindObjectOfType<ConditionTimeTracker>();
            var snapManager = FindObjectOfType<SnapDistanceManager>();

            if (tracker != null && snapManager != null)
            {
                tracker.RecordCompletionTime(
                    snapManager.currentCondition,
                    snapManager.currentSnapDistance,
                    elapsedTime
                );
            }
            else
            {
                Debug.LogWarning("[Study] Missing ConditionTimeTracker or SnapDistanceManager, not logging.");
            }

            return;


        }

        // Otherwise tick
        elapsedTime += Time.deltaTime;

        if (timerText)
            timerText.text = $"Time: {elapsedTime:F2}s";
    }

    public void StartTimer()
    {
        elapsedTime = 0f;
        timerRunning = true;
        if (timerText)
            timerText.text = "Time: 0.00s";
    }

    public void StopTimer()
    {
        timerRunning = false;
        if (timerText)
            timerText.text = $"Final Time: {elapsedTime:F2}s";
    }
}
