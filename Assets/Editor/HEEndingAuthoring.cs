using System;
using System.IO;
using DDay;
using UnityEditor;
using UnityEngine;

public static class HEEndingAuthoring
{
    public const string ConfigPath = "Assets/Resources/DDay/Endings/HE/HEEndingConfig.asset";

    [MenuItem("D-DAY/HE/Import and Configure")]
    public static void Configure()
    {
        string[] paths = { "Assets/Art/Endings/HE/01_Truth.png", "Assets/Art/Endings/HE/02_Consequences.png",
            "Assets/Art/Endings/HE/03_NewTomorrow.png" };
        var textures = new Texture2D[3];
        for (int i = 0; i < paths.Length; i++)
        {
            AssetDatabase.ImportAsset(paths[i]);
            var importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing HE illustration: " + paths[i]);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false; importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.SaveAndReimport();
            textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]);
        }
        var config = AssetDatabase.LoadAssetAtPath<HEEndingConfig>(ConfigPath);
        if (config == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            AssetDatabase.Refresh();
            config = ScriptableObject.CreateInstance<HEEndingConfig>();
            config.shots = new[] {
                new HEEndingShot {
                    id = "HE_01_Truth", image = textures[0], duration = 10,
                    startScale = 1, endScale = 1.07f, endOffset = new Vector2(-.004f, .008f),
                    subtitle = "那些被隐藏的秘密，终于有了被揭开的机会。",
                    subtitleStart = 7, subtitleEnd = 9, fadeIn = 1, fadeOut = 1,
                    atmosphere = HEAtmosphere.DeskLamp, lightPosition = new Vector2(.10f, .73f),
                    lightIntensity = .035f, dustIntensity = .045f },
                new HEEndingShot {
                    id = "HE_02_Consequences", image = textures[1], duration = 10,
                    startScale = 1.04f, endScale = 1, startOffset = new Vector2(-.010f, 0),
                    subtitle = "这一次，作恶者终于付出了代价。",
                    subtitleStart = 7, subtitleEnd = 9, fadeIn = 1, fadeOut = 1,
                    atmosphere = HEAtmosphere.PoliceLights, lightPosition = new Vector2(.95f, .63f),
                    lightIntensity = .045f, dustIntensity = .012f },
                new HEEndingShot {
                    id = "HE_03_NewTomorrow", image = textures[2], duration = 12,
                    startScale = 1.035f, endScale = 1,
                    subtitle = "这一次，我们终于可以选择自己的明天。",
                    subtitleStart = 6, subtitleEnd = 10, fadeIn = 2, fadeOut = 2,
                    atmosphere = HEAtmosphere.Sunset, lightPosition = new Vector2(.72f, .58f),
                    lightIntensity = .028f, dustIntensity = .038f }
            };
            config.endingSubtitle = "";
            config.openingTitle = "如露亦如电，应作如是观。";
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
        }
        Selection.activeObject = config;
        Debug.Log("D-DAY HE: original three illustrations connected; duration=" + config.Duration +
            "s. AudioClip slots intentionally empty (no suitable licensed project audio found).");
    }

    [MenuItem("D-DAY/HE/Preview Ending (Play Mode)")]
    public static void Preview()
    {
        var game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        if (!Application.isPlaying || game == null)
        { Debug.LogWarning("Open TodayWhereToSit and enter Play Mode before HE preview."); return; }
        game.GetComponent<HEEndingPlayer>().PlayPreview();
    }
    [MenuItem("D-DAY/HE/Preview Ending (Play Mode)", true)]
    static bool CanPreview() => Application.isPlaying && UnityEngine.Object.FindFirstObjectByType<DDayGame>() != null;
}
