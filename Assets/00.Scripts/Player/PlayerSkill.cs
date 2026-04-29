using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    public enum SkillType { BloodSpear }

    [Header("Current Skill")]
    public SkillType currentSkill = SkillType.BloodSpear;

    [Header("Blood Spear")]
    public GameObject bloodSpearPrefab;
    public int spearCount = 5;
    public float hoverRadius = 1.2f;
    public float spearDamage = 30f;
    public float bloodCost = 25f;

    private PlayerControl _player;

    void Awake()
    {
        _player = GetComponent<PlayerControl>();
    }

    public void UseSkill()
    {
        switch (currentSkill)
        {
            case SkillType.BloodSpear: BloodSpear(); break;
        }
    }

    private void BloodSpear()
    {
        if (bloodSpearPrefab == null || _player == null) return;
        if (!_player.ConsumeBloodGage(bloodCost)) return;

        for (int i = 0; i < spearCount; i++)
        {
            float angle = 360f / spearCount * i * Mathf.Deg2Rad;
            Vector2 offset = new(Mathf.Cos(angle) * hoverRadius, Mathf.Sin(angle) * hoverRadius);

            GameObject spear = Instantiate(bloodSpearPrefab, (Vector2)transform.position + offset, Quaternion.identity);
            if (spear.TryGetComponent<BloodSpear>(out var bs))
                bs.Init(transform, offset, _player.FacingDir(), spearDamage, _player.enemyLayer);
        }
    }
}
