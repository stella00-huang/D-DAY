#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using DDay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Only real campaign saves are bookmarked. No dates, clues or story flags are injected.
[InitializeOnLoad]
public sealed class DDayPVRecording : EditorWindow
{
    const string Key = "DDay.PVRecording.";
    const string Variable = "DDAY_SAVE_OVERRIDE";
    const string Scene = "Assets/Scenes/TodayWhereToSit.unity";
    static readonly string[] Preferences = { "DDay.BgmPercent", "DDay.SfxPercent", "DDay.Fullscreen", "DDay.TextSpeed" };
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static string Root => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "PVRecording");
    static bool Active => SessionState.GetBool(Key + "Active", false);
    static string Session => SessionState.GetString(Key + "Session", "");
    static string AutoSave => Path.Combine(Session, "AutoSave.json");
    string message = "从独立会话开始，随后使用原开始游戏按钮。";

    static DDayPVRecording()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
        EditorApplication.quitting += Restore;
        if (SessionState.GetBool(Key + "Verify", false)) EditorApplication.update += VerifyTick;
        if (Active && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Restore;
    }

    [MenuItem("D-DAY/PV Recording/Open Recording Assistant")]
    public static void Open() => GetWindow<DDayPVRecording>("D-DAY 录制准备");

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Editor 专用；自动/手动存档均隔离到 Docs/PVRecording/Sessions。退出 Play 自动恢复环境、设置和 Game 视图尺寸。", MessageType.Info);
        EditorGUILayout.LabelField("目标画面", "1920 × 1080 / 16:9");
        EditorGUILayout.LabelField("会话", Active ? Session : "未启动");
        var game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        if (EditorApplication.isPlaying && game != null)
            EditorGUILayout.LabelField("真实状态", "周目 " + game.Loop + " / Day " + game.Day + " / " + game.CurrentPhase);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || Active))
            if (GUILayout.Button("开始新的隔离录制会话（1080p）")) RunAction(StartSession);
        using (new EditorGUI.DisabledScope(!Active || !EditorApplication.isPlaying))
        {
            if (GUILayout.Button("保存当前排座阶段检查点")) RunAction(() => Bookmark());
            if (GUILayout.Button("恢复已实际到达的检查点")) RunAction(() =>
            {
                string path = EditorUtility.OpenFilePanel("选择录制检查点", Path.Combine(Root, "Sessions"), "json");
                if (!string.IsNullOrEmpty(path)) LoadBookmark(path);
            });
            if (GUILayout.Button("结束会话（退出 Play）")) EditorApplication.isPlaying = false;
        }
        if (GUILayout.Button("打开素材与会话目录")) { Directory.CreateDirectory(Root); EditorUtility.RevealInFinder(Root); }
        EditorGUILayout.HelpBox(message, MessageType.None);
        EditorGUILayout.HelpBox("检查点仅接受一周目 Day 1–5 的排座阶段。Day 1 死亡事件从工作前检查点重放。录制时把此窗口放在 Game 视图之外。", MessageType.None);
    }

    void RunAction(Action action)
    {
        try { action(); message = "完成。检查点目录：" + Session; }
        catch (Exception e) { message = e.Message; Debug.LogWarning("D-DAY PV: " + e.Message); }
    }

    static void StartSession()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Active || EditorApplication.isCompiling)
            throw new InvalidOperationException("先退出 Play，等待编译完成。");
        if (EditorSceneManager.GetActiveScene().path != Scene || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("请打开并保存原 TodayWhereToSit 场景；辅助工具不会保存或改写场景。");
        if (EditorSceneManager.playModeStartScene != null && AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) != Scene)
            throw new InvalidOperationException("Play Mode Start Scene 指向其他场景，请先在编辑器中改回原场景。");
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Variable)))
            throw new InvalidOperationException("其他验收/隔离存档正在生效，请先结束那个会话。");
        foreach (string test in new[] { "DDay.TutorialVerification", "DDay.MenuVerification", "DDay.AudioVerification",
            "DDay.FinalDecisionVerification", "DDay.DualEndingVerification", "DDayABPlayVerification", "DDayCampaignSmoke.Active" })
            if (SessionState.GetBool(test, false)) throw new InvalidOperationException("请先结束现有自动验收：" + test);
        string session = Path.Combine(Root, "Sessions", DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(session);
        SessionState.SetString(Key + "Session", session);
        SessionState.SetString(Key + "OriginalOverride", Environment.GetEnvironmentVariable(Variable) ?? "");
        foreach (string pref in Preferences)
        {
            SessionState.SetBool(Key + pref + ".Exists", PlayerPrefs.HasKey(pref));
            SessionState.SetInt(Key + pref, PlayerPrefs.GetInt(pref));
        }
        SessionState.SetString(Key + "FormalSaves", FormalSaveFingerprint());
        SessionState.SetBool(Key + "Active", true);
        Environment.SetEnvironmentVariable(Variable, AutoSave);
        try { Set1080(); EditorApplication.isPlaying = true; }
        catch { Restore(); throw; }
    }

    static DDayGame RequireSession()
    {
        if (!Active || !EditorApplication.isPlaying || Environment.GetEnvironmentVariable(Variable) != AutoSave)
            throw new InvalidOperationException("请先开始隔离录制会话；存档路径不匹配时拒绝操作。");
        var game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        if (game == null || game.Data == null) throw new InvalidOperationException("原游戏尚未就绪。");
        return game;
    }

    static bool SafeLayout(DDayGame game) => game.Loop == 1 && game.Day >= 1 && game.Day <= 5
        && game.CurrentPhase == Phase.Layout && game.DragAnimal < 0
        && !game.BoardOpen && !game.TasksOpen && !game.SaveSlotsOpen && !game.GetComponent<DDayUI>().OfficeSettingsOpen;

    static string Bookmark()
    {
        var game = RequireSession();
        if (!SafeLayout(game)) throw new InvalidOperationException("先返回一周目 Day 1–5 排座画面并关闭弹窗、停止拖拽。");
        game.SaveGame();
        if (!File.Exists(AutoSave)) throw new IOException("隔离自动存档没有生成。");
        string path = Path.Combine(Session, "Checkpoint_Day" + game.Day + "_" + Guid.NewGuid().ToString("N") + ".json");
        File.Copy(AutoSave, path, false);
        Debug.Log("D-DAY PV checkpoint: " + path);
        return path;
    }

    [Serializable] sealed class BookmarkHeader { public int loop, day; public string phase; }

    static void LoadBookmark(string path)
    {
        var game = RequireSession();
        if (game.CurrentPhase != Phase.Title && !SafeLayout(game))
            throw new InvalidOperationException("只在主菜单或关闭弹窗后的排座阶段恢复；剧情中请先正常播放完。");
        string full = Path.GetFullPath(path);
        string allowed = Path.GetFullPath(Path.Combine(Root, "Sessions")) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(full).StartsWith("Checkpoint_Day", StringComparison.Ordinal))
            throw new InvalidOperationException("仅可加载本工具生成的录制检查点。");
        string json = File.ReadAllText(full);
        var header = JsonUtility.FromJson<BookmarkHeader>(json);
        if (header == null || header.loop != 1 || header.day < 1 || header.day > 5 || header.phase != "Layout")
            throw new InvalidOperationException("检查点不是支持恢复的真实排座状态。");
        File.WriteAllText(AutoSave, json);
        game.ContinueGame();
        if (game.Loop != header.loop || game.Day != header.day || game.CurrentPhase != Phase.Layout)
            throw new InvalidOperationException("原游戏恢复失败，请查看 Console。");
    }

    static void OnPlayState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        Restore();
        if (SessionState.GetBool(Key + "Verify", false))
        {
            try
            {
                Check(SessionState.GetInt(Key + "VerifyStage", 0) == 3, "Play checks completed");
                Check(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Variable)), "Save override restored");
                foreach (string pref in Preferences)
                    Check(PlayerPrefs.HasKey(pref) == SessionState.GetBool(Key + pref + ".Exists", false)
                        && (!PlayerPrefs.HasKey(pref) || PlayerPrefs.GetInt(pref) == SessionState.GetInt(Key + pref, 0)), "Preference restored: " + pref);
                Check(FormalSaveFingerprint() == SessionState.GetString(Key + "FormalSaves", ""), "Formal saves unchanged after exit");
                VerificationResult("PASS: Unity Play Mode, 1080p, actual menu callback, real move, checkpoint restore, path rejection, preferences and formal-save protection. Native mouse input and video recording were not tested.");
            }
            catch (Exception e) { VerificationResult("FAIL: " + e); }
        }
    }

    [MenuItem("D-DAY/PV Recording/Verify Isolated Session")]
    public static void Verify()
    {
        StartSession();
        SessionState.SetBool(Key + "Verify", true);
        SessionState.SetInt(Key + "VerifyStage", 0);
        SessionState.SetFloat(Key + "VerifyDeadline", (float)EditorApplication.timeSinceStartup + 90);
        EditorApplication.update -= VerifyTick;
        EditorApplication.update += VerifyTick;
    }

    static void Check(bool pass, string text) { if (!pass) throw new InvalidOperationException(text); }

    static void VerifyTick()
    {
        if (!SessionState.GetBool(Key + "Verify", false)) return;
        try
        {
            Check(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "VerifyDeadline", 0), "Verification timeout");
            if (!EditorApplication.isPlaying) return;
            var game = RequireSession();
            int stage = SessionState.GetInt(Key + "VerifyStage", 0);
            if (stage == 0)
            {
                if (Screen.width != 1920 || Screen.height != 1080) return;
                Check(game.CurrentPhase == Phase.Title, "Normal main menu");
                ScreenCapture.CaptureScreenshot(Path.Combine(Session, "Verification_Menu_1920x1080.png"));
                UnityEngine.Object.FindFirstObjectByType<DDayMainMenu>().start.onClick.Invoke();
                SessionState.SetInt(Key + "VerifyStage", 1);
            }
            else if (stage == 1 && game.CurrentPhase == Phase.Layout)
            {
                Check(game.Day == 1 && game.Loop == 1, "Original Day1 entered");
                game.GetComponent<DDayTutorialController>().Skip();
                int[] seats = (int[])game.Seats.Clone();
                string checkpoint = Bookmark();
                Check(game.TryPlaceAnimal(1, 0) && game.Seats[1] == 0, "Original legal seat move");
                Check(File.Exists(AutoSave), "Isolated autosave created");
                Check(game.SaveManualSlot(15, false), "Isolated manual slot created");
                Check(File.Exists(Path.Combine(Session, "DDayManualSlot15.json")), "Manual save in session folder");
                LoadBookmark(checkpoint);
                Check(game.Seats.SequenceEqual(seats) && game.Records.Count == 0, "Actual checkpoint restored without synthetic clues");
                bool rejected = false;
                try { LoadBookmark(Path.Combine(Application.persistentDataPath, "DDayCampaign.json")); }
                catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "Formal save path rejected");
                DDaySettings.SetBgmPercent(DDaySettings.BgmPercent == 17 ? 18 : 17);
                Check(FormalSaveFingerprint() == SessionState.GetString(Key + "FormalSaves", ""), "Formal saves unchanged during Play");
                SessionState.SetInt(Key + "VerifyStage", 2);
                SessionState.SetFloat(Key + "CaptureAfter", (float)EditorApplication.timeSinceStartup + 1);
            }
            else if (stage == 2 && EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "CaptureAfter", 0))
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Session, "Verification_Office_1920x1080.png"));
                SessionState.SetInt(Key + "VerifyStage", 3);
                SessionState.SetFloat(Key + "CaptureAfter", (float)EditorApplication.timeSinceStartup + 1);
            }
            else if (stage == 3 && EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "CaptureAfter", 0))
                EditorApplication.isPlaying = false;
        }
        catch (Exception e)
        {
            VerificationResult("FAIL: " + e);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            else Restore();
        }
    }

    static void VerificationResult(string result)
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root, "Verification.txt"), result + "\nSession: " + Session);
        SessionState.SetBool(Key + "Verify", false);
        EditorApplication.update -= VerifyTick;
        Debug.Log("D-DAY PV: " + result);
    }

    static string FormalSaveFingerprint()
    {
        if (!Directory.Exists(Application.persistentDataPath)) return "";
        using (var sha = SHA256.Create())
            return string.Join("\n", Directory.GetFiles(Application.persistentDataPath, "DDay*.json*")
                .OrderBy(p => p, StringComparer.Ordinal).Select(p => Path.GetFileName(p) + "="
                    + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)))));
    }

    static void Restore()
    {
        if (!Active) return;
        try
        {
            Environment.SetEnvironmentVariable(Variable, SessionState.GetString(Key + "OriginalOverride", ""));
            foreach (string pref in Preferences)
                if (SessionState.GetBool(Key + pref + ".Exists", false)) PlayerPrefs.SetInt(pref, SessionState.GetInt(Key + pref, 0));
                else PlayerPrefs.DeleteKey(pref);
            PlayerPrefs.Save();
            var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            if (SessionState.GetBool(Key + "SizeStored", false))
                view.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(view, SessionState.GetInt(Key + "OriginalSize", 0));
            bool intact = FormalSaveFingerprint() == SessionState.GetString(Key + "FormalSaves", "");
            File.WriteAllText(Path.Combine(Session, "SessionResult.txt"), "Formal campaign/manual saves unchanged: " + intact
                + "\nSave override and four preferences restored.\nNo video was recorded by this helper.");
            if (!intact) Debug.LogError("D-DAY PV: 正式存档指纹发生变化，请检查是否同时运行了其他验收工具。");
        }
        finally { SessionState.SetBool(Key + "Active", false); SessionState.SetBool(Key + "SizeStored", false); }
    }

    static void Set1080()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var viewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        var selected = viewType.GetProperty("selectedSizeIndex", Flags);
        SessionState.SetInt(Key + "OriginalSize", (int)selected.GetValue(view));
        SessionState.SetBool(Key + "SizeStored", true);
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = sizesType.BaseType.GetProperty("instance", Flags).GetValue(null);
        var group = sizesType.GetProperty("currentGroup", Flags).GetValue(singleton);
        int count = (int)group.GetType().GetMethod("GetTotalCount", Flags).Invoke(group, null);
        int index = -1;
        for (int i = 0; i < count; i++)
        {
            var size = group.GetType().GetMethod("GetGameViewSize", Flags).Invoke(group, new object[] { i });
            if ((int)size.GetType().GetProperty("width", Flags).GetValue(size) == 1920
                && (int)size.GetType().GetProperty("height", Flags).GetValue(size) == 1080
                && size.GetType().GetProperty("sizeType", Flags).GetValue(size).ToString() == "FixedResolution") { index = i; break; }
        }
        if (index < 0)
        {
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var enumType = assembly.GetType("UnityEditor.GameViewSizeType");
            var size = sizeType.GetConstructor(Flags, null, new[] { enumType, typeof(int), typeof(int), typeof(string) }, null)
                .Invoke(new object[] { Enum.Parse(enumType, "FixedResolution"), 1920, 1080, "D-DAY PV 1080p" });
            group.GetType().GetMethod("AddCustomSize", Flags).Invoke(group, new[] { size }); index = count;
        }
        selected.SetValue(view, index); view.Repaint();
    }
}
#endif
