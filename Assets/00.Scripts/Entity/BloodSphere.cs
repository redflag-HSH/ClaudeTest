using System.Linq;
using UnityEngine;

public class BloodSphere : MonoBehaviour
{
    [SerializeField] private float _gatherRadius = 5f;
    public int bloodCount = 0;
    public int moneyValue = 0;
    public float hpHeal = 0f;
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
                player.AddBloodMoney(moneyValue);
                if (hpHeal > 0f) player.Heal(hpHeal);
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
                moneyValue += bloodPond.moneyValue;
                hpHeal += bloodPond.hpHeal;
                bloodPond.TriggerMovement(transform.position);
                _pondCount++;
                bloodPond.onComplete += () => _absorbedCount++;
            }
        }
        if (bloodCount == 0)
        {
            Debug.Log("No blood ponds found within the gather radius.");
            Destroy(gameObject);
        }
    }
}
