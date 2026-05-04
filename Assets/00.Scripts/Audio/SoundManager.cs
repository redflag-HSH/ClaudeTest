using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// ── Sound IDs ─────────────────────────────────────────────────────────────────
// Add new entries here; wire them up in the Inspector via SoundManager.sounds[].
public enum SoundID
{
    None = -1,

    // Player actions
    PlayerFootstep,
    PlayerJump,
    PlayerLand,
    PlayerDodge,
    PlayerLightAttack,
    PlayerHeavyAttackCharge,
    PlayerHeavyAttackRelease,
    PlayerGuardStart,
    PlayerParrySuccess,
    PlayerHurt,
    PlayerDeath,
    PlayerLimbSliced,
    PlayerBloodGaugeFull,

    // Enemy
    EnemyHurt,
    EnemyDeath,
    EnemySliced,
    EnemyAttack,
    EnemySpawn,

    // World / FX
    BloodSplat,
    LimbLand,
    BonfireLoop,

    // UI
    UIClick,
    UIConfirm,
    UICancel,
    UILevelUp,

    // BGM
    BGMMenu,
    BGMGameplay,
    BGMBoss,
    BGMBonfire,
}

// ── Sound Entry ───────────────────────────────────────────────────────────────
[System.Serializable]
public class SoundEntry
{
    public SoundID id;

    [Tooltip("Assign one or more clips — a random one is chosen on each play.")]
    public AudioClip[] clips;

    [Range(0f, 1f)]   public float volume       = 1f;
    [Range(0.5f, 2f)] public float pitch        = 1f;
    [Range(0f, 0.3f)] public float pitchVariance = 0.05f;

    public bool loop = false;

    [Header("3D Settings (ignored for 2D calls)")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 2f;
    public float maxDistance = 25f;

    public AudioMixerGroup mixerGroup;
}

// ── SoundManager ──────────────────────────────────────────────────────────────
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer (optional)")]
    public AudioMixer audioMixer;
    [Tooltip("Exposed parameter name for master volume in the AudioMixer.")]
    public string mixerParamMaster = "MasterVolume";
    [Tooltip("Exposed parameter name for BGM volume.")]
    public string mixerParamBGM    = "BGMVolume";
    [Tooltip("Exposed parameter name for SFX volume.")]
    public string mixerParamSFX    = "SFXVolume";

    [Header("Sound Library")]
    public SoundEntry[] sounds;

    [Header("BGM")]
    public float bgmFadeDuration = 1.2f;

    [Header("SFX Pool")]
    [SerializeField] int sfxPoolSize = 16;

    [Header("Head Muffle")]
    [Tooltip("AudioLowPassFilter on the Camera/AudioListener GameObject.")]
    [SerializeField] AudioLowPassFilter headMuffleFilter;
    [Tooltip("Low-pass cutoff frequency when the head is slayed (muffled).")]
    [Range(100f, 5000f)] public float muffledCutoffFreq = 800f;
    [Tooltip("Seconds to interpolate between normal and muffled cutoff.")]
    public float headMuffleFadeDuration = 0.5f;

    // ── Private ───────────────────────────────────────────────────────────────

    readonly Dictionary<SoundID, SoundEntry> _map     = new();
    readonly Queue<AudioSource>              _pool    = new();
    readonly List<AudioSource>               _active  = new();

    AudioSource _bgmA;
    AudioSource _bgmB;
    bool _usingA = true;

    Coroutine _bgmRoutine;
    Coroutine _muffleRoutine;

