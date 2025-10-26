using UnityEngine;

public class WinBanner : MonoBehaviour
{
    public GridManager gridManager;   // drag your Grid (with GridManager) here
    public GameObject bannerPanel;    // drag the Panel (or the whole WinMenu) here

    bool last;

    void Start()
    {
        if (bannerPanel) bannerPanel.SetActive(false); // start hidden
    }

    void Update()
    {
        if (!gridManager || !bannerPanel) return;

        bool isFull = gridManager.IsGridFull();
        if (isFull != last)
        {
            bannerPanel.SetActive(isFull);
            last = isFull;
        }
    }
}
