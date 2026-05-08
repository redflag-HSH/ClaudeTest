using UnityEngine;

public class PlayerSlashSkill : MonoBehaviour
{
    [Header("Slash Skill Level System")]
    public SlashSkillLevelData slashSkillLevelData;
    [Min(0)] public int quickdrawLevel    = 0;
    [Min(0)] public int risingAttackLevel = 0;
    [Min(0)] public int smashdownLevel    = 0;
    [Min(0)] public int bodySlamLevel     = 0;
    [Min(0)] public int grabThrowLevel    = 0;

    [Header("Quickdraw")]
    public float quickdrawRange       = 6f;
    public float quickdrawDamage      = 30f;
    public float quickdrawStaminaCost = 30f;
    public float quickdrawCooldown    = 1.0f;

    [Header("Rising Attack")]
    public float risingDamage      = 20f;
    public float risingRange       = 1.0f;
    public float risingKnockback   = 12f;
    public float risingStaminaCost = 20f;
    public float risingStartup     = 0.08f;
    public float risingRecovery    = 0.3f;

    [Header("Smashdown")]
    public float smashdownDamage      = 25f;
    public float smashdownRange       = 0.9f;
    public float smashdownKnockback   = 6f;
    public float smashdownStaminaCost = 20f;
    public float smashdownStartup     = 0.1f;
    public float smashdownRecovery    = 0.35f;
    public float smashdownCooldown    = 1.0f;

    [Header("Body Slam")]
    public float bodySlamStaminaCost   = 30f;
    public float bodySlamSlipSpeed     = 10f;
    public float bodySlamDuration      = 0.45f;
    public float bodySlamHitRadius     = 0.55f;
    public float bodySlamDamage        = 20f;
    public float bodySlamKnockback     = 8f;
    public float bodySlamSelfDamage    = 10f;
    public float bodySlamSelfKnockback = 12f;
    public float bodySlamCooldown      = 1.5f;

    [Header("Grab Throw")]
    public float grabThrowStaminaCost   = 30f;
    public float grabThrowCooldown      = 1.0f;
    public float grabThrowStartup       = 0.3f;
    public float grabRange              = 1.5f;
    public float throwForce             = 14f;
    public float throwCollisionDamage   = 25f;
    public float throwDuration          = 0.8f;

    private PlayerControl _player;

    void Awake()
    {
        _player = GetComponent<PlayerControl>();
        if (slashSkillLevelData != null) ApplySlashLevels();
    }

    // ── Slash Skill Level API ─────────────────────────────────────────────────

    public void ApplySlashLevels()
    {
        ApplyQuickdrawLevel(quickdrawLevel);
        ApplyRisingAttackLevel(risingAttackLevel);
        ApplySmashdownLevel(smashdownLevel);
        ApplyBodySlamLevel(bodySlamLevel);
        ApplyGrabThrowLevel(grabThrowLevel);
    }

    public void ApplyQuickdrawLevel(int level)
    {
        if (slashSkillLevelData == null) return;
        quickdrawLevel = Mathf.Clamp(level, 0, slashSkillLevelData.MaxQuickdrawLevel);
        var d = slashSkillLevelData.GetQuickdraw(quickdrawLevel);
        quickdrawRange       = d.range;
        quickdrawDamage      = d.damage;
        quickdrawStaminaCost = d.staminaCost;
        quickdrawCooldown    = d.cooldown;
        PushToPlayer();
    }

    public void ApplyRisingAttackLevel(int level)
    {
        if (slashSkillLevelData == null) return;
        risingAttackLevel = Mathf.Clamp(level, 0, slashSkillLevelData.MaxRisingAttackLevel);
        var d = slashSkillLevelData.GetRisingAttack(risingAttackLevel);
        risingDamage      = d.damage;
        risingRange       = d.range;
        risingKnockback   = d.knockback;
        risingStaminaCost = d.staminaCost;
        risingStartup     = d.startup;
        risingRecovery    = d.recovery;
        PushToPlayer();
    }

