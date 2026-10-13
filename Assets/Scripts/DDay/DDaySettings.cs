using UnityEngine;

namespace DDay
{
    // Preferences are separate from campaign and manual save files.
    public static class DDaySettings
    {
        const string MusicKey = "DDay.BgmPercent";
        const string SoundKey = "DDay.SfxPercent";
        const string FullscreenKey = "DDay.Fullscreen";
        const string TextSpeedKey = "DDay.TextSpeed";

        public static int BgmPercent { get; private set; } = 70;
        public static int SfxPercent { get; private set; } = 35;
        public static bool Fullscreen { get; private set; } = true;
        public static int TextSpeed { get; private set; } = 1; // 0 slow, 1 medium, 2 fast
        public static int CharactersPerSecond => TextSpeed == 0 ? 18 : TextSpeed == 2 ? 72 : 36;

        public static void Load()
        {
            BgmPercent = Mathf.Clamp(PlayerPrefs.GetInt(MusicKey, 70), 0, 100);
            SfxPercent = Mathf.Clamp(PlayerPrefs.GetInt(SoundKey, 35), 0, 100);
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) != 0;
            TextSpeed = Mathf.Clamp(PlayerPrefs.GetInt(TextSpeedKey, 1), 0, 2);
            Apply();
        }

        public static void SetBgmPercent(int value)
        {
            BgmPercent = Mathf.Clamp(value, 0, 100);
            Persist();
        }

        public static void SetFullscreen(bool value)
        {
            Fullscreen = value;
            Persist();
        }

        public static void SetSfxPercent(int value)
        {
            SfxPercent = Mathf.Clamp(value, 0, 100);
            Persist();
        }

        public static void SetTextSpeed(int value)
        {
            TextSpeed = Mathf.Clamp(value, 0, 2);
            Persist();
        }

        static void Persist()
        {
            PlayerPrefs.SetInt(MusicKey, BgmPercent);
            PlayerPrefs.SetInt(SoundKey, SfxPercent);
            PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(TextSpeedKey, TextSpeed);
            PlayerPrefs.Save();
            Apply();
        }

        static void Apply()
        {
            // Music and interface/ambience have independent bus volumes.
            AudioListener.volume = 1;
            if (Application.isEditor) return; // Game view is managed by the Editor.
            Screen.fullScreenMode = Fullscreen
                ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }
}
