using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PrimsAlgorithm : MazeAlgorithm
{
    public override bool[,] Generate(int gridSize)
    {
        bool[,] isWall = new bool[gridSize, gridSize];
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                isWall[r, c] = true;

        bool[,] inMaze = new bool[gridSize, gridSize];

        int[] dr = { -2, 2, 0, 0 };
        int[] dc = { 0, 0, -2, 2 };

        int startR = 1 + Random.Range(0, (gridSize - 1) / 2) * 2;
        int startC = 1 + Random.Range(0, (gridSize - 1) / 2) * 2;
        inMaze[startR, startC] = true;
        isWall[startR, startC] = false;

        // frontier: (wall position, unvisited cell position)
        List<(Vector2Int wall, Vector2Int cell)> frontier = new List<(Vector2Int, Vector2Int)>();
        AddFrontier(startR, startC, gridSize, inMaze, frontier, dr, dc);

        while (frontier.Count > 0)
        {
            int idx = Random.Range(0, frontier.Count);
            var (wall, cell) = frontier[idx];
            frontier.RemoveAt(idx);

            if (inMaze[cell.x, cell.y]) continue;

            inMaze[cell.x, cell.y] = true;
            isWall[cell.x, cell.y] = false;
            isWall[wall.x, wall.y] = false;

            AddFrontier(cell.x, cell.y, gridSize, inMaze, frontier, dr, dc);
        }

        return isWall;
    }

    private void AddFrontier(int r, int c, int gridSize, bool[,] inMaze,
        List<(Vector2Int, Vector2Int)> frontier, int[] dr, int[] dc)
    {
        for (int d = 0; d < 4; d++)
        {
            int nr = r + dr[d];
            int nc = c + dc[d];
            if (nr > 0 && nr < gridSize - 1 && nc > 0 && nc < gridSize - 1 && !inMaze[nr, nc])
                frontier.Add((new Vector2Int((r + nr) / 2, (c + nc) / 2), new Vector2Int(nr, nc)));
        }
    }
}
