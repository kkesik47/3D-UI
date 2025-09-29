using UnityEngine;

public class StartMenuManager : MonoBehaviour
{
    public GameObject menuUI;

    public void OnStartButtonPressed()
    {
        menuUI.SetActive(false);
        // You can also trigger puzzle logic, start timer, etc. here
    }
}
