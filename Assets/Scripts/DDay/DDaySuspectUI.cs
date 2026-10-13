using System;
using UnityEngine;

namespace DDay
{
    // Day7 presentation only. Employee indices, seat rules and dialogue portraits are unchanged.
    public static class Day7Suspects
    {
        public static readonly string[] Ids = { "fox", "shiba", "bear", "wolf", "panda", "cat", "boss" };
        public const string ResourceRoot = "DDay/Story/Day7/Portraits/";
        public static bool Contains(string id) => Array.IndexOf(Ids, id) >= 0;
        public static string Name(string id) => id == "boss" ? "马 老板" : SeatRules.DisplayName(id);
        public static Sprite Portrait(string id, bool selected) => Resources.Load<Sprite>(
            ResourceRoot + id + (selected ? "_surprised" : "_neutral"));
        public static Vector2 Center(int index) => index < 4
            ? new Vector2(504 + 304 * index, 360) : new Vector2(656 + 304 * (index - 4), 700);
        public static readonly Rect ConfirmRect = new Rect(760, 948, 400, 72);
        public const float Diameter = 224;
        public static Rect PortraitRect(int index, bool selected)
        {
            float size = Diameter * (selected ? 1.05f : 1f);
            Vector2 center = Center(index);
            return new Rect(center.x - size / 2, center.y - size / 2, size, size);
        }
    }

    public sealed partial class DDayUI
    {
        Texture2D suspectRing;

        void DrawSuspects()
        {
            Fill(0, 0, 1920, 1080, new Color(.025f, .04f, .045f, .77f));
            DrawText(new Rect(280, 76, 1360, 112), "谁促成了 SISY 的死亡？",
                new GUIStyle(title) { fontSize = 64, normal = { textColor = warmText } });
            Event e = Event.current;
            for (int i = 0; i < Day7Suspects.Ids.Length; i++)
            {
                string id = Day7Suspects.Ids[i];
                bool selected = game.Day7SelectedSuspectId == id;
                Rect rect = Day7Suspects.PortraitRect(i, selected);
                Sprite portrait = Day7Suspects.Portrait(id, selected);
                if (portrait != null) GUI.DrawTexture(rect, portrait.texture, ScaleMode.ScaleToFit, true);
                if (suspectRing == null) suspectRing = MakeSuspectRing();
                GUI.color = selected ? new Color(1, .80f, .43f) : new Color(.64f, .62f, .54f, .7f);
                GUI.DrawTexture(new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + 10), suspectRing);
                GUI.color = Color.white;
                Vector2 center = Day7Suspects.Center(i);
                DrawText(new Rect(center.x - 144, center.y + 132, 288, 50), Day7Suspects.Name(id),
                    new GUIStyle(body) { fontSize = 28, alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = warmText } });
                // Round hit area matches the visible portrait, including the selected size.
                if (e.type == EventType.MouseDown && e.button == 0
                    && Vector2.Distance(e.mousePosition, center) <= rect.width / 2)
                { e.Use(); game.SelectDay7Suspect(id); }
            }
            Rect confirm = Day7Suspects.ConfirmRect;
            if (PixelButton("确认选择", confirm.x, confirm.y, confirm.width, confirm.height,
                game.CanConfirmDay7Suspect)) game.ConfirmDay7Suspect();
        }

        static Texture2D MakeSuspectRing()
        {
            const int size = 320;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = "Day7 selection ring", filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float radius = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(160, 160));
                pixels[y * size + x] = new Color32(255, 255, 255,
                    (byte)(radius >= 154 && radius <= 159 ? 255 : 0));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return texture;
        }
    }
}
