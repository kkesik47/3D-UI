using TMPro;
using UnityEngine;

public class GridDebugDisplay : MonoBehaviour
{
    public TextMeshProUGUI text;   // Drag your TMP text here
    public GridManager grid;       // Drag the GridManager here

    void Update()
    {
        if (grid == null || text == null) return;

        // Count filled cells
        int filled = 0;
        foreach (var pos in grid.allGridPositions)
            if (grid.IsOccupied(pos)) filled++;

        // Update text
        text.text = $"Grid Fill: {filled}/{grid.allGridPositions.Count}\n" +
                    $"Grid Full: {grid.IsGridFull()}";
    }
}
