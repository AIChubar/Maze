using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class KruskalsAlgorithm : MazeAlgorithm
{
    public override bool[,] Generate(int gridSize)
    {
        bool[,] isWall = new bool[gridSize, gridSize];
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                isWall[r, c] = true;

        // Carve all cells and assign ids
        int[,] cellId = new int[gridSize, gridSize];
        int id = 0;
        for (int r = 1; r < gridSize - 1; r += 2)
            for (int c = 1; c < gridSize - 1; c += 2)
            {
                isWall[r, c] = false;
                cellId[r, c] = id++;
            }

        // Collect all walls between adjacent cells
        List<(Vector2Int a, Vector2Int wall, Vector2Int b)> walls =
            new List<(Vector2Int, Vector2Int, Vector2Int)>();

        for (int r = 1; r < gridSize - 1; r += 2)
            for (int c = 1; c < gridSize - 1; c += 2)
            {
                if (r + 2 < gridSize - 1)
                    walls.Add((new Vector2Int(r, c), new Vector2Int(r + 1, c), new Vector2Int(r + 2, c)));
                if (c + 2 < gridSize - 1)
                    walls.Add((new Vector2Int(r, c), new Vector2Int(r, c + 1), new Vector2Int(r, c + 2)));
            }

        // Shuffle
        for (int i = walls.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (walls[i], walls[j]) = (walls[j], walls[i]);
        }

        // Union-Find — merge sets and carve walls between different sets
        int[] parent = new int[id];
        for (int i = 0; i < id; i++) parent[i] = i;

        foreach (var (a, wall, b) in walls)
        {
            int rootA = Find(parent, cellId[a.x, a.y]);
            int rootB = Find(parent, cellId[b.x, b.y]);
            if (rootA == rootB) continue;
            parent[rootA] = rootB;
            isWall[wall.x, wall.y] = false;
        }

        return isWall;
    }

    public override IEnumerator GenerateAnimated(int gridSize, bool[,] isWall, float stepDelay, Action<int, int> onCarve)
    {
        int[,] cellId = new int[gridSize, gridSize];
        int id = 0;
        for (int r = 1; r < gridSize - 1; r += 2)
            for (int c = 1; c < gridSize - 1; c += 2)
            {
                isWall[r, c] = false;
                onCarve(r, c);
                cellId[r, c] = id++;
            }

        yield return new WaitForSeconds(stepDelay);

        List<(Vector2Int a, Vector2Int wall, Vector2Int b)> walls =
            new List<(Vector2Int, Vector2Int, Vector2Int)>();

        for (int r = 1; r < gridSize - 1; r += 2)
            for (int c = 1; c < gridSize - 1; c += 2)
            {
                if (r + 2 < gridSize - 1)
                    walls.Add((new Vector2Int(r, c), new Vector2Int(r + 1, c), new Vector2Int(r + 2, c)));
                if (c + 2 < gridSize - 1)
                    walls.Add((new Vector2Int(r, c), new Vector2Int(r, c + 1), new Vector2Int(r, c + 2)));
            }

        for (int i = walls.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (walls[i], walls[j]) = (walls[j], walls[i]);
        }

        int[] parent = new int[id];
        for (int i = 0; i < id; i++) parent[i] = i;

        foreach (var (a, wall, b) in walls)
        {
            int rootA = Find(parent, cellId[a.x, a.y]);
            int rootB = Find(parent, cellId[b.x, b.y]);
            if (rootA == rootB) continue;
            parent[rootA] = rootB;
            isWall[wall.x, wall.y] = false;
            onCarve(wall.x, wall.y);
            yield return new WaitForSeconds(stepDelay);
        }
    }

    private int Find(int[] parent, int x)
    {
        if (parent[x] != x) parent[x] = Find(parent, parent[x]);
        return parent[x];
    }
}
