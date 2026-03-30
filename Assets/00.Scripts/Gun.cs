using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform muzzle;
    public float projectileSpeed = 20f;
    public float projectileDamage = 10f;
    public float projectileLifetime = 3f;

    [Header("Fire Settings")]
    public float fireRate = 0.2f;       // seconds between shots
    public bool isAutomatic = false;

    [Header("Ammo")]
    public int magazineSize = 12;
    public int reserveAmmo = 60;
    public float reloadTime = 1.5f;

    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading { get; private set; }
    public bool IsEmpty => CurrentAmmo <= 0;

    private float nextFireTime;
    private bool attackHeld;
    private _2DActions actions;

    void Awake()
    {
        CurrentAmmo = magazineSize;
        ReserveAmmo = reserveAmmo;

        actions = new _2DActions();
    }

    void OnEnable()
    {
        actions.Player2D.Attack.performed += OnAttackPerformed;
        actions.Player2D.Attack.canceled  += OnAttackCanceled;
        actions.Player2D.Reload.performed += OnReloadPerformed;
        actions.Player2D.Enable();
    }

    void OnDisable()
    {
        actions.Player2D.Attack.performed -= OnAttackPerformed;
        actions.Player2D.Attack.canceled  -= OnAttackCanceled;
        actions.Player2D.Reload.performed -= OnReloadPerformed;
        actions.Player2D.Disable();
    }

    void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        attackHeld = true;
        if (!isAutomatic)
            TryShoot();
    }

    void OnAttackCanceled(InputAction.CallbackContext ctx)
    {
        attackHeld = false;
    }

    void OnReloadPerformed(InputAction.CallbackContext ctx)
    {
        TryReload();
    }

    void Update()
    {
        if (isAutomatic && attackHeld)
            TryShoot();
    }

    // ── Shooting ────────────────────────────────────────────────────────────

    public void TryShoot()
    {
        if (IsReloading || IsEmpty || Time.time < nextFireTime) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Shoot();
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireRate;
        CurrentAmmo--;

        SpawnProjectile();

        if (IsEmpty)
            EmptyShoot();
    }

    void SpawnProjectile()
    {
        Transform spawnPoint = muzzle != null ? muzzle : transform;

        GameObject proj = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);

        if (proj.TryGetComponent<Projectile>(out var p))
        {
            p.Init(projectileSpeed, projectileDamage, projectileLifetime);
        }
        else if (proj.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = spawnPoint.right * projectileSpeed;
            Destroy(proj, projectileLifetime);
        }
    }

    void EmptyShoot()
    {
        //공탄 사격 음 재생
    }

    // ── Reload ───────────────────────────────────────────────────────────────

    public void TryReload()
    {
        if (IsReloading || CurrentAmmo == magazineSize || ReserveAmmo <= 0) return;

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        IsReloading = true;

        yield return new WaitForSeconds(reloadTime);

        int needed = magazineSize - CurrentAmmo;
        int taken  = Mathf.Min(needed, ReserveAmmo);
        CurrentAmmo  += taken;
        ReserveAmmo  -= taken;

        IsReloading = false;
    }

    // ── Ammo Management ──────────────────────────────────────────────────────

    public void AddAmmo(int amount)
    {
        ReserveAmmo += amount;
    }

    public void Refill()
    {
        CurrentAmmo = magazineSize;
        ReserveAmmo = reserveAmmo;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (muzzle == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(muzzle.position, muzzle.right * 2f);
    }
}