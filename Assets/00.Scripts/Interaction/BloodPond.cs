using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BloodPond : MonoBehaviour
{
    public Action onComplete;
    public float fadeDuration = 0.4f;
    public GameObject trailObject;

    [SerializeField] private int count = 1;
    private bool isTriggered = false;
    Vector2 targetPosition;
    SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (isTriggered)
            MoveToSphere();
    }

    public int GetCount() => count;

    public void ResetState()
    {
        isTriggered = false;
        count = 1;
        if (trailObject != null) trailObject.SetActive(false);
        if (sr != null) { Color c = sr.color; c.a = 1f; sr.color = c; }
    }

    private void MoveToSphere()
    {
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, Time.deltaTime * 2f);
        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (onComplete != null) onComplete();
            else Destroy(gameObject);
        }
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Color startColor = sr != null ? sr.color : Color.white;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                sr.color = c;
            }
            yield return null;
        }
    }

    public void TriggerMovement(Vector2 pos)
    {
        isTriggered = true;
        targetPosition = pos;
        if (trailObject != null) trailObject.SetActive(true);
        StartCoroutine(FadeOut());
    }
}
