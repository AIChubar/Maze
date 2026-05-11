using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class DfsAlgorithm : MazeAlgorithm
{
    public override bool[,] Generate(int gridSize)
    {
        bool[,] isWall = new bool[gridSize, gridSize];
        bool[,] visited = new bool[gridSize, gridSize];

        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                isWall[r, c] = true;

        CarveDfs(1, 1, gridSize, isWall, visited);
        return isWall;
    }

    public override IEnumerator GenerateAnimated(int gridSize, bool[,] isWall, float stepDelay, Action<int, int> onCarve)
    {
        bool[,] visited = new bool[gridSize, gridSize];
        yield return CarveDfsAnimated(1, 1, gridSize, isWall, visited, stepDelay, onCarve);
    }

    private IEnumerator CarveDfsAnimated(int r, int c, int gridSize, bool[,] isWall, bool[,] visited, float stepDelay, Action<int, int> onCarve)
    {
        visited[r, c] = true;
        isWall[r, c] = false;
        onCarve(r, c);
        yield return new WaitForSeconds(stepDelay);

        int[] dr = { -2, 2, 0, 0 };
        int[] dc = { 0, 0, -2, 2 };
        Shuffle(dr, dc);

        for (int d = 0; d < 4; d++)
        {
            int nr = r + dr[d];
            int nc = c + dc[d];
            if (InBounds(nr, nc, gridSize) && !visited[nr, nc])
            {
                int wr = r + dr[d] / 2;
                int wc = c + dc[d] / 2;
                isWall[wr, wc] = false;
                onCarve(wr, wc);
                yield return CarveDfsAnimated(nr, nc, gridSize, isWall, visited, stepDelay, onCarve);
            }
        }
    }

    private void CarveDfs(int r, int c, int gridSize, bool[,] isWall, bool[,] visited)
    {
        visited[r, c] = true;
        isWall[r, c] = false;

        int[] dr = { -2, 2, 0, 0 };
        int[] dc = { 0, 0, -2, 2 };
        Shuffle(dr, dc);

        for (int d = 0; d < 4; d++)
        {
            int nr = r + dr[d];
            int nc = c + dc[d];
            if (InBounds(nr, nc, gridSize) && !visited[nr, nc])
            {
                isWall[r + dr[d] / 2, c + dc[d] / 2] = false;
                CarveDfs(nr, nc, gridSize, isWall, visited);
            }
        }
    }

    private bool InBounds(int r, int c, int gridSize) =>
        r > 0 && r < gridSize - 1 && c > 0 && c < gridSize - 1;

    private void Shuffle(int[] dr, int[] dc)
    {
        for (int i = 3; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (dr[i], dr[j]) = (dr[j], dr[i]);
            (dc[i], dc[j]) = (dc[j], dc[i]);
        }
    }
}
