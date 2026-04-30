using UnityEngine;

[System.Serializable]
public abstract class MazeAlgorithm
{
    public abstract bool[,] Generate(int gridSize);
}
