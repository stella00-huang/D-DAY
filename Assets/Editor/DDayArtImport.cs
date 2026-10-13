using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DDayArtImport
{
    [MenuItem("D-DAY/Import Day4 and Day6 Comic Pages")]
    public static void ImportDayFourAndSixComics()
    {
        foreach (int day in new[] { 4, 6 })
        {
            string path = "Assets/Resources/DDay/Story/Day" + day + "/Day" + day + "ComicPage.png";
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Missing comic page: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.SaveAndReimport();
        }
        Debug.Log("D-DAY: Day4/Day6 original pages imported without resizing/compression.");
    }
    [MenuItem("D-DAY/Import Day2 Comic Page")]
    public static void ImportDayTwoComic()
    {
        const string path = "Assets/Resources/DDay/Story/Day2/Day2ComicPage.png";
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Missing Day2 comic page");
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.SaveAndReimport();
        Debug.Log("D-DAY: original Day2 page imported without resizing/compression/mipmaps.");
    }
    [MenuItem("D-DAY/Import Day1 Comic Page")]
    public static void ImportDayOneComic()
    {
        const string path = "Assets/Resources/DDay/Story/Day1/Day1ComicPage.png";
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Missing Day1 comic page");
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.SaveAndReimport();
        Debug.Log("D-DAY: original Day1 page imported without resizing/compression/mipmaps.");
    }
    [MenuItem("D-DAY/Import Rearranged Facilities Background")]
    public static void ImportFacilitiesBackground()
    {
        const string path = "Assets/Resources/DDay/OfficeTemplate/BackgroundFacilitiesV2.png";
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Missing facilities background");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
    }
    [MenuItem("D-DAY/Preview DAY 1 Layout")]
    public static void PreviewDayOne()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("D-DAY: enter Play Mode before previewing DAY 1.");
            return;
        }
        var game = Object.FindFirstObjectByType<DDay.DDayGame>();
        if (game == null) { Debug.LogError("D-DAY: runtime controller missing."); return; }
        game.Restart();
        game.StartGame();
        for (int i = 0; i < 10 && game.CurrentPhase == DDay.Phase.Narrative; i++)
            game.AdvanceNarrative();
        Debug.Log("D-DAY: DAY 1 visual preview ready; gameplay remains active.");
    }

    [MenuItem("D-DAY/Preview DAY 2 Layout")]
    public static void PreviewDayTwo()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("D-DAY: enter Play Mode before previewing DAY 2.");
            return;
        }
        var game = Object.FindFirstObjectByType<DDay.DDayGame>();
        if (game == null) { Debug.LogError("D-DAY: runtime controller missing."); return; }
        game.Restart();
        var begin = typeof(DDay.DDayGame).GetMethod("BeginDay",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (begin == null) { Debug.LogError("D-DAY: BeginDay method missing."); return; }
        begin.Invoke(game, new object[] { 2 });
        for (int i = 0; i < 10 && game.CurrentPhase == DDay.Phase.Narrative; i++)
            game.AdvanceNarrative();
        Debug.Log("D-DAY: DAY 2 layout preview ready; gameplay remains active.");
    }

    [MenuItem("D-DAY/Preview DAY 2 Bear Seat 3")]
    public static void PreviewDayTwoBearSeatThree()
    {
        PreviewDayTwo();
        var game = Object.FindFirstObjectByType<DDay.DDayGame>();
        if (game == null || game.CurrentPhase != DDay.Phase.Layout) return;
        game.TryPlaceAnimal(5, 3);
        game.SelectAnimal(5);
    }

    [MenuItem("D-DAY/Preview DAY 2 Bear Seat 7")]
    public static void PreviewDayTwoBearSeatSeven()
    {
        PreviewDayTwo();
        var game = Object.FindFirstObjectByType<DDay.DDayGame>();
        if (game != null && game.CurrentPhase == DDay.Phase.Layout) game.SelectAnimal(5);
    }

    [MenuItem("D-DAY/Preview DAY 2 Fox Right Bubble")]
    public static void PreviewDayTwoFoxRight()
    {
        PreviewDayTwo();
        var game = Object.FindFirstObjectByType<DDay.DDayGame>();
        if (game == null || game.CurrentPhase != DDay.Phase.Layout) return;
        game.TryPlaceAnimal(2, 3);
        game.TryPlaceAnimal(3, 7);
        game.SelectAnimal(2);
    }

    [MenuItem("D-DAY/Test 16-9 Game View")]
    public static void SetGameView16By9()
    {
        if (!EditorApplication.isPlaying) return;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Static;
        var editorAssembly = typeof(EditorWindow).Assembly;
        var viewType = editorAssembly.GetType("UnityEditor.GameView");
        var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
        if (viewType == null || sizesType == null) return;
        var view = EditorWindow.GetWindow(viewType);
        var singleton = sizesType.BaseType?.GetProperty("instance", flags)?.GetValue(null);
        var group = sizesType.GetProperty("currentGroup", flags)?.GetValue(singleton);
        var total = group?.GetType().GetMethod("GetTotalCount", flags);
        var getSize = group?.GetType().GetMethod("GetGameViewSize", flags);
        var selected = viewType.GetProperty("selectedSizeIndex", flags);
        if (total == null || getSize == null || selected == null)
        {
            Debug.LogWarning("D-DAY: 16:9 Game View option could not be inspected in this Unity version.");
            return;
        }
        int count = (int)total.Invoke(group, null);
        for (int i = 0; i < count; i++)
        {
            var size = getSize.Invoke(group, new object[] { i });
            if (size == null) continue;
            var sizeType = size.GetType();
            object Member(string name) => sizeType.GetProperty(name, flags)?.GetValue(size)
                ?? sizeType.GetField(name, flags)?.GetValue(size);
            var label = (Member("baseText") ?? Member("m_BaseText") ?? size.ToString())?.ToString();
            int width = System.Convert.ToInt32(Member("width") ?? Member("m_Width") ?? 0);
            int height = System.Convert.ToInt32(Member("height") ?? Member("m_Height") ?? 0);
            if ((label == null || !label.Contains("16:9"))
                && (width <= 0 || height <= 0 || width * 9 != height * 16)) continue;
            selected.SetValue(view, i);
            view.Repaint();
            Debug.Log("D-DAY: Game View set to " + label + " " + width + "x" + height
                + " (size index " + i + ").");
            return;
        }
        Debug.LogWarning("D-DAY: no 16:9 Game View option found in " + count
            + " sizes; current size unchanged.");
    }

    [MenuItem("D-DAY/Inspect Game View Sizes")]
    public static void InspectGameViewSizes()
    {
        var editor = typeof(EditorWindow).Assembly;
        var sizesType = editor.GetType("UnityEditor.GameViewSizes");
        var groupType = editor.GetType("UnityEditor.GameViewSizeGroupType");
        var viewType = editor.GetType("UnityEditor.GameView");
        Debug.Log("D-DAY GameView reflection types: sizes=" + sizesType +
            ", group=" + groupType + ", view=" + viewType);
        if (sizesType == null || groupType == null || viewType == null) return;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Static;
        Debug.Log("D-DAY GameView size methods: " + string.Join(", ",
            System.Array.ConvertAll(sizesType.GetMethods(flags), method => method.Name)));
        Debug.Log("D-DAY GameView selected size members: " + string.Join(", ",
            System.Array.ConvertAll(viewType.GetMembers(flags), member => member.Name)
                .Where(name => name.ToLowerInvariant().Contains("size"))));
        var instanceProperty = sizesType.GetProperty("instance", flags);
        var instance = instanceProperty?.GetValue(null);
        Debug.Log("D-DAY GameView sizes singleton: " + instanceProperty +
            ", instance=" + instance);
        if (instance == null) return;
        var group = sizesType.GetProperty("currentGroup", flags)?.GetValue(instance);
        Debug.Log("D-DAY GameView current group: " + group +
            ", methods=" + (group == null ? "none" : string.Join(", ",
                System.Array.ConvertAll(group.GetType().GetMethods(flags), method => method.Name))));
    }

    [MenuItem("D-DAY/Toggle Seat Debug")]
    public static void ToggleSeatDebug()
    {
        var ui = Object.FindFirstObjectByType<DDay.DDayUI>();
        if (ui == null) { Debug.LogWarning("D-DAY: UI is available in Play Mode."); return; }
        ui.SeatDebugVisible = !ui.SeatDebugVisible;
        Debug.Log("D-DAY seat debug " + (ui.SeatDebugVisible ? "on" : "off"));
    }

    [MenuItem("D-DAY/Preview Shiba Near Printer")]
    public static void PreviewShibaNear()
    {
        PreviewDayOne();
        var game = Object.FindFirstObjectByType<DDay.DDayGame>();
        if (game != null && game.CurrentPhase == DDay.Phase.Layout) game.SelectAnimal(3);
    }

    [MenuItem("D-DAY/Preview Shiba Far From Printer")]
    public static void PreviewShibaFar()
    {
        PreviewDayOne();
        var game = Object.FindFirstObjectByType<DDay.DDayGame>();
        if (game != null && game.CurrentPhase == DDay.Phase.Layout)
        {
            game.TryPlaceAnimal(3, 0);
            game.SelectAnimal(3);
        }
    }

    [MenuItem("D-DAY/Preview Seat Closeup")]
    public static void PreviewSeatCloseup()
    {
        var camera = Camera.main;
        if (camera == null || !EditorApplication.isPlaying) return;
        camera.orthographicSize = 1.85f;
        camera.transform.position = new Vector3(-5.35f, -.15f, -10f);
    }

    [MenuItem("D-DAY/Restore Full Camera")]
    public static void RestoreFullCamera()
    {
        var camera = Camera.main;
        if (camera == null || !EditorApplication.isPlaying) return;
        camera.orthographicSize = 5.1f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    [MenuItem("D-DAY/Focus Game View")]
    public static void FocusGameView()
    {
        var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType == null) return;
        var window = EditorWindow.GetWindow(gameViewType);
        window.Show();
        window.Focus();
        window.Repaint();
    }

    [MenuItem("D-DAY/Configure New Pixel Art")]
    public static void Configure()
    {
        foreach (string name in new[] { "Employees", "Facilities",
            "OfficeTemplate/Background", "OfficeTemplate/BackgroundFacilitiesV2", "OfficeTemplate/Workstation",
            "OfficeTemplate/SeatedEmployees", "OfficeTemplate/DeskFrontFacing",
            "OfficeTemplate/PersonalProps" })
        {
            string path = "Assets/Resources/DDay/" +
                (name.StartsWith("OfficeTemplate/") ? name : "PixelV2/" + name) + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("D-DAY missing art: " + path); continue; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }
        Debug.Log("D-DAY: original character and facility sheets imported as point-filtered sprites.");
    }
}
