using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneMover : MonoBehaviour, IInteractable
{
    [SerializeField] bool _interactOrEnter;
    [SerializeField] string _sceneName;
    [SerializeField] Vector2 _movePoint;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_interactOrEnter)
            MoveOn();
    }
    public void Interact(GameObject Object)
    {
        if (_interactOrEnter)
            MoveOn();
    }
    void MoveOn()
    {
        GameManager.Instance.LoadScene(_sceneName, _movePoint);
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);

        if (col is BoxCollider2D box)
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube((Vector3)box.offset, box.size);
            Gizmos.matrix = prev;
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
    }
}
