using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DDay;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DDayMainMenuVerification
{
    const string Key="DDay.MenuVerification";
    static string Output=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Docs/FinalCoverVerification");
    static string Save=>Path.Combine(Output,"TestAutoSave.json");
    static IEnumerator sequence;
    static DDayGame game;
    static DDayMainMenu menu;
    static readonly List<string> evidence=new List<string>();
    static DateTime began;
    static DDayMainMenuVerification(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
    [MenuItem("D-DAY/Main Menu/Verify APIS Menu in Play Mode")]
    public static void Start()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before verification.");
        Directory.CreateDirectory(Output);sequence=null;evidence.Clear();
        SessionState.SetString(Key+".Override",Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE")??"");
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE",Save);
        if(File.Exists(Save))File.Delete(Save);
        SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            if(sequence==null){began=DateTime.UtcNow;sequence=Run();}
            if((DateTime.UtcNow-began).TotalSeconds>90)throw new Exception("Menu verification timeout");
            if(!sequence.MoveNext())Finish(true,"Final cover rendered at three 16:9 sizes, 4:3 and ultrawide. UGUI raycasts/events and original actions tested; physical mouse, listening and Windows quit not claimed.");
        }catch(Exception e){Finish(false,e.ToString());}
    }
    static IEnumerable<object> Wait(float seconds)
    {double until=Time.realtimeSinceStartupAsDouble+seconds;while(Time.realtimeSinceStartupAsDouble<until)yield return null;}
    static void Check(bool pass,string text){if(!pass)throw new Exception(text);evidence.Add("PASS | "+text);}
    static IEnumerator Run()
    {
        do { yield return null; game = UnityEngine.Object.FindFirstObjectByType<DDayGame>(); menu = UnityEngine.Object.FindFirstObjectByType<DDayMainMenu>(); } while (game == null || menu == null);
        foreach (var t in Wait(.25f)) yield return t;
        Check(menu.Visible && menu.game == game, "Existing scene/menu campaign binding intact");
        // Enter Play can stall while shaders warm up; measure a fresh menu entry after that.
        var titleUi = game.GetComponent<DDayUI>(); titleUi.OpenTitleSettings();
        foreach (var t in Wait(.15f)) yield return t;
        typeof(DDayUI).GetField("settingsOpen", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(titleUi, false);
        foreach (var t in Wait(.15f)) yield return t;
        Check(menu.shade.color.a > .3f, "Menu begins with a black fade-in");
        foreach (var t in Wait(1)) yield return t;
        Check(menu.shade.color.a < .01f, "Menu fade-in completes after one unscaled second");
        Check(UnityEngine.Object.FindObjectsByType<DDayMainMenu>(FindObjectsSortMode.None).Length == 1, "Exactly one existing menu controller");
        Check(UnityEngine.Object.FindObjectsByType<DDayAudioManager>(FindObjectsSortMode.None).Length == 1, "Exactly one existing audio service");
        Check(!menu.resume.interactable, "Continue disabled without save");
        Check(menu.transform.Find("D-day logo") == null && menu.transform.Find("D-DAY title") == null, "No old logo or duplicate title object");
        Check(menu.GetComponentsInChildren<TMP_Text>(true).Length == 0, "No added menu text or labels over baked cover");
        Check(AssetDatabase.GetAssetPath(menu.background.sprite) == DDayMainMenuAuthoring.CoverPath, "Supplied final cover bound");
        Check(menu.background.sprite.texture.width == 1677 && menu.background.sprite.texture.height == 938, "Imported dimensions preserved");
        var buttons = new[] { menu.start, menu.resume, menu.settings, menu.quit };
        Check(menu.buttons.GetComponentsInChildren<Button>(true).Length == 4, "Four existing menu actions only");
        foreach (var button in buttons)
        {
            var image = button.GetComponent<Image>();
            Check(image.color.a == 0 && image.sprite == null && button.transition == Selectable.Transition.None, "Transparent hit area: " + button.name);
            Check(button.GetComponent<DDayPaperButton>().highlight != null && !button.GetComponent<DDayPaperButton>().highlight.raycastTarget, "Non-blocking paper effect: " + button.name);
            Check(buttons.Contains(button.navigation.selectOnUp) && buttons.Contains(button.navigation.selectOnDown), "Original keyboard navigation: " + button.name);
        }
        Check(!menu.background.raycastTarget && !menu.shade.raycastTarget, "Background/fade cannot intercept clicks");
        int musicStarts = DDayAudioManager.Instance.MusicStarts;
        foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1600, 900), new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(2560, 1080) })
        {
            Resolution(size.x, size.y); foreach (var t in Wait(.3f)) yield return t;
            Check(Screen.width == size.x && Screen.height == size.y, "Actual Game View " + size);
            Check(Mathf.Abs(menu.cover.rect.width / menu.cover.rect.height - 1677f / 938) < .001f, "Cover has original aspect: " + size);
            for (int i = 0; i < buttons.Length; i++)
            {
                var rect = (RectTransform)buttons[i].transform;
                var pixel = DDayMainMenuAuthoring.PaperPixels[i];
                var expected = new Vector2(pixel.center.x / 1677, 1 - pixel.center.y / 938);
                var point = (Vector2)menu.cover.TransformPoint(menu.cover.rect.min + Vector2.Scale(menu.cover.rect.size, expected));
                Check(Vector2.Distance(point, rect.TransformPoint(rect.rect.center)) < .05f, "Cover-aligned hit area: " + size + " " + buttons[i].name
                    + " expected=" + point + " actual=" + rect.TransformPoint(rect.rect.center) + " parent=" + rect.parent.name
                    + " offsets=" + ((RectTransform)rect.parent).offsetMin + "/" + ((RectTransform)rect.parent).offsetMax);
                Check(Hits(point) == buttons[i].gameObject, "Real UGUI raycast reaches correct action: " + size + " " + buttons[i].name);
                Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
                Check(corners[0].x >= 0 && corners[0].y >= 0 && corners[2].x <= Screen.width && corners[2].y <= Screen.height, "Button completely inside viewport: " + buttons[i].name);
                if (i > 0) {
                    Vector3[] previous = new Vector3[4]; ((RectTransform)buttons[i-1].transform).GetWorldCorners(previous);
                    Check(corners[2].y < previous[0].y, "No overlapping hit areas: " + buttons[i].name);
                }
            }
            // The important title and APIS areas stay visible even at the largest breathing scale.
            foreach (var pixel in new[] { new Vector2(580, 150), new Vector2(1120, 315), new Vector2(138, 235), new Vector2(210, 286) }) {
                var point = menu.cover.TransformPoint(menu.cover.rect.min + Vector2.Scale(menu.cover.rect.size, new Vector2(pixel.x / 1677, 1 - pixel.y / 938)));
                Check(point.x >= 0 && point.y >= 0 && point.x <= Screen.width && point.y <= Screen.height, "Original title/APIS visible: " + size + " " + pixel);
            }
            Check(Hits(new Vector2(20, Screen.height / 2)) == null, "Cover outside buttons does not intercept clicks: " + size);
            ScreenCapture.CaptureScreenshot(Path.Combine(Output, "APIS_" + size.x + "x" + size.y + ".png")); foreach (var t in Wait(.25f)) yield return t;
        }
        Check(DDayAudioManager.Instance.MusicStarts == musicStarts && DDayAudioManager.Instance.CurrentCue == BgmCue.MainMenu, "Menu BGM not restarted by resize or repaint");
        Resolution(1920, 1080); foreach (var t in Wait(.2f)) yield return t;
        var paper = menu.start.GetComponent<DDayPaperButton>();
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        int sounds = DDayAudioManager.Instance.SoundPlays;
        ExecuteEvents.Execute(menu.start.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        foreach (var t in Wait(.25f)) yield return t;
        Check(paper.HighlightStrength > .99f && paper.highlight.color.a > .09f, "Warm paper highlight fades in");
        Check(menu.start.transform.localScale == Vector3.one && menu.start.transform.localRotation == Quaternion.identity, "Baked buttons never scale or rotate");
        Check(DDayAudioManager.Instance.SoundPlays == sounds, "Hover has no repeated sound");
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "Hover.png")); foreach (var t in Wait(.2f)) yield return t;
        Time.timeScale = 0;
        ExecuteEvents.Execute(menu.start.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        foreach (var t in Wait(.15f)) yield return t;
        Check(paper.PressStrength > .99f && paper.highlight.color.r < .2f, "Subtle press shade works at timeScale zero");
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "Press.png")); foreach (var t in Wait(.2f)) yield return t;
        ExecuteEvents.Execute(menu.start.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(menu.start.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        EventSystem.current.SetSelectedGameObject(null);
        foreach (var t in Wait(.25f)) yield return t;
        Check(paper.highlight.color.a < .001f, "Paper overlay clears after leaving");
        menu.start.Select(); foreach (var t in Wait(.25f)) yield return t;
        Check(paper.HighlightStrength > .99f, "Keyboard focus receives the same subtle highlight");
        ExecuteEvents.Execute(menu.start.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        menu.start.onClick.Invoke();
        foreach (var t in Wait(.4f)) yield return t;
        Check(menu.IsTransitioning && game.CurrentPhase == Phase.Title && !menu.buttons.interactable && menu.shade.color.a > .2f, "Keyboard Start disables repeated clicks and fades menu");
        foreach (var t in Wait(.5f)) yield return t;
        Time.timeScale = 1;
        Check(game.Day == 1 && game.Loop == 1 && game.CurrentPhase == Phase.Layout && !menu.Visible, "Original StartGame enters real Day1");
        Check(game.GetComponent<DDayTutorialController>().Active, "Existing Day1 tutorial still activates");
        Check(DDayAudioManager.Instance.SoundPlays == sounds + 1, "Exactly one guarded Start click sound");
        game.ReturnToTitle(); foreach (var t in Wait(.2f)) yield return t;
        File.WriteAllText(Save, "{\"loop\":2,\"day\":4,\"phase\":\"Layout\",\"sisyAlive\":true,\"seats\":[0,1,2,3,4,5,6],\"records\":[{\"id\":\"menu_test\",\"category\":\"情报\",\"title\":\"test\",\"description\":\"preserved\",\"unread\":true}]}");
        foreach (var t in Wait(1.1f)) yield return t;
        Check(menu.resume.interactable, "Continue enabled with an existing save");
        // The animated cover and hit areas still agree at the peak zoom.
        float actualScale = menu.cover.localScale.x;
        menu.cover.localScale = Vector3.one * 1.012f; Canvas.ForceUpdateCanvases();
        foreach (var button in buttons) Check(Hits(((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center)) == button.gameObject, "Hit area tracks the peak zoom: " + button.name);
        menu.cover.localScale = Vector3.one * actualScale;
        Click(menu.resume); menu.resume.onClick.Invoke(); foreach (var t in Wait(.95f)) yield return t;
        Check(game.Loop == 2 && game.Day == 4 && game.CurrentPhase == Phase.Layout, "Continue restores original loop/day");
        Check(File.ReadAllText(Save).Contains("preserved"), "Continue preserves record data");
        game.ReturnToTitle(); foreach (var t in Wait(1.1f)) yield return t;
        var saved = File.ReadAllText(Save); Click(menu.quit); foreach (var t in Wait(.2f)) yield return t;
        Check(game.CurrentPhase == Phase.Title && File.ReadAllText(Save) == saved, "Original Quit callback keeps save (Editor ignores OS quit)");
        Click(menu.settings); foreach (var t in Wait(.2f)) yield return t;
        Check(game.GetComponent<DDayUI>().TitleSettingsOpen && !menu.Visible, "Settings opens the existing UI and disables all cover targets");
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "TitleSettings.png")); foreach (var t in Wait(.2f)) yield return t;
    }
    static GameObject Hits(Vector2 position)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, results);
        return results.Count > 0 ? results[0].gameObject : null;
    }
    static void Click(Button button)
    {
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current) { position = rect.TransformPoint(rect.rect.center), button = PointerEventData.InputButton.Left };
        Check(Hits(pointer.position) == button.gameObject, "Action dispatched to raycast target: " + button.name);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    static void Resolution(int width,int height)
    {
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;var assembly=typeof(EditorWindow).Assembly;
        var type=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);
        if(!SessionState.GetBool(Key+".SizeStored",false)){SessionState.SetInt(Key+".Size",(int)selected.GetValue(view));SessionState.SetBool(Key+".SizeStored",true);}
        var sizes=assembly.GetType("UnityEditor.GameViewSizes");var singleton=sizes.BaseType.GetProperty("instance",flags).GetValue(null);
        var group=sizes.GetProperty("currentGroup",flags).GetValue(singleton);var get=group.GetType().GetMethod("GetGameViewSize",flags);
        for(int i=0;i<(int)group.GetType().GetMethod("GetTotalCount",flags).Invoke(group,null);i++)
        {var size=get.Invoke(group,new object[]{i});var st=size.GetType();if((int)st.GetProperty("width",flags).GetValue(size)==width&&(int)st.GetProperty("height",flags).GetValue(size)==height){selected.SetValue(view,i);view.Focus();view.Repaint();return;}}
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var kind = assembly.GetType("UnityEditor.GameViewSizeType");
        var added = Activator.CreateInstance(sizeType, flags, null, new object[] { Enum.Parse(kind, "FixedResolution"), width, height, "D-DAY cover " + width + "x" + height }, null);
        group.GetType().GetMethod("AddCustomSize", flags).Invoke(group, new[] { added });
        selected.SetValue(view, (int)group.GetType().GetMethod("GetTotalCount", flags).Invoke(group, null) - 1); view.Focus(); view.Repaint();
    }
    static void Finish(bool success,string message)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);Time.timeScale=1;
        if(game!=null)game.ReturnToTitle();Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE",SessionState.GetString(Key+".Override",""));
        if(SessionState.GetBool(Key+".SizeStored",false))
        {var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
            .SetValue(EditorWindow.GetWindow(type),SessionState.GetInt(Key+".Size",0));SessionState.SetBool(Key+".SizeStored",false);}
        File.WriteAllLines(Path.Combine(Output,"Results.txt"),new[]{success?"PASS":"FAIL",message}.Concat(evidence));
        sequence=null;EditorApplication.isPlaying=false;Debug.Log("D-DAY APIS menu verification "+(success?"PASS":"FAIL"));
    }
}
