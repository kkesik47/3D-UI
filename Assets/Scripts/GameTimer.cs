using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public GridManager gridManager;      
    public TextMeshProUGUI timerText;    
    public StartMenuManager startMenu;  

    private bool timerRunning = false;
    private float elapsedTime = 0f;

    void Start()
    {
        if (timerText)
            timerText.text = "Time: 0.00s";
        if (startMenu != null)
            startMenu.OnGameStarted += StartTimer;
    }

    void Update()
    {
        if (!timerRunning) return;
        if (gridManager != null && gridManager.IsGridFull())
        {
            StopTimer();
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
