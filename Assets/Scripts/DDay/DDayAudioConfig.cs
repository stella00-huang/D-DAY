using System;
using UnityEngine;

namespace DDay
{
    public enum BgmCue { MainMenu, Days12, Days34, Days56, Day7, HE, BE }
    public enum SfxCue { Click, Pickup, Drop, Error, Confirmation, Back, Clue, Toggle, Dialogue }

    [Serializable]
    public class DDayMusicEntry
    {
        public BgmCue cue;
        public AudioClip clip;
        [Range(0, 1)] public float gain = .3f;
        public bool loop = true;
    }
    [Serializable]
    public class DDaySoundEntry
    {
        public SfxCue cue;
        public AudioClip clip;
        [Range(0, 1)] public float gain = .12f;
        public float repeatInterval = .12f;
    }
    [CreateAssetMenu(menuName = "D-DAY/Audio configuration")]
    public sealed class DDayAudioConfig : ScriptableObject
    {
        public float crossfadeSeconds = 1.5f;
        public DDayMusicEntry[] music;
        public DDaySoundEntry[] sounds;
        public DDayMusicEntry Music(BgmCue cue) => Array.Find(music, entry => entry.cue == cue);
        public DDaySoundEntry Sound(SfxCue cue) => Array.Find(sounds, entry => entry.cue == cue);
    }
}
