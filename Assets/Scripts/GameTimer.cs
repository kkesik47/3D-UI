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
