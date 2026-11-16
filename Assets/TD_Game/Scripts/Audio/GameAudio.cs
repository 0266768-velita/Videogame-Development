using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Music and sound effects for the whole game (adapted from Mictlán v5's AudioManager).
    /// Put one in EVERY scene with the AudioLibrary and the song of that scene. The first one survives scene changes;
    /// the ones in later scenes only tell it which song to play and then remove themselves.
    /// Usage from code: GameAudio.PlaySfx("hurt");  GameAudio.PlayMusic(MusicTrack.Battle);
    /// Volumes (master, music, effects) are saved in PlayerPrefs and changed in the Options menu.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        const string MasterKey = "td_master_volume", MusicKey = "td_music_volume", SfxKey = "td_sfx_volume";

        [SerializeField] AudioLibrary library;
        [Tooltip("Song that starts when this scene loads.")]
        [SerializeField] MusicTrack sceneMusic = MusicTrack.Explore;
        [SerializeField, Min(0f)] float crossFadeSeconds = 0.8f;
        [SerializeField, Range(1, 24)] int sfxVoices = 10;

        public static GameAudio Instance { get; private set; }

        AudioSource musicA, musicB;    // two sources so songs can cross-fade
        AudioSource[] voices;
        int nextVoice;
        MusicTrack current = MusicTrack.None;
        Coroutine fade;

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.7f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); if (Instance != null) Instance.ApplyMusicVolume(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        }

        public static MusicTrack CurrentTrack => Instance != null ? Instance.current : MusicTrack.None;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // A new scene: the surviving GameAudio plays this scene's song.
                Instance.PlayTrack(sceneMusic);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            AudioListener.volume = MasterVolume;

            musicA = NewSource(true);
            musicB = NewSource(true);
            voices = new AudioSource[sfxVoices];
            for (int i = 0; i < voices.Length; i++) voices[i] = NewSource(false);
        }

        void Start()
        {
            if (Instance == this) PlayTrack(sceneMusic);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource NewSource(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        // ---------------- Static shortcuts (safe without a GameAudio in the scene) ----------------

        public static void PlaySfx(string clipName, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (Instance != null) Instance.Sfx(clipName, volume, pitchJitter);
        }

        public static void PlayMusic(MusicTrack track)
        {
            if (Instance != null) Instance.PlayTrack(track);
        }

        // ---------------- Implementation ----------------

        void Sfx(string clipName, float volume, float pitchJitter)
        {
            var entry = library != null ? library.GetSfx(clipName) : null;
            if (entry == null || entry.clip == null) return;
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            voice.PlayOneShot(entry.clip, entry.volume * volume * SfxVolume);
        }

        public void PlayTrack(MusicTrack track)
        {
            if (track == current) return;
            current = track;
            var clip = library != null ? library.GetMusic(track) : null;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(CrossFade(clip));
        }

        IEnumerator CrossFade(AudioClip clip)
        {
            // The new song goes in the silent source; the playing one fades out.
            var from = musicA.isPlaying ? musicA : musicB;
            var to = from == musicA ? musicB : musicA;
            float fromStart = from.volume;
            if (clip != null)
            {
                to.clip = clip;
                to.volume = 0f;
                to.Play();
            }
            for (float t = 0f; t < crossFadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / crossFadeSeconds;
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (clip != null) to.volume = Mathf.Lerp(0f, MusicVolume, k);
                yield return null;
            }
            from.Stop();
            if (clip != null) to.volume = MusicVolume;
            fade = null;
        }

        void ApplyMusicVolume()
        {
            if (fade != null) return;
            if (musicA.isPlaying) musicA.volume = MusicVolume;
            if (musicB.isPlaying) musicB.volume = MusicVolume;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
