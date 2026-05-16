using System;
using UnityEngine;

[Serializable]
public class BloodSpearLevel
{
    public int   spearCount        = 5;
    public float hoverRadius       = 1.2f;
    public float spearDamage       = 30f;
    public float bloodCostPerSpear = 10f;
    public float spearInterval     = 0.25f;
    public int   maxSpearCount     = 10;
}

[Serializable]
public class DrainLevel
{
    public float drainRange       = 3f;
    public float drainDamage      = 20f;
    public float drainHealRatio   = 0.5f;
    public float drainBloodCost   = 25f;
    public float drainCooldown    = 2.0f;
    public int   drainTargetCount = 3;
}

[Serializable]
public class HedgehogLevel
{
    public float hedgehogRange         = 4f;
    public float hedgehogDamage        = 25f;
    public float hedgehogBloodCost     = 20f;
    public float hedgehogCooldown      = 3f;
    public float hedgehogSpikeSpeed    = 12f;
    public float hedgehogSpikeLifetime = 2f;
}

[Serializable] public class EclipseLevel          { /* TODO */ }
[Serializable] public class DaughterOfDragonLevel { /* TODO */ }
[Serializable] public class SteelBloodLevel       { /* TODO */ }
[Serializable] public class HeatBloodLevel        { /* TODO */ }
[Serializable] public class ColdBloodLevel        { /* TODO */ }

[CreateAssetMenu(fileName = "MagicSkillLevelData", menuName = "Player/Magic Skill Level Data")]
public class MagicSkillLevelData : ScriptableObject
{
    [Header("Blood Spear Levels")]
    public BloodSpearLevel[] bloodSpearLevels = new BloodSpearLevel[1] { new() };

    [Header("Drain Levels")]
    public DrainLevel[] drainLevels = new DrainLevel[1] { new() };

    [Header("Hedgehog Levels")]
    public HedgehogLevel[] hedgehogLevels = new HedgehogLevel[1] { new() };

    [Header("Eclipse Levels")]
    public EclipseLevel[] eclipseLevels = new EclipseLevel[1] { new() };

    [Header("Daughter of Dragon Levels")]
    public DaughterOfDragonLevel[] daughterOfDragonLevels = new DaughterOfDragonLevel[1] { new() };

    [Header("Steel Blood Levels")]
    public SteelBloodLevel[] steelBloodLevels = new SteelBloodLevel[1] { new() };

    [Header("Heat Blood Levels")]
    public HeatBloodLevel[] heatBloodLevels = new HeatBloodLevel[1] { new() };

    [Header("Cold Blood Levels")]
    public ColdBloodLevel[] coldBloodLevels = new ColdBloodLevel[1] { new() };

    public int MaxBloodSpearLevel       => bloodSpearLevels.Length - 1;
    public int MaxDrainLevel            => drainLevels.Length - 1;
    public int MaxHedgehogLevel         => hedgehogLevels.Length - 1;
    public int MaxEclipseLevel          => eclipseLevels.Length - 1;
    public int MaxDaughterOfDragonLevel => daughterOfDragonLevels.Length - 1;
    public int MaxSteelBloodLevel       => steelBloodLevels.Length - 1;
    public int MaxHeatBloodLevel        => heatBloodLevels.Length - 1;
    public int MaxColdBloodLevel        => coldBloodLevels.Length - 1;

    public BloodSpearLevel       GetBloodSpear(int l)       => bloodSpearLevels      [Mathf.Clamp(l, 0, MaxBloodSpearLevel)];
    public DrainLevel            GetDrain(int l)            => drainLevels           [Mathf.Clamp(l, 0, MaxDrainLevel)];
    public HedgehogLevel         GetHedgehog(int l)         => hedgehogLevels        [Mathf.Clamp(l, 0, MaxHedgehogLevel)];
    public EclipseLevel          GetEclipse(int l)          => eclipseLevels         [Mathf.Clamp(l, 0, MaxEclipseLevel)];
    public DaughterOfDragonLevel GetDaughterOfDragon(int l) => daughterOfDragonLevels[Mathf.Clamp(l, 0, MaxDaughterOfDragonLevel)];
    public SteelBloodLevel       GetSteelBlood(int l)       => steelBloodLevels      [Mathf.Clamp(l, 0, MaxSteelBloodLevel)];
    public HeatBloodLevel        GetHeatBlood(int l)        => heatBloodLevels       [Mathf.Clamp(l, 0, MaxHeatBloodLevel)];
    public ColdBloodLevel        GetColdBlood(int l)        => coldBloodLevels       [Mathf.Clamp(l, 0, MaxColdBloodLevel)];
}