    public void ApplySmashdownLevel(int level)
    {
        if (slashSkillLevelData == null) return;
        smashdownLevel = Mathf.Clamp(level, 0, slashSkillLevelData.MaxSmashdownLevel);
        var d = slashSkillLevelData.GetSmashdown(smashdownLevel);
        smashdownDamage      = d.damage;
        smashdownRange       = d.range;
        smashdownKnockback   = d.knockback;
        smashdownStaminaCost = d.staminaCost;
        smashdownStartup     = d.startup;
        smashdownRecovery    = d.recovery;
        smashdownCooldown    = d.cooldown;
        PushToPlayer();
    }

    public void ApplyBodySlamLevel(int level)
    {
        if (slashSkillLevelData == null) return;
        bodySlamLevel = Mathf.Clamp(level, 0, slashSkillLevelData.MaxBodySlamLevel);
        var d = slashSkillLevelData.GetBodySlam(bodySlamLevel);
        bodySlamStaminaCost   = d.staminaCost;
        bodySlamSlipSpeed     = d.slipSpeed;
        bodySlamDuration      = d.duration;
        bodySlamHitRadius     = d.hitRadius;
        bodySlamDamage        = d.damage;
        bodySlamKnockback     = d.knockback;
        bodySlamSelfDamage    = d.selfDamage;
        bodySlamSelfKnockback = d.selfKnockback;
        bodySlamCooldown      = d.cooldown;
        PushToPlayer();
    }

    public void ApplyGrabThrowLevel(int level)
    {
        if (slashSkillLevelData == null) return;
        grabThrowLevel = Mathf.Clamp(level, 0, slashSkillLevelData.MaxGrabThrowLevel);
        var d = slashSkillLevelData.GetGrabThrow(grabThrowLevel);
        grabThrowStaminaCost  = d.staminaCost;
        grabThrowCooldown     = d.cooldown;
        grabThrowStartup      = d.startup;
        grabRange             = d.grabRange;
        throwForce            = d.throwForce;
        throwCollisionDamage  = d.throwCollisionDamage;
        throwDuration         = d.throwDuration;
        PushToPlayer();
    }

    // Writes current stats to PlayerControl so its execution logic stays current.
    public void PushToPlayer()
    {
        if (_player == null) return;
        _player.quickdrawRange       = quickdrawRange;
        _player.quickdrawDamage      = quickdrawDamage;
        _player.quickdrawStaminaCost = quickdrawStaminaCost;
        _player.quickdrawCooldown    = quickdrawCooldown;

        _player.risingDamage      = risingDamage;
        _player.risingRange       = risingRange;
        _player.risingKnockback   = risingKnockback;
        _player.risingStaminaCost = risingStaminaCost;
        _player.risingStartup     = risingStartup;
        _player.risingRecovery    = risingRecovery;

        _player.smashdownDamage      = smashdownDamage;
        _player.smashdownRange       = smashdownRange;
        _player.smashdownKnockback   = smashdownKnockback;
        _player.smashdownStaminaCost = smashdownStaminaCost;
        _player.smashdownStartup     = smashdownStartup;
        _player.smashdownRecovery    = smashdownRecovery;
        _player.smashdownCooldown    = smashdownCooldown;

        _player.bodySlamStaminaCost   = bodySlamStaminaCost;
        _player.bodySlamSlipSpeed     = bodySlamSlipSpeed;
        _player.bodySlamDuration      = bodySlamDuration;
        _player.bodySlamHitRadius     = bodySlamHitRadius;
        _player.bodySlamDamage        = bodySlamDamage;
        _player.bodySlamKnockback     = bodySlamKnockback;
        _player.bodySlamSelfDamage    = bodySlamSelfDamage;
        _player.bodySlamSelfKnockback = bodySlamSelfKnockback;
        _player.bodySlamCooldown      = bodySlamCooldown;

        _player.grabThrowStaminaCost  = grabThrowStaminaCost;
        _player.grabThrowCooldown     = grabThrowCooldown;
        _player.grabThrowStartup      = grabThrowStartup;
        _player.grabRange             = grabRange;
        _player.throwForce            = throwForce;
        _player.throwCollisionDamage  = throwCollisionDamage;
        _player.throwDuration         = throwDuration;
    }
}
