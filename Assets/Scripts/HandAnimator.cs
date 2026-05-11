using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimator : MonoBehaviour
{
    [SerializeField] private float swingDuration = 0.2f;

    private Transform _hand;
    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private bool _swinging;

    private void Start()
    {
        _hand = CreateHand();
        _restPosition = _hand.localPosition;
        _restRotation = _hand.localRotation;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying || _swinging) return;

        bool holdingRmb = Mouse.current.rightButton.isPressed;
        bool acted = holdingRmb && (
            Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.fKey.wasPressedThisFrame ||
            Keyboard.current.gKey.wasPressedThisFrame
        );

        if (acted)
            StartCoroutine(Swing());
    }

    private Transform CreateHand()
    {
        // Arm (lower, thicker)
        GameObject arm = new GameObject("Hand");
        arm.transform.SetParent(transform);
        arm.transform.localPosition = new Vector3(0.22f, -0.28f, 0.42f);
        arm.transform.localRotation = Quaternion.Euler(8f, -12f, 4f);

        GameObject armMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(armMesh.GetComponent<Collider>());
        armMesh.transform.SetParent(arm.transform);
        armMesh.transform.localPosition = Vector3.zero;
        armMesh.transform.localRotation = Quaternion.identity;
        armMesh.transform.localScale = new Vector3(0.11f, 0.09f, 0.32f);
        armMesh.GetComponent<Renderer>().material = MakeSkinMat();

        // Fist on top
        GameObject fist = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(fist.GetComponent<Collider>());
        fist.transform.SetParent(arm.transform);
        fist.transform.localPosition = new Vector3(0f, 0.01f, 0.18f);
        fist.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        fist.transform.localScale = new Vector3(0.12f, 0.10f, 0.14f);
        fist.GetComponent<Renderer>().material = MakeSkinMat();

        return arm.transform;
    }

    private static Material MakeSkinMat()
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor(Shader.PropertyToID("_BaseColor"), new Color(0.82f, 0.64f, 0.51f));
        return mat;
    }

    private IEnumerator Swing()
    {
        _swinging = true;

        Vector3 forwardPos = _restPosition + new Vector3(0f, -0.08f, 0.15f);
        Quaternion forwardRot = _restRotation * Quaternion.Euler(35f, 0f, 0f);

        float half = swingDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
            _hand.localPosition = Vector3.Lerp(_restPosition, forwardPos, t);
            _hand.localRotation = Quaternion.Slerp(_restRotation, forwardRot, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
            _hand.localPosition = Vector3.Lerp(forwardPos, _restPosition, t);
            _hand.localRotation = Quaternion.Slerp(forwardRot, _restRotation, t);
            yield return null;
        }

        _hand.localPosition = _restPosition;
        _hand.localRotation = _restRotation;
        _swinging = false;
    }
}
