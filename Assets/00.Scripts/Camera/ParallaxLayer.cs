using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax")]
    [Tooltip("0 = fixed, 1 = moves with camera (no parallax). X and Y controlled independently.")]
    public Vector2 parallaxFactor = new Vector2(0.5f, 0f);

    [Header("Infinite Scroll")]
    public bool infiniteHorizontal = false;
    public bool infiniteVertical = false;

    private Transform _cam;
    private Vector3 _lastCamPos;
    private float _spriteWidth;
    private float _spriteHeight;

    void Start()
    {
        _cam = CameraFollow2D.GameCamera.transform;
        _lastCamPos = _cam.position;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            _spriteWidth = sr.bounds.size.x;
            _spriteHeight = sr.bounds.size.y;
        }
    }

    void LateUpdate()
    {
        Vector3 delta = _cam.position - _lastCamPos;

        transform.position += new Vector3(
            delta.x * parallaxFactor.x,
            delta.y * parallaxFactor.y,
            0f
        );

        _lastCamPos = _cam.position;

        if (infiniteHorizontal && _spriteWidth > 0f)
        {
            float distX = _cam.position.x - transform.position.x;
            if (Mathf.Abs(distX) >= _spriteWidth)
                transform.position += new Vector3(Mathf.Sign(distX) * _spriteWidth, 0f, 0f);
        }

        if (infiniteVertical && _spriteHeight > 0f)
        {
            float distY = _cam.position.y - transform.position.y;
            if (Mathf.Abs(distY) >= _spriteHeight)
                transform.position += new Vector3(0f, Mathf.Sign(distY) * _spriteHeight, 0f);
        }
    }
}
