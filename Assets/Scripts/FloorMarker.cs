using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FloorMarker : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Color primaryColor = new Color(1f, 0.75f, 0f);
    [SerializeField] private Color secondaryColor = Color.red;
    [SerializeField] private float cellSize = 2f;
    [SerializeField] private float maxReach = 4f;
    [SerializeField] private Transform cameraTransform;

    private readonly Dictionary<Vector2Int, GameObject> _marks = new Dictionary<Vector2Int, GameObject>();
    private GameObject _preview;

    private void Start()
    {
        _preview = CreateQuad(Vector3.zero, MakeTransparentMaterial(new Color(1f, 1f, 1f, 0.4f)));
        _preview.SetActive(false);
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

        if (Keyboard.current.eKey.wasPressedThisFrame)
            Paint(cell.Value, primaryColor);
        else if (Keyboard.current.fKey.wasPressedThisFrame)
            Paint(cell.Value, secondaryColor);
        else if (Keyboard.current.gKey.wasPressedThisFrame)
            Remove(cell.Value);
    }

    private Vector2Int? GetPointedCell()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxReach)) return null;
        if (hit.normal != Vector3.up) return null;
        return WorldToCell(hit.point);
    }

    private void Paint(Vector2Int cell, Color color)
    {
        if (_marks.TryGetValue(cell, out GameObject existing))
            Destroy(existing);
        _marks[cell] = CreateQuad(CellToWorld(cell), MakeMaterial(color));
    }

    private void Remove(Vector2Int cell)
    {
        if (!_marks.TryGetValue(cell, out GameObject existing)) return;
        Destroy(existing);
        _marks.Remove(cell);
    }

    private GameObject CreateQuad(Vector3 position, Material mat)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.position = position;
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
