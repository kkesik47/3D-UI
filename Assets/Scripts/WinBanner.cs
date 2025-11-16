using UnityEngine;

public class WinBanner : MonoBehaviour
{
    public GridManager gridManager;
    public GameObject winMenu;          // the UI panel you already show/hide

    [Header("Audio")]
    public AudioSource victoryAudio;    // 👈 drag the AudioSource here in Inspector

    bool wasFull = false;              // track last frame state

    void Update()
    {
        if (gridManager == null || winMenu == null)
            return;

        bool isFull = gridManager.IsGridFull();

        // Show / hide win menu (your existing behaviour)
        winMenu.SetActive(isFull);

        // 🔊 Play victory sound only on the transition: not full -> full
        if (isFull && !wasFull)
        {
            if (victoryAudio != null)
                victoryAudio.Play();
        }

        wasFull = isFull;
    }
}
