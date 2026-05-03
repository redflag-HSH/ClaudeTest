using UnityEngine;

/// <summary>
/// Place this on an invisible marker GameObject where an enemy should live.
/// Spawns the enemy on Start and respawns it every time any bonfire is rested at.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    [Tooltip("Seconds to wait before respawning after a bonfire rest.")]
    public float respawnDelay = 0f;

    private GameObject _current;

    // ──────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────────────────────

    private void Start()
    {
        Spawn();
    }

    private void OnEnable()
    {
        Bonfire.OnAnyBonfireRest += OnBonfireRest;
    }

    private void OnDisable()
    {
        Bonfire.OnAnyBonfireRest -= OnBonfireRest;
    }

    // ──────────────────────────────────────────────────────────────
    //  Respawn
    // ──────────────────────────────────────────────────────────────

    private void OnBonfireRest()
    {
        if (respawnDelay > 0f)
            StartCoroutine(RespawnAfterDelay());
        else
            Respawn();
    }

    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        if (_current != null)
            Destroy(_current);
        Spawn();
    }

    private void Spawn()
    {
        if (enemyPrefab == null) return;
        _current = Instantiate(enemyPrefab, transform.position, transform.rotation);
    }

    // ──────────────────────────────────────────────────────────────
    //  Editor Debug
    // ──────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Debug / Force Respawn")]
    private void Debug_ForceRespawn() => Respawn();

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawIcon(transform.position + Vector3.up * 0.5f, "sv_icon_dot4_pix16_gizmo", true);
    }
#endif
}
