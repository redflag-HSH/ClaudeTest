using System.Linq;
using UnityEngine;

public class BloodSphere : MonoBehaviour
{
    [SerializeField] private float _gatherRadius = 5f;
    public int bloodCount = 0;
    private int _pondCount = 0, _absorbedCount = 0;
    void Start()
    {
        GatherBloodPonds();
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        CollisionCheck(collision);
    }
    void OnTriggerStay2D(Collider2D collision)
    {
        CollisionCheck(collision);
    }
    private void CollisionCheck(Collider2D collision)
    {
        if (collision.CompareTag("Player") && _absorbedCount >= _pondCount)
        {
            PlayerControl player = collision.GetComponent<PlayerControl>();
            if (player != null)
            {
                player.AddBloodGage(bloodCount);
            }
            Destroy(gameObject);
        }
    }
    private void GatherBloodPonds()
    {
        Collider2D[] ponds = Physics2D.OverlapCircleAll(transform.position, _gatherRadius);
        foreach (Collider2D pond in ponds)
        {
            BloodPond bloodPond = pond.GetComponent<BloodPond>();
            if (bloodPond != null)
            {
                bloodCount += bloodPond.GetCount();
                bloodPond.TriggerMovement(transform.position);
                _pondCount++;
                bloodPond.onComplete += () => _absorbedCount++;
            }
        }
        if (bloodCount == 0)
        {
            // 주변에 혈구가 없을 때의 처리 (예: 혈구가 없다는 메시지를 출력하거나, 혈구가 없음을 나타내는 효과를 발동)
            Debug.Log("No blood ponds found within the gather radius.");
            Destroy(gameObject);
        }
    }
}
