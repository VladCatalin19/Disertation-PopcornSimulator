using UnityEngine;

public class TempMiddlePartExpander : MonoBehaviour
{
    [SerializeField] private Mesh[] puffMeshes = null;
    [Space]
    [SerializeField] private Vector3 initialLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 finalLocalPosition = Vector3.zero;
    [Space]
    [SerializeField] private Vector3 initialLocalRotation = Vector3.zero;
    [SerializeField] private Vector3 finalLocalRotation = Vector3.zero;
    [Space]
    [SerializeField] private Vector3 initialLocalScale = Vector3.zero;
    [SerializeField] private Vector3 finalLocalScale = Vector3.zero;
    [Space]
    [SerializeField] private float expansionTime = 0.0f;
    [Space]
    [SerializeField] private AnimationCurve positionCurve = null;
    [SerializeField] private AnimationCurve rotationCurve = null;
    [SerializeField] private AnimationCurve scaleCurve = null;

    private float elapsedTime = 0.0f;

    private void Awake()
    {
        elapsedTime = 0.0f;
        transform.localPosition = initialLocalPosition;
        transform.localEulerAngles = initialLocalRotation;
        transform.localScale = initialLocalScale;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();

        meshFilter.mesh = puffMeshes[Random.Range(0, puffMeshes.Length)];
        meshCollider.enabled = true;
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        enabled = false;
    }

    private void LateUpdate()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime > expansionTime)
        {
            transform.localPosition = finalLocalPosition;
            transform.localEulerAngles = finalLocalRotation;
            transform.localScale = finalLocalScale;
            enabled = false;

            return;
        }

        float positionInterp = positionCurve.Evaluate(elapsedTime / expansionTime);
        float rotationInterp = rotationCurve.Evaluate(elapsedTime / expansionTime);
        float scaleInterp = scaleCurve.Evaluate(elapsedTime / expansionTime);

        transform.localPosition = Vector3.Lerp(initialLocalPosition, finalLocalPosition, positionInterp);
        transform.localEulerAngles = Vector3.Lerp(initialLocalRotation, finalLocalRotation, rotationInterp);
        transform.localScale = Vector3.Lerp(initialLocalScale, finalLocalScale, scaleInterp);
    }
}
