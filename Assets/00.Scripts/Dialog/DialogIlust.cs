using UnityEngine;

/// <summary>
/// Controls the left and right character portrait SpriteRenderers.
/// Called by DialogSystem each time a new line is shown.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DialogIlust : MonoBehaviour
{
    [SerializeField] Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    [Header("Portraits")]
    [SerializeField] SpriteRenderer portraitLeft;
    [SerializeField] SpriteRenderer portraitRight;

    [Tooltip("Alpha of the portrait that is NOT speaking.")]
    [Range(0f, 1f)]
    [SerializeField] float dimmedAlpha = 0.45f;

    void Start()
    {
        ToggleShow();
    }

    public void Apply(DialogLine line)
    {
        bool leftSpeaking = line.side == DialogSide.Left;

        if (portraitLeft != null)
        {
            if (leftSpeaking && line.portrait != null)
                portraitLeft.sprite = line.portrait;
            SetAlpha(portraitLeft, leftSpeaking ? 1f : dimmedAlpha);
        }

        if (portraitRight != null)
        {
            if (!leftSpeaking && line.portrait != null)
                portraitRight.sprite = line.portrait;

            SetAlpha(portraitRight, leftSpeaking ? dimmedAlpha : 1f);
        }
    }

    void OnDrawGizmos()
    {
        if (cam == null) return;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0f));
    }

    static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    public void ToggleShow()
    {
        bool isActive = portraitLeft.gameObject.activeSelf;
        portraitLeft.gameObject.SetActive(!isActive);
        portraitRight.gameObject.SetActive(!isActive);
    }
}
