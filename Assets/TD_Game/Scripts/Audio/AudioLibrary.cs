using UnityEngine;

namespace TD_Game
{
    /// <summary>The songs of the game.</summary>
    public enum MusicTrack { None, Menu, Explore, Battle, Boss, GameOver }

    /// <summary>
    /// Every song and sound effect of the game in one asset (adapted from Mictlán v5's Resources/Audio).
    /// Sound effects are found by name: GameAudio.PlaySfx("hurt").
    /// To replace a sound, drag another clip into its slot here. Create with: Create > TD_Game > Audio Library.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public class NamedClip
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Other versions of the same sound: one is picked at random each time, so repeated sounds do not tire the ear.")]
            public AudioClip[] variants;

            public AudioClip Pick()
            {
                int count = variants == null ? 0 : variants.Length;
                int i = Random.Range(-1, count);
                var chosen = i < 0 ? clip : variants[i];
                return chosen != null ? chosen : clip;
            }
        }

        [Header("Music")]
        public AudioClip menu;
        public AudioClip explore;
        public AudioClip battle;
        public AudioClip boss;
        public AudioClip gameOver;

        [Header("Sound effects (found by name)")]
        public NamedClip[] sfx;

        public AudioClip GetMusic(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Menu: return menu;
                case MusicTrack.Explore: return explore;
                case MusicTrack.Battle: return battle;
                case MusicTrack.Boss: return boss;
                case MusicTrack.GameOver: return gameOver;
                default: return null;
            }
        }

        public NamedClip GetSfx(string clipName)
        {
            if (sfx == null) return null;
            foreach (var s in sfx)
                if (s != null && s.name == clipName) return s;
            return null;
        }
    }
}
