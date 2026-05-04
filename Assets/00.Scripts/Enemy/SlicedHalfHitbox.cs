using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SlicedHalfHitbox : MonoBehaviour
{
    SlicedHalf _parent;

    void Awake()
    {
        _parent = GetComponentInParent<SlicedHalf>();
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (_parent == null) return;

        if ((_parent.groundLayer.value & (1 << col.gameObject.layer)) != 0)
            _parent.OnHitGround();
        else
            _parent.OnHitEnemy(col);
    }
}
