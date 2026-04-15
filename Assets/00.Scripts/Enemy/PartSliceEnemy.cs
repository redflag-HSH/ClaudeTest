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
            limb.onSliced += () => { Cutted(limb.limbPart); };
        }
    }
    public void Cutted(Limb limbPart)
    {
        print("Cutted " + limbPart);
        if (limbPart == Limb.Head || limbPart == Limb.Body)
        {
            this.gameObject.SetActive(false);
        }
        else
            cuttedLimbs.Add(limbPart);
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
