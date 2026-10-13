using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DDay
{
    public sealed class EndingSubtitlePage
    {
        public string text;
        public float start, duration;
    }

    public static class EndingSubtitles
    {
        // Keep every authored character. Break at punctuation/paragraphs, never rewrite.
        public static EndingSubtitlePage[] Build(HEEndingShot shot, float secondsPerCharacter, float minimum)
        {
            var result = new List<EndingSubtitlePage>();
            string source = string.IsNullOrEmpty(shot.narration) ? shot.subtitle ?? "" : shot.narration;
            var page = new StringBuilder(); float cursor = Mathf.Max(.2f, shot.fadeIn);
            void Flush()
            {
                string text = page.ToString().Trim(); page.Clear();
                if (text.Length == 0) return;
                float duration = Mathf.Max(minimum, Weight(text) * secondsPerCharacter + .65f);
                result.Add(new EndingSubtitlePage { text = text, start = cursor, duration = duration });
                cursor += duration + .3f;
            }
            int paragraphStart = 0;
            while (paragraphStart < source.Length)
            {
                int paragraphEnd = source.IndexOf('\n', paragraphStart);
                if (paragraphEnd < 0) paragraphEnd = source.Length;
                int at = paragraphStart;
                while (at < paragraphEnd)
                {
                    int end = at; float weight = 0; int softBreak = -1;
                    while (end < paragraphEnd)
                    {
                        char c = source[end]; weight += Weight(c);
                        if ("，；：、。！？…".IndexOf(c) >= 0) softBreak = end + 1;
                        end++;
                        if ("。！？".IndexOf(c) >= 0 || c == '…' && end < paragraphEnd && source[end] == '…')
                        {
                            while (end < paragraphEnd && "”’…".IndexOf(source[end]) >= 0) end++;
                            break;
                        }
                        if (weight > 54) { if (softBreak > at) end = softBreak; break; }
                    }
                    string fragment = source.Substring(at, end - at);
                    if (Weight(page.ToString()) + Weight(fragment) > 54) Flush();
                    page.Append(fragment); at = end;
                    if (Weight(page.ToString()) >= 38) Flush();
                }
                Flush(); paragraphStart = paragraphEnd + 1;
            }
            return result.ToArray();
        }
        static float Weight(char c) => char.IsWhiteSpace(c) ? .2f : c < 128 ? .48f : 1;
        public static float Weight(string text)
        { float weight = 0; foreach (char c in text) weight += Weight(c); return weight; }
    }
}
