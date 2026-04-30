using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Settings")]
    [SerializeField] private int gridSize = 21;
    [SerializeField] private float cellSize = 2f;

    [Header("References")]
    [SerializeField] private GameObject player;

    private bool[,] _isWall;
    private Vector2Int _exitCell;

    private void Start()
    {
        GenerateMaze();
        _exitCell = FindFarthestCell();
        LogMazeSchema();
        BuildGeometry();
        PlaceActors();
    }

    private void LogMazeSchema()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Maze {gridSize}x{gridSize}  # = wall  . = path  P = player  E = exit");
        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                if (r == 1 && c == 1) sb.Append('P');
                else if (r == _exitCell.x && c == _exitCell.y) sb.Append('E');
                else sb.Append(_isWall[r, c] ? '#' : '.');
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }

    private void GenerateMaze()
    {
        _isWall = new bool[gridSize, gridSize];
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                _isWall[r, c] = true;

        List<Vector2Int> cells = new List<Vector2Int>();
        for (int r = 1; r < gridSize - 1; r += 2)
            for (int c = 1; c < gridSize - 1; c += 2)
                cells.Add(new Vector2Int(r, c));

        bool[,] inMaze = new bool[gridSize, gridSize];

        Vector2Int seed = cells[Random.Range(0, cells.Count)];
        inMaze[seed.x, seed.y] = true;
        _isWall[seed.x, seed.y] = false;
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

            // Trace walk path into the maze
            current = walkStart;
            while (!inMaze[current.x, current.y])
            {
                inMaze[current.x, current.y] = true;
                _isWall[current.x, current.y] = false;
                Vector2Int nextCell = next[current];
                _isWall[(current.x + nextCell.x) / 2, (current.y + nextCell.y) / 2] = false;
                current = nextCell;
                inMazeCount++;
            }
        }
    }

    private Vector2Int FindFarthestCell()
    {
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        int[,] dist = new int[gridSize, gridSize];
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                dist[r, c] = -1;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        dist[1, 1] = 0;
        queue.Enqueue(new Vector2Int(1, 1));

        Vector2Int farthest = new Vector2Int(1, 1);
        int maxDist = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nr = current.x + dr[d];
                int nc = current.y + dc[d];
                if (nr < 0 || nr >= gridSize || nc < 0 || nc >= gridSize) continue;
                if (_isWall[nr, nc] || dist[nr, nc] != -1) continue;
                dist[nr, nc] = dist[current.x, current.y] + 1;
                queue.Enqueue(new Vector2Int(nr, nc));

                bool isRoomCell = nr % 2 == 1 && nc % 2 == 1 && !(nr == 1 && nc == 1);
                if (isRoomCell && dist[nr, nc] > maxDist)
                {
                    maxDist = dist[nr, nc];
                    farthest = new Vector2Int(nr, nc);
                }
            }
        }

        return farthest;
    }

    private void BuildGeometry()
    {
        float half = (gridSize - 1) * cellSize * 0.5f;

        CreatePlane(new Vector3(half, 0f, half), new Color(0.25f, 0.15f, 0.08f), false);

        Material wallMat = MakeMaterial(new Color(0.3f, 0.3f, 0.35f));
        GameObject wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(transform);

        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                if (!_isWall[r, c]) continue;
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(wallParent.transform);
                wall.transform.position = new Vector3(c * cellSize, 1.5f, r * cellSize);
                wall.transform.localScale = new Vector3(cellSize, 3f, cellSize);
                wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            }
        }
    }

    private void CreatePlane(Vector3 position, Color color, bool flipUp)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.SetParent(transform);
        plane.transform.position = position;
        plane.transform.localScale = Vector3.one * (gridSize * cellSize / 10f);
        if (flipUp) plane.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        plane.GetComponent<Renderer>().material = MakeMaterial(color);
    }

    private void PlaceActors()
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = new Vector3(cellSize, 1f, cellSize);
        cc.enabled = true;

        GameObject exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exit.transform.position = new Vector3(_exitCell.y * cellSize, 1f, _exitCell.x * cellSize);
        exit.transform.localScale = new Vector3(cellSize * 0.7f, 2f, cellSize * 0.7f);
        exit.GetComponent<BoxCollider>().isTrigger = true;
        exit.AddComponent<ExitTrigger>();

        Material exitMat = MakeMaterial(new Color(0f, 0.8f, 0.2f));
        exitMat.EnableKeyword("_EMISSION");
        exitMat.SetColor(EmissionColorId, new Color(0f, 4f, 0.5f));
        exitMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        exit.GetComponent<Renderer>().material = exitMat;
    }

    private Material MakeMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor(BaseColorId, color);
        return mat;
    }
}