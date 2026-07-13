using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Ladder : MonoBehaviour
{
    [Tooltip("Where the player is placed after climbing off the top. Leave empty to use collider top + offset.")]
    [SerializeField] Transform topExitPoint;

    [Tooltip("Vertical offset above the collider top when no topExitPoint is set.")]
    [SerializeField] float topExitOffset = 0.6f;

    Collider2D _col;

    public float LadderX  => transform.position.x;
    public float TopY     => topExitPoint != null ? topExitPoint.position.y : _col.bounds.max.y + topExitOffset;
    public float BottomY  => _col.bounds.min.y;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerControl>(out var player))
            player.SetNearLadder(true, this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerControl>(out var player))
            player.SetNearLadder(false, null);
    }
}
