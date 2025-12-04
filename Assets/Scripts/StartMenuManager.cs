using UnityEngine;

public class StartMenuManager : MonoBehaviour
{
    public GameObject menuUI;
    public System.Action OnGameStarted;

    public void OnStartButtonPressed()
    {
        menuUI.SetActive(false);
        OnGameStarted?.Invoke();
    }
}
