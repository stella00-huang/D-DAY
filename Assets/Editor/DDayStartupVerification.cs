using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using DDay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Editor-only startup audit. Never starts or resumes the campaign on its own.
public static class DDayStartupVerification
{
    public const string Output = "C:/Users/stella/.codex/visualizations/2026/10/02/01a0fb0b-5071-7ba2-b32a-20b42e70d0ef/StartupVerification";
    static readonly string[] Keys = { "DDay.TutorialVerification", "DDay.MenuVerification",
        "DDay.AudioVerification", "DDay.FinalDecisionVerification", "DDay.DualEndingVerification",
        "DDayABPlayVerification", "DDayCampaignSmoke.Active" };
    const BindingFlags Hidden = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static IEnumerator run;
    static readonly List<string> checks = new List<string>();
    static DDayGame game;
    static DDayMainMenu menu;
    static string originalOverride, originalSavePath, originalSaveHash, testPrefix;
    static DateTime started;
    [MenuItem("D-DAY/Startup/Inspect Startup Without Playing")]
    public static void Inspect()
    {
        Directory.CreateDirectory(Output);
        var lines = Keys.Select(key => key + "=" + SessionState.GetBool(key, false)).ToList();
        lines.Add("isPlaying=" + EditorApplication.isPlaying);
        lines.Add("activeScene=" + EditorSceneManager.GetActiveScene().path);
        lines.Add("playModeStartScene=" + (EditorSceneManager.playModeStartScene == null
            ? "(active scene)" : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)));
        lines.Add("saveOverride=" + Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE"));
        var callbacks = typeof(EditorApplication).GetField("update", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null) as Delegate;
        if (callbacks != null) lines.AddRange(callbacks.GetInvocationList()
            .Where(d => d.Method.DeclaringType?.Name.StartsWith("DDay") == true)
            .Select(d => "updateCallback=" + d.Method.DeclaringType.Name + "." + d.Method.Name));
        var game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        if (game != null) lines.Add("sceneComponents=" + string.Join(",", game.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().Name)));
        File.WriteAllLines(Path.Combine(Output, "Before.txt"), lines);
        Debug.Log("D-DAY startup audit written: " + Output);
    }
    [MenuItem("D-DAY/Startup/Verify Buttons In Current Play Mode")]
    public static void Verify()
    {
        if (!EditorApplication.isPlaying || run != null) return;
        game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        menu = UnityEngine.Object.FindFirstObjectByType<DDayMainMenu>();
        if (game == null || menu == null || game.CurrentPhase != Phase.Title)
            throw new InvalidOperationException("Start ordinary Play and wait at the main menu first.");
        Directory.CreateDirectory(Output); checks.Clear();
        originalOverride = Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE");
        originalSavePath = (string)typeof(DDayGame).GetProperty("SavePath", Hidden).GetValue(null);
        originalSaveHash = Hash(originalSavePath);
        testPrefix = Path.Combine(Output, Guid.NewGuid().ToString("N"));
        started = DateTime.UtcNow; run = Sequence();
        EditorApplication.update += Tick; EditorApplication.playModeStateChanged += OnExit;
    }
    static string Hash(string path)
    {
        if (!File.Exists(path)) return "(absent)";
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
    }
    static void Check(bool pass, string label)
    { if (!pass) throw new Exception(label); checks.Add("PASS | " + label); }
    static IEnumerable<object> Wait(float seconds)
    { double end = Time.realtimeSinceStartupAsDouble + seconds; while (Time.realtimeSinceStartupAsDouble < end) yield return null; }
    static void Click(Button button)
    { ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current)
        { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler); }
    static void Use(string label, string json = null)
    {
        string path = testPrefix + "_" + label + ".json";
        if (json != null) File.WriteAllText(path, json);
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE", path);
    }
    static IEnumerator Sequence()
    {
        Check(game.CurrentPhase == Phase.Title && game.Day == 0 && menu.Visible, "Ordinary Play waits at the existing main menu / Day 0");
        Check(!Keys.Any(key => SessionState.GetBool(key, false)), "No leftover automatic verification flag");
        Check(game.GetComponent<DDaySmokeRunner>() == null && game.GetComponent<DDay2SliceSmoke>() == null, "No scene smoke component");
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "01_OrdinaryPlay_MainMenu.png"));
        foreach (var t in Wait(.3f)) yield return t;
        foreach (string invalid in new[] { "missing", "malformed", "bounds" })
        {
            Use(invalid, invalid == "missing" ? null : invalid == "malformed" ? "not valid JSON" : "{\"loop\":3,\"day\":99}");
            foreach (var t in Wait(.25f)) yield return t;
            Check(!game.CanContinue && !menu.resume.interactable, invalid + " save cannot enable Continue");
            string path = (string)typeof(DDayGame).GetProperty("SavePath", Hidden).GetValue(null);
            string before = Hash(path); menu.resume.onClick.Invoke();
            foreach (var t in Wait(.9f)) yield return t;
            Check(game.CurrentPhase == Phase.Title && !menu.IsTransitioning && Hash(path) == before, invalid + " save neither resumes nor changes");
        }
        Use("valid", "{\"loop\":1,\"day\":4,\"phase\":\"Layout\",\"tutorial\":{\"done\":true}}");
        foreach (var t in Wait(.3f)) yield return t;
        Check(game.CanContinue && menu.resume.interactable, "Valid saved progress enables Continue");
        foreach (var t in Wait(2f)) yield return t;
        Check(game.CurrentPhase == Phase.Title && game.Day == 0, "A valid save does not auto-resume");
        Click(menu.resume); Check(menu.IsTransitioning && game.CurrentPhase == Phase.Title, "Continue waits for original cover transition");
        foreach (var t in Wait(1f)) yield return t;
        Check(game.Loop == 1 && game.Day == 4 && game.CurrentPhase == Phase.Layout && !menu.Visible, "Continue restores saved Day4 rather than starts Day1");
        game.ReturnToTitle(); Use("newgame"); foreach (var t in Wait(.3f)) yield return t;
        Check(menu.Visible && !menu.resume.interactable, "Returning to main menu retains original menu behavior");
        Click(menu.start); foreach (var t in Wait(1f)) yield return t;
        Check(game.Loop == 1 && game.Day == 1 && game.CurrentPhase == Phase.Layout, "Only Start button enters formal Day1");
        Check(game.GetComponent<DDayTutorialController>().Active, "Existing first-run Day1 tutorial remains active");
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "02_StartButton_Day1.png"));
        foreach (var t in Wait(.3f)) yield return t;
        Check(Hash(originalSavePath) == originalSaveHash, "Production save is byte-for-byte unchanged");
    }
    static void Tick()
    {
        try
        {
            if ((DateTime.UtcNow - started).TotalSeconds > 60) throw new Exception("Startup verification timeout");
            if (!run.MoveNext()) Finish(true, "Real Play Mode rendering and UGUI button event callbacks passed; physical mouse not claimed.");
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }
    static void OnExit(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            Finish(false, "Stopped before verification completed.");
    }
    static void Finish(bool pass, string message)
    {
        EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= OnExit;
        if (EditorApplication.isPlaying && game != null) game.ReturnToTitle();
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE", originalOverride);
        File.WriteAllLines(Path.Combine(Output, "Results.txt"), new[] { pass ? "PASS" : "FAIL", message }.Concat(checks));
        run = null; Debug.Log("D-DAY startup verification " + (pass ? "PASS" : "FAIL"));
    }
}