    const float NormalCutoffFreq = 22000f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildMap();
        BuildPool();
        BuildBGMSources();
    }

    void Update()
    {
        // Return finished SFX sources back to the pool
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var src = _active[i];
            if (src != null && !src.isPlaying)
            {
                _active.RemoveAt(i);
                ReturnToPool(src);
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// Play a 2D (non-spatial) sound effect.
    public static void Play(SoundID id)
        => Instance?.PlayInternal(id, Vector3.zero, null, false);

    /// Play a 3D positional sound at a world-space point.
    public static void Play(SoundID id, Vector3 worldPosition)
        => Instance?.PlayInternal(id, worldPosition, null, true);

    /// Play a 3D sound attached to a Transform so it follows it.
    public static void Play(SoundID id, Transform follow)
        => Instance?.PlayInternal(id, Vector3.zero, follow, true);

    /// Start BGM, crossfading from whatever is currently playing.
    public static void PlayBGM(SoundID id)
        => Instance?.PlayBGMInternal(id);

    /// Fade out and stop the current BGM.
    public static void StopBGM()
        => Instance?.StopBGMInternal();

    /// Stop all active SFX immediately.
    public static void StopAllSFX()
    {
        if (Instance == null) return;
        foreach (var src in Instance._active) src?.Stop();
    }

    // Volume setters — map a 0..1 linear value to the AudioMixer (dB) parameter.
    public static void SetMasterVolume(float linear) => Instance?.SetMixerVolume(Instance.mixerParamMaster, linear);
    public static void SetBGMVolume(float linear)    => Instance?.SetMixerVolume(Instance.mixerParamBGM,    linear);
    public static void SetSFXVolume(float linear)    => Instance?.SetMixerVolume(Instance.mixerParamSFX,    linear);

    /// Check whether a BGM track is currently playing.
    public static bool IsBGMPlaying()
    {
        if (Instance == null) return false;
        return Instance._bgmA.isPlaying || Instance._bgmB.isPlaying;
    }

    /// Current BGM clip (whichever source is active).
    public static AudioClip CurrentBGMClip()
    {
        if (Instance == null) return null;
        var active = Instance._usingA ? Instance._bgmA : Instance._bgmB;
        return active.isPlaying ? active.clip : null;
    }

    /// Enable or disable the head-muffle low-pass filter with a smooth frequency fade.
    public static void SetHeadMuffle(bool muffled)
        => Instance?.SetHeadMuffleInternal(muffled);

    // ── Internal implementation ───────────────────────────────────────────────

    void PlayInternal(SoundID id, Vector3 pos, Transform follow, bool spatial)
    {
        if (!_map.TryGetValue(id, out var entry)) return;
        var clip = PickClip(entry);
        if (clip == null) return;

        var src = RentSource();

        if (follow != null)
        {
            src.transform.SetParent(follow);
            src.transform.localPosition = Vector3.zero;
        }
        else if (spatial)
        {
            src.transform.SetParent(transform);
            src.transform.position = pos;
        }
        else
        {
            src.transform.SetParent(transform);
        }

        src.clip         = clip;
        src.volume       = entry.volume;
        src.pitch        = entry.pitch + Random.Range(-entry.pitchVariance, entry.pitchVariance);
        src.loop         = entry.loop;
        src.spatialBlend = spatial ? entry.spatialBlend : 0f;
        src.minDistance  = entry.minDistance;
        src.maxDistance  = entry.maxDistance;
        src.rolloffMode  = AudioRolloffMode.Logarithmic;
        src.outputAudioMixerGroup = entry.mixerGroup;

        src.Play();
    }

    void PlayBGMInternal(SoundID id)
    {
        if (!_map.TryGetValue(id, out var entry)) return;
        var clip = PickClip(entry);
        if (clip == null) return;

        var outgoing = _usingA ? _bgmA : _bgmB;
        var incoming = _usingA ? _bgmB : _bgmA;
        _usingA = !_usingA;

        incoming.outputAudioMixerGroup = entry.mixerGroup;
        incoming.clip   = clip;
        incoming.volume = 0f;
        incoming.loop   = true;
        incoming.Play();

        if (_bgmRoutine != null) StopCoroutine(_bgmRoutine);
        _bgmRoutine = StartCoroutine(CrossfadeBGM(outgoing, incoming, entry.volume, bgmFadeDuration));
    }

    void StopBGMInternal()
    {
        var current = _usingA ? _bgmA : _bgmB;
        if (_bgmRoutine != null) StopCoroutine(_bgmRoutine);
        _bgmRoutine = StartCoroutine(FadeOut(current, bgmFadeDuration));
    }

    void SetMixerVolume(string param, float linear)
    {
        if (audioMixer == null) return;
        float dB = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        audioMixer.SetFloat(param, dB);
    }

    void SetHeadMuffleInternal(bool muffled)
    {
        if (headMuffleFilter == null) return;
        headMuffleFilter.enabled = true;

        float target = muffled ? muffledCutoffFreq : NormalCutoffFreq;
        float current = headMuffleFilter.cutoffFrequency;

        if (_muffleRoutine != null) StopCoroutine(_muffleRoutine);
        _muffleRoutine = StartCoroutine(TransitionMuffle(current, target, headMuffleFadeDuration));
    }

    IEnumerator TransitionMuffle(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            headMuffleFilter.cutoffFrequency = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        headMuffleFilter.cutoffFrequency = to;
        // Disable the component when fully unfiltered to save a tiny amount of DSP cost
        headMuffleFilter.enabled = to < NormalCutoffFreq;
    }

    // ── Pool helpers ──────────────────────────────────────────────────────────

    AudioSource RentSource()
    {
        if (_pool.Count > 0)
        {
            var src = _pool.Dequeue();
            src.gameObject.SetActive(true);
            _active.Add(src);
            return src;
        }

        // Pool exhausted — spawn an overflow source (auto-returned by Update)
        var go = new GameObject("SFX_Overflow");
        go.transform.SetParent(transform);
        var overflow = go.AddComponent<AudioSource>();
        overflow.playOnAwake = false;
        _active.Add(overflow);
        return overflow;
    }

    void ReturnToPool(AudioSource src)
    {
        src.Stop();
        src.clip = null;
        src.transform.SetParent(transform);
        src.gameObject.SetActive(false);
        _pool.Enqueue(src);
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    void BuildMap()
    {
        foreach (var entry in sounds)
            _map[entry.id] = entry;
    }

    void BuildPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"SFX_{i:00}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            go.SetActive(false);
            _pool.Enqueue(src);
        }
    }

    void BuildBGMSources()
    {
        _bgmA = MakeBGMSource("BGM_A");
        _bgmB = MakeBGMSource("BGM_B");
    }

    AudioSource MakeBGMSource(string goName)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop        = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        return src;
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator CrossfadeBGM(AudioSource outgoing, AudioSource incoming, float targetVolume, float duration)
    {
        float startOut = outgoing.volume;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            outgoing.volume = Mathf.Lerp(startOut, 0f, t);
            incoming.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        outgoing.Stop();
        outgoing.volume = 0f;
        incoming.volume = targetVolume;
    }

    IEnumerator FadeOut(AudioSource source, float duration)
    {
        float start   = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    static AudioClip PickClip(SoundEntry entry)
    {
        if (entry.clips == null || entry.clips.Length == 0) return null;
        return entry.clips[Random.Range(0, entry.clips.Length)];
    }
}
