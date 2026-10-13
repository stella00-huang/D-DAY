using System;
using System.Linq;
using UnityEngine;

namespace DDay
{
    [Serializable] public sealed class ComicActor
    {
        public string id, expression = "neutral";
        public float x = .5f, scale = 1f, tilt;
        public bool facingLeft;
    }
    [Serializable] public sealed class ComicShot
    {
        public string eventId, shotId, background, kind, itemId, effect, timeCaption;
        public int firstLine, lastLine;
        public float tint = 1f;
        public ComicActor[] actors;
    }
    [Serializable] public sealed class ComicPresentation
    {
        public ComicShot[] shots;
        static ComicPresentation cached;
        public static ComicPresentation Current
        {
            get
            {
                if (cached == null)
                {
                    TextAsset source = Resources.Load<TextAsset>("DDay/ComicPresentation");
                    cached = source == null ? new ComicPresentation { shots = Array.Empty<ComicShot>() }
                        : JsonUtility.FromJson<ComicPresentation>(source.text);
                }
                return cached;
            }
        }
        public ComicShot Find(string eventId, int line) => shots?.FirstOrDefault(shot =>
            shot.eventId == eventId && line >= shot.firstLine && line <= shot.lastLine);
    }
}
