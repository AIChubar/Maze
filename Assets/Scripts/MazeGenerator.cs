using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Settings")]
    [SerializeField] private int gridSize = 21;
    [SerializeField] private float cellSize = 3f;
    [SerializeField] private float secondsPerStep = 0.6f;
    [SerializeField] private int simulationRuns = 200;

    [Header("Visualization")]
    [SerializeField] private bool visualize;
    [SerializeField] private float stepDelay = 0.05f;

    [SerializeReference] private MazeAlgorithm algorithm = new WilsonAlgorithm();

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private FloorMarker floorMarker;

    private bool[,] _isWall;
    private Vector2Int _exitCell;
    private GameObject[,] _wallCubes;
    private bool _wallsReady;

    private void OnValidate()
    {
        if (gridSize % 2 == 0) gridSize++;
        gridSize = Mathf.Max(5, gridSize);
    }

    private void Start()
    {
        Run();
    }

    public void FillWalls()
    {
        StopAllCoroutines();
        CleanScene();

        _isWall = new bool[gridSize, gridSize];
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                _isWall[r, c] = true;

        BuildAnimatedWalls();
        _wallsReady = true;
    }

    public void Regenerate()
    {
        StopAllCoroutines();

        if (visualize && _wallsReady)
        {
            _wallsReady = false;
            for (int r = 0; r < gridSize; r++)
                for (int c = 0; c < gridSize; c++)
                    _isWall[r, c] = true;
            StartCoroutine(RunAnimated(skipBuild: true));
            return;
        }

        _wallsReady = false;
        CleanScene();
        Run();
    }

    private void CleanScene()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        ExitTrigger existingExit = FindFirstObjectByType<ExitTrigger>();
        if (existingExit != null)
            Destroy(existingExit.gameObject);
    }

    private void Run()
    {
        if (visualize)
            StartCoroutine(RunAnimated());
        else
            RunInstant();
    }

    private void RunInstant()
    {
        GenerateMaze();
        BuildGeometry();
        FinishGeneration();
    }

    private IEnumerator RunAnimated(bool skipBuild = false)
    {
        if (!skipBuild)
        {
            _isWall = new bool[gridSize, gridSize];
            for (int r = 0; r < gridSize; r++)
                for (int c = 0; c < gridSize; c++)
                    _isWall[r, c] = true;

            BuildAnimatedWalls();
        }

        yield return algorithm.GenerateAnimated(gridSize, _isWall, stepDelay, OnCarve);
        FinishGeneration();
    }

    private void FinishGeneration()
    {
        _exitCell = FindFarthestCell();
        float timeLimit = RunAgentSimulations(simulationRuns) * secondsPerStep;
        GameManager.Instance.StartGame(timeLimit);
        LogMazeSchema();
        PlaceActors();
        SetMarkLimit();
    }

    private void SetMarkLimit()
    {
        int paintable = 0;
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                if (!_isWall[r, c]) paintable++;
        floorMarker.SetMarkLimit(Mathf.Max(1, paintable / 8));
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
        _isWall = algorithm.Generate(gridSize);
    }

    private int RunAgentSimulations(int count)
    {
        int total = 0;
        for (int i = 0; i < count; i++)
            total += SimulateAgent();
        return total / count;
    }

    private int SimulateAgent()
    {
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        bool[,] deadEnd = new bool[gridSize, gridSize];
        bool[,] visited = new bool[gridSize, gridSize];
        List<Vector2Int> path = new List<Vector2Int>();

        path.Add(new Vector2Int(1, 1));
        visited[1, 1] = true;
        int steps = 0;

        while (path[path.Count - 1] != _exitCell)
        {
            Vector2Int current = path[path.Count - 1];
            List<Vector2Int> unvisited = new List<Vector2Int>();

            for (int d = 0; d < 4; d++)
            {
                int nr = current.x + dr[d];
                int nc = current.y + dc[d];
                if (nr < 0 || nr >= gridSize || nc < 0 || nc >= gridSize) continue;
                if (_isWall[nr, nc] || deadEnd[nr, nc] || visited[nr, nc]) continue;
                unvisited.Add(new Vector2Int(nr, nc));
            }

            if (unvisited.Count > 0)
            {
                Vector2Int next = unvisited[Random.Range(0, unvisited.Count)];
                path.Add(next);
                visited[next.x, next.y] = true;
                steps++;
            }
            else
            {
                int validExits = 0;
                for (int d = 0; d < 4; d++)
                {
                    int nr = current.x + dr[d];
                    int nc = current.y + dc[d];
                    if (nr < 0 || nr >= gridSize || nc < 0 || nc >= gridSize) continue;
                    if (!_isWall[nr, nc] && !deadEnd[nr, nc]) validExits++;
                }
                if (validExits <= 1)
                    deadEnd[current.x, current.y] = true;

                path.RemoveAt(path.Count - 1);
                steps++;
            }
        }

        return steps;
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

    private void BuildAnimatedWalls()
    {
        float half = (gridSize - 1) * cellSize * 0.5f;
        CreatePlane(new Vector3(half, 0f, half), new Color(0.25f, 0.15f, 0.08f), false);

        Material wallMat = MakeMaterial(new Color(0.3f, 0.3f, 0.35f));
        GameObject wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(transform);

        _wallCubes = new GameObject[gridSize, gridSize];
        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(wallParent.transform);
                wall.transform.position = new Vector3(c * cellSize, 1.5f, r * cellSize);
                wall.transform.localScale = new Vector3(cellSize, 3f, cellSize);
                wall.GetComponent<Renderer>().sharedMaterial = wallMat;
                _wallCubes[r, c] = wall;
            }
    }

    private void OnCarve(int r, int c)
    {
        Destroy(_wallCubes[r, c]);
        _wallCubes[r, c] = null;
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