using UnityEngine;

/// <summary>
/// Attach to the center object. The center object is unaffected.
/// The orbiter snaps to the point on the orbit circle closest to the mouse pointer.
/// </summary>
public class OrbitFollower : MonoBehaviour
{
    [Header("Orbit")]
    public Transform orbiter;
    public float orbitRadius = 1.5f;
    public float followSpeed = 8f;  // how fast the orbiter rotates toward the mouse angle

    private float angle;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (orbiter != null)
            angle = Vector2.SignedAngle(Vector2.right,
                        (orbiter.position - transform.position).normalized);
    }

    void Update()
    {
        if (orbiter == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;

        float targetAngle = Vector2.SignedAngle(Vector2.right,
                                (mouseWorld - transform.position).normalized);

        angle = Mathf.LerpAngle(angle, targetAngle, followSpeed * Time.deltaTime);

        float rad = angle * Mathf.Deg2Rad;
        orbiter.SetPositionAndRotation(
            transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius,
            Quaternion.Euler(0f, 0f, angle));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);
    }
}