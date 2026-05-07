using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Blood cost consumed per spear as it is summoned.")]
    public float bloodCostPerSpear = 10f;
    [Tooltip("Seconds between each spear being summoned while holding.")]
    public float spearInterval = 0.25f;
    [Tooltip("Hard cap on total spears that can orbit at once.")]
    public int maxSpearCount = 10;

    private PlayerControl _player;
    private readonly List<BloodSpear> _activeSpears = new();
    private Coroutine _holdCoroutine;

    void Awake()
    {
        _player = GetComponent<PlayerControl>();
    }

    // Called when Skill button is pressed — start summoning spears one by one
    public void OnSkillPress()
    {
        if (currentSkill == SkillType.BloodSpear)
            _holdCoroutine = StartCoroutine(BloodSpearHoldCoroutine());
    }

    // Called when Skill button is released — fire all summoned spears
    public void OnSkillRelease()
    {
        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }

        if (currentSkill == SkillType.BloodSpear)
            BloodSpearRelease();
    }

    private IEnumerator BloodSpearHoldCoroutine()
    {
        _activeSpears.RemoveAll(s => s == null);

        for (int i = 0; i < spearCount; i++)
        {
            if (_activeSpears.Count >= maxSpearCount) break;
            if (!_player.ConsumeBloodGage(bloodCostPerSpear)) break;

            float angle = 360f / maxSpearCount * _activeSpears.Count * Mathf.Deg2Rad;
            Vector2 offset = new(Mathf.Cos(angle) * hoverRadius, Mathf.Sin(angle) * hoverRadius);

            GameObject spear = Instantiate(bloodSpearPrefab, (Vector2)transform.position + offset, Quaternion.identity);
            if (spear.TryGetComponent<BloodSpear>(out var bs))
            {
                bs.InitHold(transform, offset, _player.FacingDir(), spearDamage, _player.enemyLayer, _player.bloodPuddleMaker);
                _activeSpears.Add(bs);
            }

            yield return new WaitForSeconds(spearInterval);
        }

        _holdCoroutine = null;
    }

    private void BloodSpearRelease()
    {
        _activeSpears.RemoveAll(s => s == null);
        foreach (var spear in _activeSpears)
            spear.Fire();
        _activeSpears.Clear();
    }
}
