using TMPro;
using UnityEngine;

public class GridDebugDisplay : MonoBehaviour
{
    public TextMeshProUGUI text;
    public GridManager grid;

    void Update()
    {
        if (grid == null || text == null) return;
        int filled = 0;
        foreach (var pos in grid.allGridPositions)
            if (grid.IsOccupied(pos)) filled++;
        text.text = $"Grid Fill: {filled}/{grid.allGridPositions.Count}\n" +
                    $"Grid Full: {grid.IsGridFull()}";
    }
}
