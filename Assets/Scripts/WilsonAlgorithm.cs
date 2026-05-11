using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class WilsonAlgorithm : MazeAlgorithm
{
    public override bool[,] Generate(int gridSize)
    {
        bool[,] isWall = new bool[gridSize, gridSize];
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                isWall[r, c] = true;

        List<Vector2Int> cells = new List<Vector2Int>();
        for (int r = 1; r < gridSize - 1; r += 2)
            for (int c = 1; c < gridSize - 1; c += 2)
                cells.Add(new Vector2Int(r, c));

        bool[,] inMaze = new bool[gridSize, gridSize];

        Vector2Int seed = cells[Random.Range(0, cells.Count)];
        inMaze[seed.x, seed.y] = true;
        isWall[seed.x, seed.y] = false;
        int inMazeCount = 1;

        int[] dr = { -2, 2, 0, 0 };
        int[] dc = { 0, 0, -2, 2 };

        while (inMazeCount < cells.Count)
        {
            Vector2Int walkStart = cells[Random.Range(0, cells.Count)];
            while (inMaze[walkStart.x, walkStart.y])
                walkStart = cells[Random.Range(0, cells.Count)];

            // Loop-erased random walk — overwriting next[cell] erases loops naturally
            Dictionary<Vector2Int, Vector2Int> next = new Dictionary<Vector2Int, Vector2Int>();
            Vector2Int current = walkStart;

            while (!inMaze[current.x, current.y])
            {
                List<int> validDirs = new List<int>();
                for (int d = 0; d < 4; d++)
                {
                    int nr = current.x + dr[d];
                    int nc = current.y + dc[d];
                    if (nr > 0 && nr < gridSize - 1 && nc > 0 && nc < gridSize - 1)
                        validDirs.Add(d);
                }
                int chosen = validDirs[Random.Range(0, validDirs.Count)];
                next[current] = new Vector2Int(current.x + dr[chosen], current.y + dc[chosen]);
                current = next[current];
            }

            current = walkStart;
            while (!inMaze[current.x, current.y])
            {
                inMaze[current.x, current.y] = true;
                isWall[current.x, current.y] = false;
                Vector2Int nextCell = next[current];
                isWall[(current.x + nextCell.x) / 2, (current.y + nextCell.y) / 2] = false;
                current = nextCell;
                inMazeCount++;
            }
        }

        return isWall;
    }

    public override IEnumerator GenerateAnimated(int gridSize, bool[,] isWall, float stepDelay, Action<int, int> onCarve)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int r = 1; r < gridSize - 1; r += 2)
            for (int c = 1; c < gridSize - 1; c += 2)
                cells.Add(new Vector2Int(r, c));

        bool[,] inMaze = new bool[gridSize, gridSize];

        Vector2Int seed = cells[Random.Range(0, cells.Count)];
        inMaze[seed.x, seed.y] = true;
        isWall[seed.x, seed.y] = false;
        onCarve(seed.x, seed.y);
        int inMazeCount = 1;

        int[] dr = { -2, 2, 0, 0 };
        int[] dc = { 0, 0, -2, 2 };

        while (inMazeCount < cells.Count)
        {
            Vector2Int walkStart = cells[Random.Range(0, cells.Count)];
            while (inMaze[walkStart.x, walkStart.y])
                walkStart = cells[Random.Range(0, cells.Count)];

            Dictionary<Vector2Int, Vector2Int> next = new Dictionary<Vector2Int, Vector2Int>();
            Vector2Int current = walkStart;

            while (!inMaze[current.x, current.y])
            {
                List<int> validDirs = new List<int>();
                for (int d = 0; d < 4; d++)
                {
                    int nr = current.x + dr[d];
                    int nc = current.y + dc[d];
                    if (nr > 0 && nr < gridSize - 1 && nc > 0 && nc < gridSize - 1)
                        validDirs.Add(d);
                }
                int chosen = validDirs[Random.Range(0, validDirs.Count)];
                next[current] = new Vector2Int(current.x + dr[chosen], current.y + dc[chosen]);
                current = next[current];
            }

            current = walkStart;
            while (!inMaze[current.x, current.y])
            {
                inMaze[current.x, current.y] = true;
                isWall[current.x, current.y] = false;
                onCarve(current.x, current.y);
                Vector2Int nextCell = next[current];
                int wr = (current.x + nextCell.x) / 2;
                int wc = (current.y + nextCell.y) / 2;
                isWall[wr, wc] = false;
                onCarve(wr, wc);
                current = nextCell;
                inMazeCount++;
                yield return new WaitForSeconds(stepDelay);
            }
        }
    }
}
