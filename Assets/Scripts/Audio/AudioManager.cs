using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple static front door for sound. Usage:
///   Audio.Play("hurt");                 // sound effect
///   Audio.PlayMusic("music_battle");    // music (cross-fades, loops)
///   Audio.StopMusic();
///
/// Clips are loaded by NAME from  Assets/Resources/Audio/<name>.wav (or .ogg / .mp3).
/// To replace any sound or song with a real one, drop a file with the SAME NAME in that folder.
/// Missing clips are ignored (one warning), never an error.
/// </summary>
public static class Audio
{
    public static void Play(string clipName, float volume = 1f, float pitchJitter = 0f)
    {
        var m = AudioManager.Instance;
        if (m != null) m.PlaySfx(clipName, volume, pitchJitter);
    }

    public static void PlayMusic(string clipName, bool loop = true, float fadeSeconds = 0.8f)
    {
        var m = AudioManager.Instance;
        if (m != null) m.PlayMusic(clipName, loop, fadeSeconds);
    }

    public static void StopMusic(float fadeSeconds = 0.6f)
    {
        var m = AudioManager.Instance;
        if (m != null) m.StopMusic(fadeSeconds);
    }

    /// <summary>Music volume 0..1 (separate from the master volume). Saved in PlayerPrefs.</summary>
    public static float MusicVolume
    {
        get => AudioManager.MusicVol;
        set => AudioManager.SetMusicVol(value);
    }
}

public class AudioManager : MonoBehaviour
{
    const string MusicKey = "mictlan_music_volume";
    const int SfxVoices = 12;

    static AudioManager instance;
    static bool quitting;
    static float musicVol = -1f;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null && !quitting)
            {
                var go = new GameObject("AudioManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioManager>();
            }
            return instance;
        }
    }

    public static float MusicVol
    {
        get
        {
            if (musicVol < 0f) musicVol = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.7f));
            return musicVol;
        }
    }

    public static void SetMusicVol(float v)
    {
        musicVol = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(MusicKey, musicVol);
        if (instance != null) instance.ApplyMusicVolume();
    }

    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    readonly HashSet<string> warned = new HashSet<string>();
    AudioSource[] sfx;
    int sfxIndex;
    AudioSource[] music;
    int activeMusic;
    string currentTrack = "";
    float duck = 1f;
    float duckTarget = 1f;
    Coroutine fadeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; quitting = false; musicVol = -1f; }

    // Creates the manager as soon as the first scene is loaded, so music can follow scenes by itself.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Application.quitting += () => quitting = true;
        var m = Instance;
        if (m != null) m.HandleScene(SceneManager.GetActiveScene().name);
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        sfx = new AudioSource[SfxVoices];
        for (int i = 0; i < SfxVoices; i++) sfx[i] = NewSource("Sfx" + i, false);
        music = new[] { NewSource("MusicA", true), NewSource("MusicB", true) };

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameState.OnModeChanged += OnModeChanged;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameState.OnModeChanged -= OnModeChanged;
        if (instance == this) instance = null;
    }

    AudioSource NewSource(string n, bool loop)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        return s;
    }

    // ---------------- Scene / mode reactions ----------------
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { HandleScene(scene.name); }

    void HandleScene(string sceneName)
    {
        if (sceneName == SceneNames.Boot) StopMusic(0.2f);
        else if (sceneName == SceneNames.Game) PlayMusic("music_explore", true, 1.2f);
        // MainMenu chooses its own track (it may have a custom clip assigned).
    }

    void OnModeChanged(GameMode mode)
    {
        duckTarget = mode == GameMode.Paused ? 0.35f : 1f;   // music gets quieter while paused
    }

    void Update()
    {
        if (!Mathf.Approximately(duck, duckTarget))
        {
            duck = Mathf.MoveTowards(duck, duckTarget, Time.unscaledDeltaTime * 3f);
            ApplyMusicVolume();
        }
    }

    // ---------------- SFX ----------------
    AudioClip GetClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return null;
        if (cache.TryGetValue(clipName, out var c)) return c;
        c = Resources.Load<AudioClip>("Audio/" + clipName);
        if (c == null && warned.Add(clipName))
            Debug.LogWarning($"[Audio] Clip 'Audio/{clipName}' not found in Resources. Add Assets/Resources/Audio/{clipName}.wav");
        cache[clipName] = c;
        return c;
    }

    public void PlaySfx(string clipName, float volume, float pitchJitter)
    {
        var c = GetClip(clipName);
        if (c == null) return;
        var s = sfx[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfx.Length;
        s.pitch = pitchJitter > 0f ? 1f + Random.Range(-pitchJitter, pitchJitter) : 1f;
        s.volume = Mathf.Clamp01(volume);
        s.clip = c;
        s.Play();
    }

    // ---------------- Music ----------------
    public void PlayMusic(string clipName, bool loop, float fade)
    {
        if (clipName == currentTrack && music[activeMusic].isPlaying) return;
        var c = GetClip(clipName);
        if (c == null) return;
        currentTrack = clipName;

        int next = 1 - activeMusic;
        var from = music[activeMusic];
        var to = music[next];
        to.clip = c;
        to.loop = loop;
        to.volume = 0f;
        to.Play();
        activeMusic = next;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Crossfade(from, to, Mathf.Max(0.01f, fade)));
    }

    public void StopMusic(float fade)
    {
        currentTrack = "";
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Crossfade(music[activeMusic], null, Mathf.Max(0.01f, fade)));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to, float seconds)
    {
        float fromStart = from != null ? from.volume : 0f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = t / seconds;
            float target = MusicVol * duck;
            if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (to != null) to.volume = Mathf.Lerp(0f, target, k);
            yield return null;
        }
        if (from != null) { from.volume = 0f; from.Stop(); }
        if (to != null) to.volume = MusicVol * duck;
        fadeRoutine = null;
    }

    void ApplyMusicVolume()
    {
        if (fadeRoutine != null) return;   // the fade sets the volume itself
        var a = music[activeMusic];
        if (a != null && a.isPlaying) a.volume = MusicVol * duck;
    }
}
