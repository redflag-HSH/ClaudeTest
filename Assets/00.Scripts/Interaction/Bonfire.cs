using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attach to a bonfire object. Player interacts to rest, healing HP and restoring resources.
/// Saves the game and records this bonfire as the active checkpoint.
/// Give each bonfire a unique BonfireId in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Bonfire : MonoBehaviour, IInteractable
{
    // ──────────────────────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────────────────────

    [Header("Identity")]
    [Tooltip("Unique ID used by SaveManager to track this bonfire across sessions.")]
    public string BonfireId = "bonfire_01";

    [Header("Rest Settings")]
    [Tooltip("How much HP to restore on rest (0–1 = fraction of max)")]
    [SerializeField] private float healFraction = 1f;
    [Tooltip("Cooldown in seconds before the player can rest again")]
    [SerializeField] private float restCooldown = 3f;

    [Header("Visual")]
    [SerializeField] private GameObject litVFX;
    [SerializeField] private GameObject unlitVFX;
    [SerializeField] private float restAnimDuration = 1.2f;

    [Header("Events")]
    public UnityEvent onFirstLight;
    public UnityEvent onRest;
    public UnityEvent onSaved;

    // ──────────────────────────────────────────────────────────────
    //  State
    // ──────────────────────────────────────────────────────────────

    public static event Action OnAnyBonfireRest;

    public bool IsLit { get; private set; }

    private float nextRestTime;
    private bool isResting;

    // ──────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────────────────────

    private void Start()
    {
        // Restore lit state from save without triggering the first-light event
        if (SaveManager.Instance != null && SaveManager.Instance.IsBonfireLit(BonfireId))
        {
            IsLit = true;
            SetLitVisual(true);
        }
        else
        {
            SetLitVisual(false);
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  IInteractable
    // ──────────────────────────────────────────────────────────────

    public void Interact(GameObject interactor)
    {
        if (isResting || Time.time < nextRestTime) return;
        if (!interactor.TryGetComponent<PlayerControl>(out var player)) return;

        StartCoroutine(RestRoutine(player));
    }

    // ──────────────────────────────────────────────────────────────
    //  Core Logic
    // ──────────────────────────────────────────────────────────────

    private IEnumerator RestRoutine(PlayerControl player)
    {
        Debug.Log($"[Bonfire] Player started resting at '{BonfireId}'");
        isResting = true;

        if (!IsLit)
            Light();

        player.SetInputEnabled(false);

        yield return new WaitForSeconds(restAnimDuration);

        HealPlayer(player);
        RestoreStamina(player);

        SaveGame();

        player.SetInputEnabled(true);

        nextRestTime = Time.time + restCooldown;
        isResting = false;

        onRest?.Invoke();
        OnAnyBonfireRest?.Invoke();
    }

    private void HealPlayer(PlayerControl player)
    {
        float amount = player.maxHp * Mathf.Clamp01(healFraction);
        float healed = Mathf.Min(amount, player.maxHp - player.CurrentHp);
        if (healed > 0f)
            player.Heal(healed);
    }

    private void RestoreStamina(PlayerControl player)
    {
        player.RestoreStamina(player.maxStamina);
    }

    private void SaveGame()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.SaveAtBonfire(this);
        onSaved?.Invoke();
    }

    // ──────────────────────────────────────────────────────────────
    //  Lighting
    // ──────────────────────────────────────────────────────────────

    public void Light()
    {
        if (IsLit) return;
        IsLit = true;
        SetLitVisual(true);
        onFirstLight?.Invoke();
    }

    private void SetLitVisual(bool lit)
    {
        if (litVFX != null) litVFX.SetActive(lit);
        if (unlitVFX != null) unlitVFX.SetActive(!lit);
    }

    // ──────────────────────────────────────────────────────────────
    //  Editor Debug
    // ──────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Debug / Force Rest")]
    private void Debug_ForceRest()
    {
        if (PlayerControl.Instance != null)
            Interact(PlayerControl.Instance.gameObject);
    }

    [ContextMenu("Debug / Toggle Lit")]
    private void Debug_ToggleLit()
    {
        IsLit = !IsLit;
        SetLitVisual(IsLit);
    }
#endif
}
