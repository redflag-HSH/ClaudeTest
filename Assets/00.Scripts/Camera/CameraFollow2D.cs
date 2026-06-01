using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    static CameraFollow2D _mainCameraInstance;
    public static CameraFollow2D Instance => _mainCameraInstance;
    // Falls back to Camera.main when CameraFollow2D is on a vcam (no Camera component on it)
    public static Camera GameCamera => (_mainCameraInstance != null && _mainCameraInstance._cam != null)
        ? _mainCameraInstance._cam
        : Camera.main;

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
    public float shakeFrequency = 25f;

    private Camera _cam;
    private float _cameraZ;

    // cutscene state
    private Transform _cutsceneTarget;
    private Transform _savedTarget;

    // shake state
    private float _shakeDuration;
    private float _shakeTimer;
    private float _shakeMagnitude;
    private float _shakeDecay;
    private Vector2 _shakeOffset;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _mainCameraInstance = this;
    }

    void Start()
    {
        _mainCameraInstance = this;
        // When on a vcam, use the vcam's Z; when on the camera directly, use its Z
        _cameraZ = transform.position.z;

        if (target == null && PlayerControl.Instance != null)
            target = PlayerControl.Instance.transform;

        SnapToTarget();
    }

    void LateUpdate()
    {
        // Cinemachine Brain owns the camera during cutscenes — do nothing
        if (_cutsceneTarget != null) return;

        if (target == null) return;

        UpdateShake();
        Vector3 gameplay = GetDesiredPosition() + (Vector3)_shakeOffset;
        transform.position = Vector3.Lerp(transform.position, gameplay, smoothSpeed * Time.deltaTime);
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
            Mathf.Sin(t * 1.37f) * currentMag
        );

        if (_shakeTimer <= 0f) _shakeOffset = Vector2.zero;
    }

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

    // ── Cutscene API ─────────────────────────────────────────────

    /// <summary>
    /// Start following a cutscene vcam. Pass a smoothSpeed override (default uses gameplay speed).
    /// </summary>
    public void StartCutscene(Transform vcam)
    {
        _savedTarget = target;
        _cutsceneTarget = vcam;
        StopShake();
    }

    /// <summary>
    /// Switch to a different vcam mid-cutscene (e.g. Timeline vcam change).
    /// </summary>
    public void SetCutsceneVCam(Transform vcam) => _cutsceneTarget = vcam;

    /// <summary>
    /// End cutscene and resume following the saved gameplay target.
    /// </summary>
    public void EndCutscene()
    {
        _cutsceneTarget = null;
        target = _savedTarget;
        _cameraZ = transform.position.z;
        StopShake();
    }

    // ── Gameplay API ─────────────────────────────────────────────

    public void SetTarget(Transform newTarget) => target = newTarget;

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = GetDesiredPosition();
    }

    public void SetOffset(Vector2 newOffset) => offset = newOffset;

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    public void DisableBounds() => useBounds = false;

    public void SetAxisLock(bool x, bool y) { lockX = x; lockY = y; }

    // ── Shake API ────────────────────────────────────────────────

    public void ShakeTimeLine(float duration) => Shake(duration, 1.0f);


    public void Shake(float duration, float magnitude)
    {
        _shakeDuration = duration;
        _shakeTimer = duration;
        _shakeMagnitude = magnitude;
        _shakeDecay = 0f;
    }

    public void ShakeFadeOut(float duration, float magnitude)
    {
        _shakeDuration = duration;
        _shakeTimer = duration;
        _shakeMagnitude = magnitude;
        _shakeDecay = magnitude / duration;
    }

    public void StopShake()
    {
        _shakeTimer = 0f;
        _shakeOffset = Vector2.zero;
    }
}
