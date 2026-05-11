using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FloorMarker : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Color primaryColor = new Color(1f, 0.75f, 0f);
    [SerializeField] private Color secondaryColor = Color.red;
    [SerializeField] private float cellSize = 2f;
    [SerializeField] private float maxReach = 4f;
    [SerializeField] private float paintDuration = 0.35f;
    [SerializeField] private bool animatePaint = true;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private TextMeshProUGUI markCountText;

    private readonly Dictionary<Vector2Int, GameObject> _marks = new Dictionary<Vector2Int, GameObject>();
    private readonly HashSet<Vector2Int> _animating = new HashSet<Vector2Int>();
    private GameObject _preview;
    private int _maxMarks;
    private int _availableMarks;

    private void Start()
    {
        _preview = CreateQuad(Vector3.zero, MakeTransparentMaterial(new Color(1f, 1f, 1f, 0.4f)));
        _preview.SetActive(false);
    }

    public void SetMarkLimit(int limit)
    {
        _maxMarks = limit;
        _availableMarks = limit;
        UpdateMarkDisplay();
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying)
        {
            _preview.SetActive(false);
            return;
        }

        bool holdingRmb = Mouse.current.rightButton.isPressed;

        if (!holdingRmb)
        {
            _preview.SetActive(false);
            return;
        }

        Vector2Int? cell = GetPointedCell();

        if (cell == null)
        {
            _preview.SetActive(false);
            return;
        }

        _preview.SetActive(true);
        _preview.transform.position = CellToWorld(cell.Value);

        if (Keyboard.current.eKey.wasPressedThisFrame && !_animating.Contains(cell.Value))
            Paint(cell.Value, primaryColor);
        else if (Keyboard.current.fKey.wasPressedThisFrame && !_animating.Contains(cell.Value))
            Paint(cell.Value, secondaryColor);
        else if (Keyboard.current.gKey.wasPressedThisFrame)
            Remove(cell.Value);
    }

    private void Paint(Vector2Int cell, Color color)
    {
        bool isOverwrite = _marks.ContainsKey(cell);
        if (!isOverwrite && _availableMarks <= 0) return;

        if (!isOverwrite)
        {
            _availableMarks--;
            UpdateMarkDisplay();
        }

        if (animatePaint)
        {
            StartCoroutine(PaintAnimation(cell, color));
            return;
        }

        if (_marks.TryGetValue(cell, out GameObject existing))
        {
            Destroy(existing);
            _marks.Remove(cell);
        }
        _marks[cell] = CreateQuad(CellToWorld(cell), MakeMaterial(color));
    }

    private IEnumerator PaintAnimation(Vector2Int cell, Color color)
    {
        _animating.Add(cell);

        if (_marks.TryGetValue(cell, out GameObject existing))
        {
            Destroy(existing);
            _marks.Remove(cell);
        }

        Material animMat = MakeTransparentMaterial(new Color(color.r, color.g, color.b, 0f));
        GameObject animQuad = CreateQuad(CellToWorld(cell), animMat, y: 0.04f);
        animQuad.transform.localScale = Vector3.zero;

        float fullScale = cellSize * 0.95f;
        float overshootScale = fullScale * 1.15f;
        float growDuration = paintDuration * 0.75f;
        float settleDuration = paintDuration * 0.25f;
        float elapsed = 0f;

        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / growDuration);
            float size = Mathf.Lerp(0f, overshootScale, t);
            animQuad.transform.localScale = new Vector3(size, size, 1f);
            animMat.SetColor(BaseColorId, new Color(color.r, color.g, color.b, t));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / settleDuration;
            float size = Mathf.Lerp(overshootScale, fullScale, t);
            animQuad.transform.localScale = new Vector3(size, size, 1f);
            yield return null;
        }

        Destroy(animQuad);
        _marks[cell] = CreateQuad(CellToWorld(cell), MakeMaterial(color));
        _animating.Remove(cell);
    }

    private Vector2Int? GetPointedCell()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxReach)) return null;
        if (hit.normal != Vector3.up) return null;
        return WorldToCell(hit.point);
    }

    private void Remove(Vector2Int cell)
    {
        if (!_marks.TryGetValue(cell, out GameObject existing)) return;
        Destroy(existing);
        _marks.Remove(cell);
        _availableMarks = Mathf.Min(_availableMarks + 1, _maxMarks);
        UpdateMarkDisplay();
    }

    private void UpdateMarkDisplay()
    {
        if (markCountText != null)
            markCountText.text = $"Marks: {_availableMarks}/{_maxMarks}";
    }

    private GameObject CreateQuad(Vector3 position, Material mat, float y = 0.03f)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.position = new Vector3(position.x, y, position.z);
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);
        Destroy(quad.GetComponent<Collider>());
        quad.GetComponent<Renderer>().material = mat;
        return quad;
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        int c = Mathf.RoundToInt(worldPos.x / cellSize);
        int r = Mathf.RoundToInt(worldPos.z / cellSize);
        return new Vector2Int(r, c);
    }

    private Vector3 CellToWorld(Vector2Int cell) =>
        new Vector3(cell.y * cellSize, 0.03f, cell.x * cellSize);

    private Material MakeMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor(BaseColorId, color);
        return mat;
    }

    private Material MakeTransparentMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetColor(BaseColorId, color);
        return mat;
    }
}
