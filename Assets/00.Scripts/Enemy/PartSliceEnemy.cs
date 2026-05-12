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

    public bool HasCuttedLimbs => cuttedLimbs != null && cuttedLimbs.Count > 0;

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
        else
            cuttedLimbs.Add(limbPart.limbPart);
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
