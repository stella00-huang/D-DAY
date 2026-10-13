using UnityEngine;

namespace DDay
{
    // The campaign keeps one DDayGame through title, both loops and endings.
    // Its audio service has the same lifetime; it does not persist into the old game.
    [RequireComponent(typeof(DDayGame))]
    [DefaultExecutionOrder(100)]
    public sealed class DDayAudioManager : MonoBehaviour
    {
        public static DDayAudioManager Instance { get; private set; }
        [SerializeField] DDayAudioConfig configuration;
        readonly AudioSource[] bgm = new AudioSource[2];
        readonly float[] gains = new float[2];
        readonly double[] soundTimes = new double[9];
        DDayGame game;
        HEEndingPlayer ending;
        AudioSource sfx;
        int active;
        float fade, outgoingVolume;
        bool fading, hasCue;
        int lastSoundFrame = -1;
        SfxCue? pendingSound;
        public BgmCue CurrentCue { get; private set; }
        public int MusicStarts { get; private set; }
        public int SoundPlays { get; private set; }
        public SfxCue LastSound { get; private set; }
        public DDayAudioConfig Configuration => configuration;
        public AudioSource ActiveMusic => bgm[active];
        public AudioSource OutgoingMusic => bgm[1 - active];
        public AudioSource SoundSource => sfx;
        public bool IsCrossfading => fading;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            game = GetComponent<DDayGame>();
            configuration = configuration != null ? configuration
                : Resources.Load<DDayAudioConfig>("DDay/Audio/DDayAudioConfig");
            for (int i = 0; i < 2; i++) bgm[i] = Source("BGM " + i);
            sfx = Source("SFX"); sfx.priority = 64;
            for (int i = 0; i < soundTimes.Length; i++) soundTimes[i] = double.NegativeInfinity;
            if (configuration == null) Debug.LogError("D-DAY audio configuration is missing.");
        }
        AudioSource Source(string name)
        {
            var obj = new GameObject(name); obj.transform.SetParent(transform, false);
            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0;
            source.ignoreListenerPause = true; source.volume = 0;
            return source;
        }
        void Update()
        {
            if (configuration == null) return;
            if (ending == null) ending = GetComponent<HEEndingPlayer>();
            BgmCue cue = Route(game.Loop, game.Day, game.CurrentPhase);
            if (ending != null && ending.OwnsUI)
                cue = ending.Configuration == ending.BEConfiguration ? BgmCue.BE : BgmCue.HE;
            if (!hasCue || cue != CurrentCue) Switch(cue);
            float master = DDaySettings.BgmPercent / 100f;
            if (fading)
            {
                fade += Time.unscaledDeltaTime / Mathf.Max(.05f, configuration.crossfadeSeconds);
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(fade));
                bgm[active].volume = gains[active] * master * t;
                bgm[1 - active].volume = outgoingVolume * master * (1 - t);
                if (fade >= 1) { bgm[1 - active].Stop(); fading = false; }
            }
            else
            {
                bgm[active].volume = gains[active] * master;
                // Blend the unchanged track's tail into its start instead of a hard loop seam.
                var entry = configuration.Music(CurrentCue);
                if (entry != null && entry.loop && bgm[active].isPlaying
                    && bgm[active].clip.length > configuration.crossfadeSeconds * 3
                    && bgm[active].time >= bgm[active].clip.length - configuration.crossfadeSeconds)
                    StartTrack(entry);
            }
            sfx.volume = DDaySettings.SfxPercent / 100f;
        }
        public static BgmCue Route(int loop, int day, Phase phase)
        {
            if (phase == Phase.Title) return BgmCue.MainMenu;
            if (phase == Phase.Credits) return BgmCue.MainMenu;
            if (loop == 2 && day == 7 && phase == Phase.BadEnding) return BgmCue.BE;
            if (loop == 2 && day == 7 && (phase == Phase.Epilogue || phase == Phase.TrueEnding)) return BgmCue.HE;
            return day <= 2 ? BgmCue.Days12 : day <= 4 ? BgmCue.Days34
                : day <= 6 ? BgmCue.Days56 : BgmCue.Day7;
        }
        void Switch(BgmCue cue)
        {
            var entry = configuration.Music(cue);
            if (entry == null || entry.clip == null)
            { Debug.LogError("D-DAY missing music: " + cue); return; }
            CurrentCue = cue; hasCue = true; StartTrack(entry);
        }
        void StartTrack(DDayMusicEntry entry)
        {
            // Collapse interrupted transitions so at most two tracks ever play.
            bgm[1 - active].Stop();
            outgoingVolume = DDaySettings.BgmPercent > 0
                ? bgm[active].volume / (DDaySettings.BgmPercent / 100f) : gains[active];
            active = 1 - active;
            bgm[active].clip = entry.clip;
            bgm[active].loop = false; // Normal cues blend their loop; endings play exactly once.
            gains[active] = entry.gain; bgm[active].volume = 0;
            bgm[active].Play(); MusicStarts++; fade = 0; fading = true;
        }
        public static void Play(SfxCue cue) { if (Instance != null) Instance.PlaySound(cue); }
        public bool PlaySound(SfxCue cue)
        {
            if (configuration == null) return false;
            var entry = configuration.Sound(cue);
            if (entry == null || entry.clip == null) return false;
            int index = (int)cue;
            if (Time.unscaledTimeAsDouble - soundTimes[index] < entry.repeatInterval)
                return false;
            // Resolve a UI click and its result to one meaningful sound, even in IMGUI's later pass.
            if (lastSoundFrame == Time.frameCount) return false;
            if (!pendingSound.HasValue || Priority(cue) > Priority(pendingSound.Value)) pendingSound = cue;
            return true;
        }
        static int Priority(SfxCue cue) => cue == SfxCue.Clue ? 9 : cue == SfxCue.Error ? 8
            : cue == SfxCue.Confirmation ? 7 : cue == SfxCue.Drop ? 6 : cue == SfxCue.Pickup ? 5
            : cue == SfxCue.Back || cue == SfxCue.Toggle ? 4 : cue == SfxCue.Dialogue ? 2 : 1;
        void LateUpdate()
        {
            if (!pendingSound.HasValue) return;
            SfxCue cue = pendingSound.Value; pendingSound = null;
            var entry = configuration.Sound(cue);
            lastSoundFrame = Time.frameCount; soundTimes[(int)cue] = Time.unscaledTimeAsDouble;
            sfx.volume = DDaySettings.SfxPercent / 100f;
            sfx.pitch = cue == SfxCue.Confirmation ? 1 : Random.Range(.985f, 1.015f);
            sfx.PlayOneShot(entry.clip, entry.gain);
            LastSound = cue; SoundPlays++;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
