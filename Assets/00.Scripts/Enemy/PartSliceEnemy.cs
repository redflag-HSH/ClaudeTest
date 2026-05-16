using System.Collections.Generic;
using UnityEngine;

public class PartSliceEnemy : MonoBehaviour
{
    [SerializeField] float hp = 300;
    public virtual bool IsDead { get; set; }
    [SerializeField] List<PartSliceable> sliceableLimbs = new List<PartSliceable>();
    List<PartSliceable> allLimbs;
    public enum Limb { Head, Body, Larm, Rarm, Llegs, Rlegs }
    [SerializeField] protected List<Limb> cuttedLimbs;

    [SerializeField] float legSpeedPenalty = 0.5f;

    public bool HasCuttedLimbs => cuttedLimbs != null && cuttedLimbs.Count > 0;

    public virtual void ReceiveLimbDamage(float amount, float stunDuration) { }
    public PartSliceable LastHitLimb { get; set; }

    public float LegSpeedMultiplier
    {
        get
        {
            if (cuttedLimbs == null) return 1f;
            int cut = 0;
            foreach (var l in cuttedLimbs)
                if (l == Limb.Llegs || l == Limb.Rlegs) cut++;
            return Mathf.Max(0.1f, 1f - cut * legSpeedPenalty);
        }
    }

    void Start()
    {
        cuttedLimbs = new List<Limb>();
        allLimbs = new List<PartSliceable>(sliceableLimbs);
        foreach (PartSliceable limb in sliceableLimbs)
        {
            limb.onSliced += () => { Cutted(limb); };
        }
    }

    public void Cutted(PartSliceable limbPart)
    {
        if (IsDead) return;

        sliceableLimbs.Remove(limbPart);
        print("Cutted " + limbPart.limbPart);
        if (limbPart.limbPart == Limb.Head || limbPart.limbPart == Limb.Body)
        {
            foreach (PartSliceable limb in sliceableLimbs)
            {
                if (limb != limbPart)
                    limb.SpawnWhole();
            }
            this.gameObject.SetActive(false);
        }
        else if (limbPart.limbPart == Limb.Llegs || limbPart.limbPart == Limb.Rlegs)
        {
            cuttedLimbs.Add(limbPart.limbPart);
        }
    }

    public void SpawnDeathParts()
    {
        var limbs = new List<PartSliceable>(sliceableLimbs);
        foreach (var limb in limbs)
        {
            if (limb == null) continue;
            if (limb == LastHitLimb)
                limb.Slice(limb.pendingSliceNormal, limb.pendingSliceContact, limb.pendingSliceForcePower, limb.pendingSlicePlayerPos);
            else
                limb.SpawnWhole();
        }
    }

    public bool HealRandomLimb()
    {
        if (!HasCuttedLimbs) return false;

        int idx = Random.Range(0, cuttedLimbs.Count);
        Limb limbType = cuttedLimbs[idx];
        PartSliceable toRestore = allLimbs.Find(l => l != null && l.limbPart == limbType);
        if (toRestore == null) return false;

        cuttedLimbs.RemoveAt(idx);
        sliceableLimbs.Add(toRestore);
        toRestore.Restore();
        return true;
    }
}
