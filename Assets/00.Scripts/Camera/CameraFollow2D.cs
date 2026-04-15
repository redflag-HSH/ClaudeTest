using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public Vector2 offset = Vector2.zero;
    public float smoothSpeed = 5f;

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Axis Lock")]
    public bool lockX = false;
    public bool lockY = false;

    [Header("Shake")]
    public float shakeFrequency = 25f;   // oscillations per second

    private float _cameraZ;

    // shake state
    private float _shakeDuration;
    private float _shakeTimer;
    private float _shakeMagnitude;
    private float _shakeDecay;           // magnitude lost per second (0 = constant)
    private Vector2 _shakeOffset;

    void Start()
    {
        _cameraZ = transform.position.z;

        if (target == null && PlayerControl.Instance != null)
            target = PlayerControl.Instance.transform;
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdateShake();

        Vector3 desired = GetDesiredPosition() + (Vector3)_shakeOffset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }

    void UpdateShake()
    {
        if (_shakeTimer <= 0f) { _shakeOffset = Vector2.zero; return; }

        _shakeTimer -= Time.deltaTime;
        float currentMag = _shakeMagnitude - _shakeDecay * (_shakeDuration - _shakeTimer);
        currentMag = Mathf.Max(currentMag, 0f);

        float t = (_shakeDuration - _shakeTimer) * shakeFrequency;
        _shakeOffset = new Vector2(
            Mathf.Sin(t * 1.00f) * currentMag,
            Mathf.Sin(t * 1.37f) * currentMag   // different frequency on Y for less uniformity
        );

        if (_shakeTimer <= 0f) _shakeOffset = Vector2.zero;
    }

    // Returns the smoothed target position (respects locks and bounds)
    Vector3 GetDesiredPosition()
    {
        float x = lockX ? transform.position.x : target.position.x + offset.x;
        float y = lockY ? transform.position.y : target.position.y + offset.y;

        if (useBounds)
        {
            x = Mathf.Clamp(x, minBounds.x, maxBounds.x);
            y = Mathf.Clamp(y, minBounds.y, maxBounds.y);
        }

        return new Vector3(x, y, _cameraZ);
    }

    // ── Public API ──────────────────────────────────────────────

    /// <summary>Set a new follow target at runtime.</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;

    /// <summary>Snap the camera instantly to the target (no smoothing).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = GetDesiredPosition();
    }

    /// <summary>Set camera offset from the target.</summary>
    public void SetOffset(Vector2 newOffset) => offset = newOffset;

    /// <summary>Enable or disable world-space camera bounds.</summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    public void DisableBounds() => useBounds = false;

    /// <summary>Lock or unlock each axis independently.</summary>
    public void SetAxisLock(bool x, bool y) { lockX = x; lockY = y; }

    // ── Shake API ────────────────────────────────────────────────

    /// <summary>Shake with constant magnitude for a duration.</summary>
    public void Shake(float duration, float magnitude)
    {
        _shakeDuration  = duration;
        _shakeTimer     = duration;
        _shakeMagnitude = magnitude;
        _shakeDecay     = 0f;
    }

    /// <summary>Shake that fades out smoothly over the duration.</summary>
    public void ShakeFadeOut(float duration, float magnitude)
    {
        _shakeDuration  = duration;
        _shakeTimer     = duration;
        _shakeMagnitude = magnitude;
        _shakeDecay     = magnitude / duration;   // reaches 0 at end
    }

    /// <summary>Stop any ongoing shake immediately.</summary>
    public void StopShake()
    {
        _shakeTimer    = 0f;
        _shakeOffset   = Vector2.zero;
    }
}