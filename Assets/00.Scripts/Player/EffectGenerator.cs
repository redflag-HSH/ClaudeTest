using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(PlayerControl))]
public class EffectGenerator : MonoBehaviour
{
    [Header("Sliced Limbs")]
    public GameObject slicedLeftArmPrefab;
    public GameObject slicedRightArmPrefab;
    public GameObject slicedLeftLegPrefab;
    public GameObject slicedRightLegPrefab;

    [Header("Blood")]
    public GameObject bloodPrefab;
    [Tooltip("Max blood objects kept alive in the pool.")]
    public int bloodPoolSize = 20;

    ObjectPool<GameObject> _bloodPool;

    // ParticleSystem cached per pooled object — avoids repeated GetComponent calls
    readonly Dictionary<GameObject, ParticleSystem> _psCache = new();

    // Reused fallback wait — avoids allocating a new WaitForSeconds each call
    static readonly WaitForSeconds s_fallbackWait = new WaitForSeconds(2f);

    void Awake()
    {
        _bloodPool = new ObjectPool<GameObject>(
            createFunc:      CreateBlood,
            actionOnGet:     OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnPoolDestroy,
            collectionCheck: false,
            defaultCapacity: bloodPoolSize / 2,
            maxSize:         bloodPoolSize
        );
    }

    public void SpawnBlood(Vector2 point, Vector2 normal)
    {
        if (bloodPrefab == null) return;

        GameObject obj = _bloodPool.Get();
        obj.transform.SetPositionAndRotation(point, Quaternion.FromToRotation(Vector2.up, normal));

        if (_psCache.TryGetValue(obj, out var ps) && ps != null)
            ps.Play();

        StartCoroutine(ReturnWhenDone(obj));
    }

    // ── Pool callbacks ────────────────────────────────────────────────────────

    GameObject CreateBlood()
    {
        var obj = Instantiate(bloodPrefab);
        var ps  = obj.GetComponent<ParticleSystem>();
        _psCache[obj] = ps;

        if (ps != null)
        {
            var main = ps.main;
            main.playOnAwake = false;
        }

        obj.SetActive(false);
        return obj;
    }

    void OnGet(GameObject obj) => obj.SetActive(true);

    void OnRelease(GameObject obj)
    {
        if (_psCache.TryGetValue(obj, out var ps) && ps != null)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        obj.SetActive(false);
    }

    void OnPoolDestroy(GameObject obj)
    {
        _psCache.Remove(obj);
        Destroy(obj);
    }

    // ── Return logic ──────────────────────────────────────────────────────────

    // ── Sliced Limbs ──────────────────────────────────────────────────────────

    public void SpawnSlicedLimb(BodyPart part, Vector2 position)
    {
        GameObject prefab = part switch
        {
            BodyPart.LeftArm  => slicedLeftArmPrefab,
            BodyPart.RightArm => slicedRightArmPrefab,
            BodyPart.LeftLeg  => slicedLeftLegPrefab,
            BodyPart.RightLeg => slicedRightLegPrefab,
            _                 => null
        };

        if (prefab == null) return;
        Instantiate(prefab, position, Quaternion.identity);
    }

    // ── Return logic ──────────────────────────────────────────────────────────

    IEnumerator ReturnWhenDone(GameObject obj)
    {
        if (_psCache.TryGetValue(obj, out var ps) && ps != null)
            yield return new WaitUntil(() => !ps.IsAlive(true));
        else
            yield return s_fallbackWait;

        if (obj != null && _bloodPool != null)
            _bloodPool.Release(obj);
    }
}
