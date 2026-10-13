using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Deterministic slicing of the supplied sheet. The original attachment is retained in Backups.
public static class DDayDay7PortraitImport
{
    const string Sheet = "Assets/Resources/DDay/Story/Day7/PortraitSheet_Cleaned.png";
    const string Folder = "Assets/Resources/DDay/Story/Day7/Portraits";
    const int Size = 320;
    // Identify by appearance, not the reversed ORIN/METIS labels in the source.
    static readonly string[] Columns = { "fox", "shiba", "bear", "cat", "panda", "wolf", "boss" };
    static readonly int[] Centers = { 135, 387, 636, 887, 1140, 1390, 1641 };

    [MenuItem("D-DAY/Import Day7 Suspect Portraits")]
    public static void Import()
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!source.LoadImage(File.ReadAllBytes(Sheet)) || source.width != 1774 || source.height != 887)
            throw new InvalidOperationException("Day7 sheet must be the supplied 1774 x 887 layout.");
        Directory.CreateDirectory(Folder);
        for (int column = 0; column < Columns.Length; column++)
            for (int row = 0; row < 2; row++)
            {
                int cx = Centers[column], cy = row == 0 ? 344 : 655;
                Color32 backdrop = source.GetPixel(cx, source.height - 1 - (cy - 138));
                var pixels = new Color32[Size * Size];
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                {
                    float dx = x + .5f - Size / 2f, dy = y + .5f - Size / 2f;
                    if (dx * dx + dy * dy > 159 * 159) continue;
                    Color32 color = backdrop;
                    if (Mathf.Abs(dx) <= 123 && Mathf.Abs(dy) <= 151)
                    {
                        int sx = cx + x - Size / 2, sy = cy + y - Size / 2;
                        Color32 sample = source.GetPixel(sx, source.height - 1 - sy);
                        float ry = row == 0 ? 140 : 148;
                        float ellipse = dx * dx / (123 * 123) + dy * dy / (ry * ry);
                        bool withinOriginal = ellipse <= 1;
                        bool cream = sample.r > 229 && sample.g > 214 && sample.b > 193;
                        // Extend only the portrait's colored backdrop to a true circle.
                        // Keep ears protruding beyond the old oval; never stretch the animal.
                        if ((withinOriginal || !cream) && !(cream && ellipse > .90f)) color = sample;
                    }
                    color.a = 255;
                    pixels[(Size - 1 - y) * Size + x] = color;
                }
                var portrait = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                portrait.SetPixels32(pixels); portrait.Apply();
                string path = Folder + "/" + Columns[column] + (row == 0 ? "_neutral.png" : "_surprised.png");
                File.WriteAllBytes(path, portrait.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(portrait);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 512;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(.5f, .5f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        UnityEngine.Object.DestroyImmediate(source);
        AssetDatabase.Refresh();
        Debug.Log("DDAY_DAY7_PORTRAITS_IMPORTED: 14 circular Sprites, grey wolf=ORIN, black cat=METIS.");
    }
}
