using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DDay;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DDayFinalDecisionVerification
{
    const string Key = "DDay.FinalDecisionVerification";
    static string Output => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "FinalDecisionVerification");
    static string TestSave => Path.Combine(Output, "TestAutoSave.json");
    static readonly List<string> evidence = new List<string>();
    static IEnumerator run;
    static DateTime began;
    static string stage;
    static DDayGame game;
    static DDayFinalDecisionUI ui;
    static float lastStatus;
    static DDayFinalDecisionVerification() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick; }

    [MenuItem("D-DAY/Verify Final Decision in Play Mode")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play before validation."); return; }
        Directory.CreateDirectory(Output);
        SessionState.SetString(Key + ".SaveOverride", Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE") ?? "");
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE", TestSave);
        SessionState.SetBool(Key, true); evidence.Clear(); run = null; stage = "Starting";
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (run == null) { run = Sequence(); began = DateTime.UtcNow; }
            if ((DateTime.UtcNow - began).TotalSeconds > 480) throw new Exception("Validation timed out.");
            if (!run.MoveNext()) { Finish(true, "Live Game View validation completed."); return; }
            if (Time.realtimeSinceStartup > lastStatus && game != null)
            {
                lastStatus = Time.realtimeSinceStartup + .3f;
                File.WriteAllText(Path.Combine(Output, "LiveStatus.txt"), stage + " | phase=" + game.CurrentPhase
                    + " | question=" + game.FinalQuestion + " | time=" + ui.MovingTime.ToString("0.0")
                    + " | YES=" + (ui.YesButton == null ? "" : ui.YesButton.GetComponent<RectTransform>().anchoredPosition.ToString())
                    + " | escapes=" + ui.EscapeCount + " | paused=" + ui.IsPaused);
            }
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }
    static void Check(bool pass, string message)
    { if (!pass) throw new Exception(message); evidence.Add("PASS | " + message); }
    static void LoadChoice(int question = 0)
    {
        File.WriteAllText(TestSave, "{\"loop\":2,\"day\":7,\"phase\":\"FinalDecision\",\"sisyAlive\":true,"
            + "\"seats\":[0,1,2,3,4,5,6],\"yesCount\":" + question + ",\"finalQuestion\":" + question + "}");
        game.ContinueGame();
    }
    static void Capture(string name)
    { ScreenCapture.CaptureScreenshot(Path.Combine(Output, name + ".png")); evidence.Add("SCREENSHOT | " + name); }

    static IEnumerator Sequence()
    {
        do { yield return null; game = UnityEngine.Object.FindFirstObjectByType<DDayGame>(); } while (game == null || game.Data == null);
        ui = game.GetComponent<DDayFinalDecisionUI>();
        Check(ui != null, "Final decision uses native UGUI Buttons with the existing campaign");
        SetResolution(1280,720); LoadChoice(); yield return null;
        var initialYes = ui.YesButton.GetComponent<RectTransform>().anchoredPosition;
        var initialNo = ui.NoButton.GetComponent<RectTransform>().anchoredPosition;
        float end = Time.realtimeSinceStartup + 1.6f;
        while (Time.realtimeSinceStartup < end)
        {
            Check(ui.YesButton.GetComponent<RectTransform>().anchoredPosition == initialYes
                && ui.NoButton.GetComponent<RectTransform>().anchoredPosition == initialNo && ui.MoveCount == 0,
                "First question positions fixed"); yield return null;
        }
        Capture("01_FirstQuestion_1280x720");
        stage = "NativeFirstNO";
        while (game.CurrentPhase == Phase.FinalDecision) yield return null;
        Check(game.CurrentPhase == Phase.BadEnding && game.FinalQuestion == 1, "Native first NO immediately enters existing BE");
        Capture("02_FirstNO_BE");
        game.ReturnToTitle(); yield return null;
        LoadChoice(); yield return null;
        stage = "NativeFirstYES";
        while (game.CurrentPhase == Phase.FinalDecision && game.FinalQuestion == 0) yield return null;
        Check(game.CurrentPhase == Phase.FinalDecision && game.FinalQuestion == 1, "Native first YES enters the same question a second time");
        Check(ui.IsTransitioning, "Choices fade and briefly darken after the first YES");
        while (ui.IsTransitioning) yield return null;
        float secondStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - secondStart < .35f)
        { Check(ui.YesButton.GetComponent<RectTransform>().anchoredPosition == initialYes, "Second YES starts in normal position"); yield return null; }
        stage = "ObserveMovementAndMouseAvoidance";
        Vector2 min = initialYes, max = initialYes;
        bool sawPause = false;
        while (ui.MovingTime < 16.5f)
        {
            Check(game.CurrentPhase == Phase.FinalDecision, "No automatic ending while YES moves");
            CheckSafe();
            Vector2 position = ui.YesButton.GetComponent<RectTransform>().anchoredPosition;
            min = Vector2.Min(min, position); max = Vector2.Max(max, position);
            sawPause |= ui.IsPaused && ui.MoveCount > 1;
            yield return null;
        }
        Check((max - min).magnitude > 500 && ui.MoveCount > 1, "YES travels across a large screen area with multiple trajectories");
        Check(sawPause && ui.CurrentSpeed <= 140, "Catch windows and 15-second speed reduction are active");
        evidence.Add((ui.EscapeCount > 0 ? "PASS" : "NOT OBSERVED") + " | Native pointer proximity avoidance; escapes=" + ui.EscapeCount);
        Capture("03_SecondQuestion_MovingYES");
        stage = "NativeSecondNO";
        while (game.CurrentPhase == Phase.FinalDecision) yield return null;
        Check(game.CurrentPhase == Phase.BadEnding && game.FinalQuestion == 2, "Native second NO enters BE");
        Capture("04_SecondNO_BE");
        game.ReturnToTitle(); yield return null;
        // Continue resumes question 2 at the original position, before movement begins again.
        LoadChoice(1); yield return null;
        Check(ui.YesButton.GetComponent<RectTransform>().anchoredPosition == initialYes && ui.MoveCount == 0,
            "Saved second question reenters at the original position");
        while (ui.MovingTime < 1.5f) yield return null;
        Check(EventSystem.current.currentSelectedGameObject == ui.YesButton.gameObject, "Moving YES retains keyboard selection");
        stage = "NativeKeyboardYES";
        while (game.CurrentPhase == Phase.FinalDecision) yield return null;
        Check(game.CurrentPhase == Phase.Epilogue && game.YesCount == 2 && game.FinalQuestion == 2,
            "Native keyboard selects moving YES and enters HE");
        for (int i = 0; i < 6; i++) { ui.YesButton.onClick.Invoke(); ui.NoButton.onClick.Invoke(); }
        Check(game.FinalQuestion == 2 && game.YesCount == 2, "Rapid repeated clicks cannot create a third question or duplicate ending");
        var he = game.GetComponent<HEEndingPlayer>();
        while (he.Elapsed < 7.8f) yield return null;
        Check(he.OwnsUI && !ui.OwnsUI && he.ShotIndex == 0, "Final choice UI stops before HE illustration playback");
        Capture("05_HE_Connection");
        while (!he.Completed) yield return null;
        Check(game.CurrentPhase == Phase.TrueEnding, "Existing three-shot HE completes after full narration");
        while (he.Elapsed < he.Configuration.Duration + 1.2f) yield return null;
        stage = "NativeReturnFromHE"; Capture("06_HE_Title");
        while (game.CurrentPhase == Phase.TrueEnding) yield return null;
        Check(game.CurrentPhase == Phase.Title, "Native HE return button restores main menu");
        yield return null;
        foreach (var resolution in new[] { new Vector2Int(1920,1080), new Vector2Int(1600,900), new Vector2Int(1280,720) })
        {
            stage = "AutomaticResolutionCheck"; SetResolution(resolution.x, resolution.y);
            LoadChoice(1); yield return null;
            float finish = Time.realtimeSinceStartup + 6;
            while (Time.realtimeSinceStartup < finish) { CheckSafe(); yield return null; }
            Check(Screen.width == resolution.x && Screen.height == resolution.y, "Live 16:9 resolution " + resolution);
            Check(ui.GetComponentsInChildren<Button>(true).Length == 2, "Exactly one YES and one NO at " + resolution);
            Check(ui.GetComponentsInChildren<Text>(true).All(t => !t.resizeTextForBestFit && t.transform.localScale == Vector3.one),
                "No deformed or shrinking text at " + resolution);
            Capture("Resolution_" + resolution.x + "x" + resolution.y);
            // Button submit uses the same listener as a controller/keyboard event.
            ExecuteEvents.Execute(ui.NoButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Check(game.CurrentPhase == Phase.BadEnding, "Submit/navigation remains usable at " + resolution);
            game.ReturnToTitle(); yield return null;
        }
        game.Restart(); game.StartGame(); yield return null;
        Check(game.Loop == 1 && game.Day == 1 && !ui.OwnsUI && !he.OwnsUI, "First-run Day1 and ordinary UI unaffected");
        game.ReturnToTitle();
    }
    static void CheckSafe()
    {
        var moving = ui.YesButton.GetComponent<RectTransform>(); var fixedButton = ui.NoButton.GetComponent<RectTransform>();
        Vector2 p = moving.anchoredPosition, half = moving.sizeDelta * .5f;
        Check(p.x - half.x >= ui.CanvasRect.rect.xMin && p.x + half.x <= ui.CanvasRect.rect.xMax
            && p.y - half.y >= ui.CanvasRect.rect.yMin && p.y + half.y <= ui.CanvasRect.rect.yMax, "YES stays in visible Canvas");
        Check(fixedButton.anchoredPosition == new Vector2(270,-120), "NO stays fixed");
        var r = new Rect(p - half, half * 2); var nr = new Rect(fixedButton.anchoredPosition - half, half * 2);
        Check(!r.Overlaps(nr), "YES never overlaps NO");
        Check(r.yMax < ui.QuestionRect.anchoredPosition.y - ui.QuestionRect.rect.height * .5f, "YES avoids question text");
    }
    static void SetResolution(int width, int height)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); var view = EditorWindow.GetWindow(type);
        var selected = type.GetProperty("selectedSizeIndex", flags);
        if (!SessionState.GetBool(Key + ".SizeStored", false))
        { SessionState.SetInt(Key + ".OriginalSize", (int)selected.GetValue(view)); SessionState.SetBool(Key + ".SizeStored", true); }
        var sizes = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = sizes.BaseType.GetProperty("instance", flags).GetValue(null);
        var group = sizes.GetProperty("currentGroup", flags).GetValue(singleton);
        var get = group.GetType().GetMethod("GetGameViewSize", flags);
        int count = (int)group.GetType().GetMethod("GetTotalCount", flags).Invoke(group, null);
        for (int i = 0; i < count; i++)
        {
            var size = get.Invoke(group, new object[] { i }); var st = size.GetType();
            if ((int)st.GetProperty("width", flags).GetValue(size) != width || (int)st.GetProperty("height", flags).GetValue(size) != height) continue;
            selected.SetValue(view, i); view.Focus(); return;
        }
        throw new Exception("Required Game View preset missing.");
    }
    static void Finish(bool success, string message)
    {
        EditorApplication.update -= Tick; SessionState.SetBool(Key,false);
        if (game != null) game.ReturnToTitle();
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE", SessionState.GetString(Key + ".SaveOverride", ""));
        if (SessionState.GetBool(Key + ".SizeStored",false))
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                .SetValue(EditorWindow.GetWindow(type),SessionState.GetInt(Key+".OriginalSize",0));
            SessionState.SetBool(Key+".SizeStored",false);
        }
        File.WriteAllLines(Path.Combine(Output,"Results.txt"),new[]{success?"PASS":"FAIL",message}.Concat(evidence.Distinct()));
        stage = success ? "Completed" : "Failed"; run = null; EditorApplication.isPlaying = false;
        Debug.Log("D-DAY final decision verification " + (success?"PASS":"FAIL") + ": " + message);
    }
}

