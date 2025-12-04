using UnityEngine;

public class WinBanner : MonoBehaviour
{
    public GridManager gridManager;
    public GameObject winMenu; 

    [Header("Audio")]
    public AudioSource victoryAudio;

    bool wasFull = false;

    void Update()
    {
        if (gridManager == null || winMenu == null)
            return;

        bool isFull = gridManager.IsGridFull();
        winMenu.SetActive(isFull);
        if (isFull && !wasFull)
        {
            if (victoryAudio != null)
                victoryAudio.Play();
        }

        wasFull = isFull;
    }
}
