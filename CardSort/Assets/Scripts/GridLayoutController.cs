using System.Collections.Generic;
using UnityEngine;

// Utility to compute card positions to fill a Rect area (centered coordinates)
public static class GridLayoutController
{
    // returns local positions for each cell (left-to-right, top-to-bottom), centered at 0,0
    public static List<Vector2> GenerateGridPositions(Vector2 areaSize, int cols, int rows, float spacing)
    {
        List<Vector2> list = new List<Vector2>();
      
        float totalSpacingX = spacing * (cols + 1);
        float totalSpacingY = spacing * (rows + 1);
        float cellW = (areaSize.x - totalSpacingX) / cols;
        float cellH = (areaSize.y - totalSpacingY) / rows;

       
        float startX = -areaSize.x / 2f + spacing + cellW / 2f;
        float startY = areaSize.y / 2f - spacing - cellH / 2f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = startX + c * (cellW + spacing);
                float y = startY - r * (cellH + spacing);
                list.Add(new Vector2(x, y));
            }
        }
        return list;
    }
}
