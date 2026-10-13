using System;
using UnityEngine;

namespace DDay
{
    public enum HEAtmosphere { DeskLamp, PoliceLights, Sunset, CorridorPhone, RainNight, Morning }

    [Serializable]
    public sealed class HEEndingShot
    {
        public string id;
        public Texture2D image;
        [Min(.1f)] public float duration = 10;
        [Min(1f)] public float startScale = 1, endScale = 1;
        [Tooltip("Movement in viewport fractions. Keep small to preserve the illustration's characters.")]
        public Vector2 startOffset, endOffset;
        [TextArea(2, 3)] public string subtitle;
        [TextArea(6, 18)] public string narration;
        public float subtitleStart = 7, subtitleEnd = 9;
        [Min(0)] public float fadeIn = 1, fadeOut = 1;
        public HEAtmosphere atmosphere;
        public Vector2 lightPosition = new Vector2(.5f, .5f);
        [Range(0, .15f)] public float lightIntensity = .035f;
        [Range(0, .15f)] public float dustIntensity = .05f;
        public AudioClip music, ambience;
        [Range(0, 1)] public float musicVolume = .25f, ambienceVolume = .08f;
    }

    [CreateAssetMenu(menuName = "D-DAY/HE Ending Configuration", fileName = "HEEndingConfig")]
    public sealed class HEEndingConfig : ScriptableObject
    {
        public HEEndingShot[] shots;
        public string endingTitle = "D-DAY";
        public string endingSubtitle = "";
        public string presentationId;
        public string openingTitle;
        [Min(.1f)] public float openingFadeIn = 1.2f, openingHold = 3, openingFadeOut = 1;
        [Min(.04f)] public float secondsPerCharacter = .13f;
        [Min(1f)] public float minimumSubtitleTime = 3.2f;
        [Min(.1f)] public float titleFade = 1;
        [NonSerialized] EndingSubtitlePage[][] pages;
        public float OpeningDuration => openingFadeIn + openingHold + openingFadeOut;
        public EndingSubtitlePage[] Pages(int index)
        {
            if (pages == null || pages.Length != shots.Length) pages = new EndingSubtitlePage[shots.Length][];
            return pages[index] ?? (pages[index] = EndingSubtitles.Build(shots[index], secondsPerCharacter, minimumSubtitleTime));
        }
        public float ShotDuration(int index)
        {
            var list = Pages(index);
            float reading = list.Length == 0 ? 0 : list[list.Length - 1].start + list[list.Length - 1].duration;
            return Mathf.Max(shots[index].duration, reading + shots[index].fadeOut + .15f);
        }
        public float Duration
        {
            get { float result = OpeningDuration; if (shots != null) for (int i = 0; i < shots.Length; i++) result += ShotDuration(i); return result; }
        }
        public float StartOf(int index)
        {
            float result = OpeningDuration;
            for (int i = 0; i < index; i++) result += ShotDuration(i);
            return result;
        }
        void OnValidate() => pages = null;
    }
}
