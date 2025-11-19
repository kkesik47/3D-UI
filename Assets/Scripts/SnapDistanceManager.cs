using UnityEngine;

public class SnapDistanceManager : MonoBehaviour
{
    [Header("Initial value used on scene start")]
    public float defaultSnapDistance = 0.1f;

    private SnapToGrid[] snapScripts;

    // For logging
    public int currentCondition { get; private set; } = 1;
    public float currentSnapDistance { get; private set; }

    void Awake()
    {
        // Cache all SnapToGrid scripts in the scene
        snapScripts = FindObjectsOfType<SnapToGrid>();
        SetSnapDistanceInternal(defaultSnapDistance);
        currentSnapDistance = defaultSnapDistance;
    }

    void SetSnapDistanceInternal(float value)
    {
        currentSnapDistance = value;

        if (snapScripts == null) return;

        foreach (var s in snapScripts)
        {
            if (s == null) continue;
            s.snapDistance = value;
        }

        Debug.Log($"[Study] Snap distance set to {value}");
    }

    // --- methods to call from buttons ---

    public void SetCondition1()
    {
        currentCondition = 1;
        SetSnapDistanceInternal(0.05f);
    }

    public void SetCondition2()
    {
        currentCondition = 2;
        SetSnapDistanceInternal(0.10f);
    }

    public void SetCondition3()
    {
        currentCondition = 3;
        SetSnapDistanceInternal(0.25f);
    }

    public void SetCondition4()
    {
        currentCondition = 4;
        SetSnapDistanceInternal(0.40f);
    }

}
