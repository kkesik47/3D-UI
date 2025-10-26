using UnityEngine;

public class StartMenuManager : MonoBehaviour
{
    public GameObject menuUI;

    // Event for others to subscribe to
    public System.Action OnGameStarted;

    public void OnStartButtonPressed()
    {
        // Hide the menu
        menuUI.SetActive(false);

        // Notify others (like the timer)
        OnGameStarted?.Invoke();
    }
}
