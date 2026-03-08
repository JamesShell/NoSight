using UnityEngine;

[RequireComponent(typeof(AudioListener))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Library")]
    public AudioLibrary library;

    [Header("Music")]
    public AudioSource musicSourceA;
    public AudioSource musicSourceB;
    private bool isPlayingA = true;
    private Coroutine musicFadeRoutine;

    [Header("SFX 2D")]
    public AudioSource sfx2DSource;

    [Header("Volumes")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Listener Follow")]
    [Tooltip("Usually the player transform. If null and autoFindPlayerByTag is true, it will search by tag.")]
    public Transform listenerTarget;

    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";

    private AudioListener audioListener;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure we have an AudioListener on this object
        audioListener = GetComponent<AudioListener>();
        if (audioListener == null)
            audioListener = gameObject.AddComponent<AudioListener>();

        // IMPORTANT: Remove any other AudioListener (e.g. on Main Camera) to avoid warnings

        // Setup music sources
        if (musicSourceA == null)
        {
            GameObject a = new GameObject("MusicSourceA");
            a.transform.SetParent(transform);
            musicSourceA = a.AddComponent<AudioSource>();
            musicSourceA.loop = true;
            musicSourceA.spatialBlend = 0f; // 2D music
        }

        if (musicSourceB == null)
        {
            GameObject b = new GameObject("MusicSourceB");
            b.transform.SetParent(transform);
            musicSourceB = b.AddComponent<AudioSource>();
            musicSourceB.loop = true;
            musicSourceB.spatialBlend = 0f; // 2D music
        }

        // Setup SFX 2D source
        if (sfx2DSource == null)
        {
            GameObject s = new GameObject("SFX_2D_Source");
            s.transform.SetParent(transform);
            sfx2DSource = s.AddComponent<AudioSource>();
            sfx2DSource.spatialBlend = 0f; // 2D
        }

        ApplyVolumes();
        PlayMusic("Music/Ambient", 0f); // start background music instantly
    }

    void LateUpdate()
    {
        // Move AudioListener to the player (or target)
        if (listenerTarget == null && autoFindPlayerByTag)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) listenerTarget = p.transform;
        }

        if (listenerTarget != null)
        {
            transform.position = listenerTarget.position;
        }
    }

    // ---------------- VOLUME MANAGEMENT ----------------

    float MusicBaseVolume => masterVolume * musicVolume;
    float SfxBaseVolume => masterVolume * sfxVolume;

    void ApplyVolumes()
    {
        // If not currently fading, set current active music source volume
        if (musicFadeRoutine == null)
        {
            if (musicSourceA != null && musicSourceB != null)
            {
                if (isPlayingA)
                {
                    musicSourceA.volume = MusicBaseVolume;
                    musicSourceB.volume = 0f;
                }
                else
                {
                    musicSourceA.volume = 0f;
                    musicSourceB.volume = MusicBaseVolume;
                }
            }
        }

        if (sfx2DSource != null)
            sfx2DSource.volume = SfxBaseVolume;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    // ---------------- MUSIC (with crossfade) ----------------

    /// <summary>
    /// Play music from library with crossfade.
    /// fadeDuration <= 0 means instant switch.
    /// </summary>
    public void PlayMusic(string id, float fadeDuration = 1f, bool loop = true)
    {
        if (library == null)
        {
            Debug.LogWarning("[AudioManager] No AudioLibrary assigned.");
            return;
        }

        AudioClip clip = library.GetRandomClip(id);
        if (clip == null) return;

        // First music ever or no fade requested
        if (fadeDuration <= 0f ||
            (!musicSourceA.isPlaying && !musicSourceB.isPlaying))
        {
            // Use A as default
            AudioSource active = isPlayingA ? musicSourceA : musicSourceB;
            AudioSource inactive = isPlayingA ? musicSourceB : musicSourceA;

            inactive.Stop();
            active.clip = clip;
            active.loop = loop;
            active.volume = MusicBaseVolume;
            active.Play();
            return;
        }

        // Crossfade
        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeMusicCoroutine(clip, fadeDuration, loop));
    }

    private System.Collections.IEnumerator FadeMusicCoroutine(AudioClip newClip, float duration, bool loop)
    {
        AudioSource from = isPlayingA ? musicSourceA : musicSourceB;
        AudioSource to = isPlayingA ? musicSourceB : musicSourceA;

        to.clip = newClip;
        to.loop = loop;
        to.volume = 0f;
        to.Play();

        float startTime = Time.unscaledTime;
        float endTime = startTime + duration;

        float fromStartVol = from.isPlaying ? from.volume : 0f;
        float targetVol = MusicBaseVolume;

        while (Time.unscaledTime < endTime)
        {
            float t = (Time.unscaledTime - startTime) / duration;
            t = Mathf.Clamp01(t);

            float fadeOut = Mathf.Lerp(fromStartVol, 0f, t);
            float fadeIn = Mathf.Lerp(0f, targetVol, t);

            from.volume = fadeOut;
            to.volume = fadeIn;

            yield return null;
        }

        from.Stop();
        to.volume = targetVol;

        isPlayingA = !isPlayingA;
        musicFadeRoutine = null;
    }

    public void StopMusic(float fadeDuration = 0.5f)
    {
        if (fadeDuration <= 0f)
        {
            if (musicSourceA != null) musicSourceA.Stop();
            if (musicSourceB != null) musicSourceB.Stop();
            return;
        }

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeOutAllMusicCoroutine(fadeDuration));
    }

    private System.Collections.IEnumerator FadeOutAllMusicCoroutine(float duration)
    {
        float startTime = Time.unscaledTime;
        float endTime = startTime + duration;

        float startA = (musicSourceA != null && musicSourceA.isPlaying) ? musicSourceA.volume : 0f;
        float startB = (musicSourceB != null && musicSourceB.isPlaying) ? musicSourceB.volume : 0f;

        while (Time.unscaledTime < endTime)
        {
            float t = (Time.unscaledTime - startTime) / duration;
            t = Mathf.Clamp01(t);

            if (musicSourceA != null)
                musicSourceA.volume = Mathf.Lerp(startA, 0f, t);
            if (musicSourceB != null)
                musicSourceB.volume = Mathf.Lerp(startB, 0f, t);

            yield return null;
        }

        if (musicSourceA != null) musicSourceA.Stop();
        if (musicSourceB != null) musicSourceB.Stop();

        musicFadeRoutine = null;
    }

    // ---------------- SFX 2D (non-positional) ----------------

    public void PlaySfx2D(string id, float volumeMultiplier = 1f)
    {
        if (library == null || sfx2DSource == null) return;

        AudioClip clip = library.GetRandomClip(id);
        if (clip == null) return;

        float vol = SfxBaseVolume * Mathf.Clamp01(volumeMultiplier);
        sfx2DSource.PlayOneShot(clip, vol);
    }

    // ---------------- SFX 3D / positional ----------------

    public void PlaySfxAtPosition(string id, Vector3 position, float volumeMultiplier = 1f, float spatialBlend = 1f)
    {
        if (library == null) return;

        AudioClip clip = library.GetRandomClip(id);
        if (clip == null) return;

        GameObject go = new GameObject($"SFX_{id}");
        go.transform.position = position;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = Mathf.Clamp01(spatialBlend); // 1 = full 3D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = 20f;

        src.volume = SfxBaseVolume * Mathf.Clamp01(volumeMultiplier);

        src.Play();
        Destroy(go, clip.length + 0.05f);
    }
}
