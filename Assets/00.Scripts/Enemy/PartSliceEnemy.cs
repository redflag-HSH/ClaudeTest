using System.Collections.Generic;
using UnityEngine;

public class PartSliceEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] float hp = 300;
    public bool IsDead { get; set; }
    [SerializeField] List<PartSliceable> sliceableLimbs = new List<PartSliceable>();
    public enum Limb { Head, Body, Larm, Rarm, Llegs, Rlegs }
    [SerializeField] List<Limb> cuttedLimbs;
    void Start()
    {
        cuttedLimbs = new List<Limb>();
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
    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            IsDead = true;
        }
    }
}
