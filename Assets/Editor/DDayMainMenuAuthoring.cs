using System;
using DDay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DDayMainMenuAuthoring
{
    public const string PrefabPath = "Assets/Prefabs/DDay/APISMainMenu.prefab";
    public const string CoverPath = "Assets/Art/MainMenu/APIS/DDAY_MainMenu_Final.png";
    // Measured from the supplied 1677 x 938 cover, top-left coordinates.
    public static readonly Rect[] PaperPixels = {
        new Rect(650, 337, 390, 77), new Rect(650, 438, 390, 77),
        new Rect(650, 539, 390, 78), new Rect(650, 640, 390, 79)
    };
    [MenuItem("D-DAY/Main Menu/Apply Final Cover")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before menu authoring.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/TodayWhereToSit.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved existing TodayWhereToSit scene first.");
        var existing = UnityEngine.Object.FindFirstObjectByType<DDayMainMenu>();
        if (existing == null || existing.game == null) throw new InvalidOperationException("Existing menu/campaign binding is missing.");
        AssetDatabase.ImportAsset(CoverPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(CoverPath);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
        importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoverPath);
        if (sprite == null) throw new InvalidOperationException("Final cover import failed.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var menu = root.GetComponent<DDayMainMenu>();
            foreach (string name in new[] { "D-DAY title", "D-day logo", "Menu buttons/制作组" })
            {
                var obsolete = root.transform.Find(name);
                if (obsolete != null) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
            }
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var underlay = Child("Letterbox", root.transform);
            Stretch(underlay); underlay.SetAsFirstSibling();
            var black = underlay.GetComponent<Image>() ?? underlay.gameObject.AddComponent<Image>();
            black.color = Color.black; black.raycastTarget = false;
            var cover = Child("Cover", root.transform);
            cover.anchorMin = cover.anchorMax = cover.pivot = new Vector2(.5f, .5f);
            cover.sizeDelta = new Vector2(1920, 1920 * sprite.rect.height / sprite.rect.width);
            cover.anchoredPosition = Vector2.zero; cover.localScale = Vector3.one; menu.cover = cover;
            var aspect = menu.background.GetComponent<AspectRatioFitter>();
            if (aspect != null) UnityEngine.Object.DestroyImmediate(aspect);
            menu.background.transform.SetParent(cover, false); Stretch(menu.background.rectTransform);
            menu.background.sprite = sprite; menu.background.preserveAspect = true; menu.background.raycastTarget = false;
            var list = (RectTransform)menu.buttons.transform;
            list.SetParent(cover, false); Stretch(list);
            var layout = list.GetComponent<VerticalLayoutGroup>();
            if (layout != null) UnityEngine.Object.DestroyImmediate(layout);
            var buttons = new[] { menu.start, menu.resume, menu.settings, menu.quit };
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i]; var rect = (RectTransform)button.transform;
                var pixels = PaperPixels[i];
                rect.anchorMin = new Vector2(pixels.xMin / sprite.rect.width, 1 - pixels.yMax / sprite.rect.height);
                rect.anchorMax = new Vector2(pixels.xMax / sprite.rect.width, 1 - pixels.yMin / sprite.rect.height);
                rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
                var element = rect.GetComponent<LayoutElement>();
                if (element != null) UnityEngine.Object.DestroyImmediate(element);
                foreach (var label in rect.GetComponentsInChildren<TMP_Text>(true)) UnityEngine.Object.DestroyImmediate(label.gameObject);
                var image = rect.GetComponent<Image>(); image.sprite = null; image.overrideSprite = null;
                image.color = Color.clear; image.raycastTarget = true;
                button.transition = Selectable.Transition.None; button.spriteState = default; button.targetGraphic = image;
                button.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i + 3) % 4], selectOnDown = buttons[(i + 1) % 4] };
                var effectRect = Child("Paper light", rect); Stretch(effectRect);
                var effect = effectRect.GetComponent<DDayPaperHighlight>() ?? effectRect.gameObject.AddComponent<DDayPaperHighlight>();
                effect.outline = new[] { new Vector2(.01f, .07f), new Vector2(.97f, .07f),
                    new Vector2(.99f, .93f), new Vector2(.04f, .98f) };
                effect.color = Color.clear; effect.raycastTarget = false;
                rect.GetComponent<DDayPaperButton>().highlight = effect;
            }
            menu.shade.transform.SetAsLastSibling(); menu.shade.raycastTarget = false; menu.shade.color = Color.clear;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        // The old layout group left driven RectTransform overrides in the scene instance.
        // Revert only these menu transforms, keeping the scene's DDayGame binding untouched.
        foreach (var rect in new[] { existing.background.rectTransform, (RectTransform)existing.buttons.transform,
            (RectTransform)existing.start.transform, (RectTransform)existing.resume.transform,
            (RectTransform)existing.settings.transform, (RectTransform)existing.quit.transform })
            PrefabUtility.RevertObjectOverride(rect, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("D-DAY final cover saved: one unchanged illustration, four transparent hit areas, no extra title/logo.");
    }
    static RectTransform Child(string name, Transform parent)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null) return existing;
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }
    static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
