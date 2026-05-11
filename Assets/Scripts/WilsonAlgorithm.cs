using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class WilsonAlgorithm : MazeAlgorithm
{
    [SerializeField] private int loopCount = 5;

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

        foreach (Vector2Int wall in PickLoopWalls(gridSize, isWall))
            isWall[wall.x, wall.y] = false;

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

        foreach (Vector2Int wall in PickLoopWalls(gridSize, isWall))
        {
            isWall[wall.x, wall.y] = false;
            onCarve(wall.x, wall.y);
            yield return new WaitForSeconds(stepDelay);
        }
    }

    private List<Vector2Int> PickLoopWalls(int gridSize, bool[,] isWall)
    {
        List<(Vector2Int wall, int dist)> candidates = new List<(Vector2Int, int)>();

        for (int r = 1; r < gridSize - 1; r += 2)
            for (int c = 1; c < gridSize - 1; c += 2)
            {
                if (c + 2 < gridSize - 1 && isWall[r, c + 1])
                {
                    int dist = BfsDistance(isWall, gridSize, new Vector2Int(r, c), new Vector2Int(r, c + 2));
                    candidates.Add((new Vector2Int(r, c + 1), dist));
                }
                if (r + 2 < gridSize - 1 && isWall[r + 1, c])
                {
                    int dist = BfsDistance(isWall, gridSize, new Vector2Int(r, c), new Vector2Int(r + 2, c));
                    candidates.Add((new Vector2Int(r + 1, c), dist));
                }
            }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int minSpacing = gridSize / 3;

        List<Vector2Int> result = new List<Vector2Int>();
        foreach (var (wall, _) in candidates)
        {
            if (result.Count >= loopCount) break;
            if (CreatesIsolatedCorner(isWall, gridSize, wall)) continue;
            if (TooClose(wall, result, minSpacing)) continue;
            isWall[wall.x, wall.y] = false;
            result.Add(wall);
        }

        return result;
    }

    private bool TooClose(Vector2Int wall, List<Vector2Int> existing, int minDist)
    {
        foreach (Vector2Int other in existing)
            if (Mathf.Abs(wall.x - other.x) + Mathf.Abs(wall.y - other.y) < minDist)
                return true;
        return false;
    }

    private bool CreatesIsolatedCorner(bool[,] isWall, int gridSize, Vector2Int wall)
    {
        int r = wall.x, c = wall.y;
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        Vector2Int[] corners = r % 2 == 1
            ? new[] { new Vector2Int(r - 1, c), new Vector2Int(r + 1, c) }
            : new[] { new Vector2Int(r, c - 1), new Vector2Int(r, c + 1) };

        foreach (Vector2Int corner in corners)
        {
            int cr = corner.x, cc = corner.y;
            if (cr <= 0 || cr >= gridSize - 1 || cc <= 0 || cc >= gridSize - 1) continue;

            bool surrounded = true;
            for (int d = 0; d < 4; d++)
            {
                int nr = cr + dr[d], nc = cc + dc[d];
                if (nr == r && nc == c) continue;
                if (isWall[nr, nc]) { surrounded = false; break; }
            }
            if (surrounded) return true;
        }

        return false;
    }

    private int BfsDistance(bool[,] isWall, int gridSize, Vector2Int from, Vector2Int to)
    {
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        int[,] dist = new int[gridSize, gridSize];
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                dist[r, c] = -1;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        dist[from.x, from.y] = 0;
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nr = current.x + dr[d];
                int nc = current.y + dc[d];
                if (nr < 0 || nr >= gridSize || nc < 0 || nc >= gridSize) continue;
                if (isWall[nr, nc] || dist[nr, nc] != -1) continue;
                dist[nr, nc] = dist[current.x, current.y] + 1;
                if (nr == to.x && nc == to.y) return dist[nr, nc];
                queue.Enqueue(new Vector2Int(nr, nc));
            }
        }

        return int.MaxValue;
    }
}
