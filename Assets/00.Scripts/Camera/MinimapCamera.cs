using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public Vector2 offset = Vector2.zero;
    public float smoothSpeed = 8f;
    public bool snapInstantly = false;

    [Header("Rotation")]
    public bool rotateWithTarget = false;

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private float _cameraZ;
    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
        _cameraZ = transform.position.z;

        if (target == null && PlayerControl.Instance != null)
            target = PlayerControl.Instance.transform;

        if (snapInstantly && target != null)
            SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = GetDesiredPosition();

        if (snapInstantly)
            transform.position = desired;
        else
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        if (rotateWithTarget)
            transform.rotation = Quaternion.Euler(0f, 0f, -target.eulerAngles.z);
        else
            transform.rotation = Quaternion.identity;
    }

    Vector3 GetDesiredPosition()
    {
        float x = target.position.x + offset.x;
        float y = target.position.y + offset.y;

        if (useBounds)
        {
            x = Mathf.Clamp(x, minBounds.x, maxBounds.x);
            y = Mathf.Clamp(y, minBounds.y, maxBounds.y);
        }

        return new Vector3(x, y, _cameraZ);
    }

    // ── Public API ───────────────────────────────────────────────

    public void SetTarget(Transform newTarget) => target = newTarget;

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = GetDesiredPosition();
    }

    public void SetOffset(Vector2 newOffset) => offset = newOffset;

    public void SetOrthographicSize(float size) => _cam.orthographicSize = size;

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    public void DisableBounds() => useBounds = false;
}
