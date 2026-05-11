using System;
using System.Collections;

[System.Serializable]
public abstract class MazeAlgorithm
{
    public abstract bool[,] Generate(int gridSize);
    public abstract IEnumerator GenerateAnimated(int gridSize, bool[,] isWall, float stepDelay, Action<int, int> onCarve);
}
