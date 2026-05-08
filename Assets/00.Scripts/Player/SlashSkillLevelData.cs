using System;
using UnityEngine;

[Serializable]
public class QuickdrawLevel
{
    public float range       = 6f;
    public float damage      = 30f;
    public float staminaCost = 30f;
    public float cooldown    = 1.0f;
}

[Serializable]
public class RisingAttackLevel
{
    public float damage      = 20f;
    public float range       = 1.0f;
    public float knockback   = 12f;
    public float staminaCost = 20f;
    public float startup     = 0.08f;
    public float recovery    = 0.3f;
}

[Serializable]
public class SmashdownLevel
{
    public float damage      = 25f;
    public float range       = 0.9f;
    public float knockback   = 6f;
    public float staminaCost = 20f;
    public float startup     = 0.1f;
    public float recovery    = 0.35f;
    public float cooldown    = 1.0f;
}

[Serializable]
public class BodySlamLevel
{
    public float staminaCost   = 30f;
    public float slipSpeed     = 10f;
    public float duration      = 0.45f;
    public float hitRadius     = 0.55f;
    public float damage        = 20f;
    public float knockback     = 8f;
    public float selfDamage    = 10f;
    public float selfKnockback = 12f;
    public float cooldown      = 1.5f;
}

[Serializable]
public class GrabThrowLevel
{
    public float staminaCost          = 30f;
    public float cooldown             = 1.0f;
    public float startup              = 0.3f;
    public float grabRange            = 1.5f;
    public float throwForce           = 14f;
    public float throwCollisionDamage = 25f;
    public float throwDuration        = 0.8f;
}

[CreateAssetMenu(fileName = "SlashSkillLevelData", menuName = "Player/Slash Skill Level Data")]
public class SlashSkillLevelData : ScriptableObject
{
    [Header("Quickdraw Levels")]
    public QuickdrawLevel[] quickdrawLevels = new QuickdrawLevel[1] { new() };

    [Header("Rising Attack Levels")]
    public RisingAttackLevel[] risingAttackLevels = new RisingAttackLevel[1] { new() };

    [Header("Smashdown Levels")]
    public SmashdownLevel[] smashdownLevels = new SmashdownLevel[1] { new() };

    [Header("Body Slam Levels")]
    public BodySlamLevel[] bodySlamLevels = new BodySlamLevel[1] { new() };

    [Header("Grab Throw Levels")]
    public GrabThrowLevel[] grabThrowLevels = new GrabThrowLevel[1] { new() };

    public int MaxQuickdrawLevel    => quickdrawLevels.Length    - 1;
    public int MaxRisingAttackLevel => risingAttackLevels.Length - 1;
    public int MaxSmashdownLevel    => smashdownLevels.Length    - 1;
    public int MaxBodySlamLevel     => bodySlamLevels.Length     - 1;
    public int MaxGrabThrowLevel    => grabThrowLevels.Length    - 1;

    public QuickdrawLevel    GetQuickdraw(int l)    => quickdrawLevels   [Mathf.Clamp(l, 0, MaxQuickdrawLevel)];
    public RisingAttackLevel GetRisingAttack(int l) => risingAttackLevels[Mathf.Clamp(l, 0, MaxRisingAttackLevel)];
    public SmashdownLevel    GetSmashdown(int l)    => smashdownLevels   [Mathf.Clamp(l, 0, MaxSmashdownLevel)];
    public BodySlamLevel     GetBodySlam(int l)     => bodySlamLevels    [Mathf.Clamp(l, 0, MaxBodySlamLevel)];
    public GrabThrowLevel    GetGrabThrow(int l)    => grabThrowLevels   [Mathf.Clamp(l, 0, MaxGrabThrowLevel)];
}
