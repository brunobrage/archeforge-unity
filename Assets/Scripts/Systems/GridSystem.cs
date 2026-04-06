using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    public int TileSize = 32;
    public int Width = 100;
    public int Height = 100;

    private int[][] grid;

    void Awake()
    {
        InitializeGrid();
    }

    void InitializeGrid()
    {
        grid = new int[Height][];
        
        for (int y = 0; y < Height; y++)
        {
            grid[y] = new int[Width];
            for (int x = 0; x < Width; x++)
            {
                bool border = (x == 0 || y == 0 || x == Width - 1 || y == Height - 1);
                bool cluster = ((y % 4 == 0 && x % 5 == 0) || (y % 6 == 2 && x % 7 == 3));
                bool randomRock = Random.value < 0.05f; // 5% chance for random rocks

                grid[y][x] = (border || cluster || randomRock) ? 1 : 0;
            }
        }

        // Debug: Count tiles
        int solidCount = 0;
        int resourceCount = 0;
        foreach (int[] row in grid)
        {
            foreach (int tile in row)
            {
                if (tile == 1) solidCount++;
                if (tile == 2) resourceCount++;
            }
        }

        Debug.Log($"[GridInit] Grid initialized with {solidCount} solid tiles and {resourceCount} resources out of {Width * Height} total tiles");
    }

    public void BreakTile(int x, int y)
    {
        if (IsWithinBounds(x, y) && grid[y][x] == 1)
        {
            grid[y][x] = 2;
        }
    }

    public bool CollectResource(int x, int y)
    {
        if (IsWithinBounds(x, y) && grid[y][x] == 2)
        {
            grid[y][x] = 0;
            return true;
        }
        return false;
    }

    public bool PlantResource(int x, int y)
    {
        if (IsWithinBounds(x, y) && grid[y][x] == 0)
        {
            grid[y][x] = 2;
            return true;
        }
        return false;
    }

    public bool BuildWall(int x, int y)
    {
        if (IsWithinBounds(x, y) && grid[y][x] == 0)
        {
            grid[y][x] = 1;
            return true;
        }
        return false;
    }

    public bool IsSolid(int x, int y)
    {
        return IsWithinBounds(x, y) && grid[y][x] == 1;
    }

    public bool IsResource(int x, int y)
    {
        return IsWithinBounds(x, y) && grid[y][x] == 2;
    }

    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool IsAreaFree(float left, float top, float width, float height)
    {
        int startX = Mathf.FloorToInt(left / TileSize);
        int startY = Mathf.FloorToInt(top / TileSize);
        int endX = Mathf.FloorToInt((left + width - 1) / TileSize);
        int endY = Mathf.FloorToInt((top + height - 1) / TileSize);

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                if (IsSolid(x, y))
                    return false;
            }
        }

        return true;
    }
}
