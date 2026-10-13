#if UNITY_EDITOR
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
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

// Continuous current Game framebuffer capture, never a still-image slideshow.
[InitializeOnLoad]
public static class DDayPVProduction
{
    const string Key = "DDay.PVProduction";
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
    static string Root => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Exports", "PV");
    static string Take => SessionState.GetString(Key + ".Take", "");
    static string Encoder => Path.Combine(Root, "Tools", "ffmpeg-9.0.2-essentials_build", "bin", "ffmpeg.exe");
    static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    static readonly List<string> evidence = new List<string>();
    static DDayGame game;
    static EditorWindow view;
    static Process encoder;
    static int frames;
    static double nextFrame, clipStarted, deadline;
    static string clip;
    static bool pausedCapture;
    static int lastScheduledFrame = -1;

    static DDayPVProduction()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(Key, false)) Finish("INTERRUPTED");
        };
    }

    [MenuItem("D-DAY/PV Recording/Produce Real Gameplay Take")]
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play and wait for compilation first.");
        if (!File.Exists(Encoder)) throw new FileNotFoundException("Portable FFmpeg missing", Encoder);
        string take = Path.Combine(Root, "Raw", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(take); SessionState.SetString(Key + ".Take", take);
        SessionState.SetBool(Key, true);
        typeof(DDayPVRecording).GetMethod("StartSession", Hidden).Invoke(null, null);
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }

    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) return;
            if (game == null)
            {
                game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
                if (game == null || game.Data == null) return;
                view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                view.Focus(); deadline = EditorApplication.timeSinceStartup + 900;
                stack.Push(Sequence());
            }
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Production timeout");
            if (Time.frameCount != lastScheduledFrame)
            { lastScheduledFrame = Time.frameCount; game.StartCoroutine(DDayPVFramePump.AfterRender(Frame)); }
            if (stack.Peek().MoveNext())
            { if (stack.Peek().Current is IEnumerator child) stack.Push(child); }
            else { stack.Pop(); if (stack.Count == 0) Finish("COMPLETE"); }
        }
        catch (Exception e) { Finish("FAIL: " + e); }
    }

    static void Frame()
    {
        if (!SessionState.GetBool(Key, false)) return;
        try { if (!string.IsNullOrEmpty(clip) && !pausedCapture && EditorApplication.timeSinceStartup >= nextFrame) Capture(); }
        catch (Exception e) { Finish("FAIL: " + e); }
    }

    static IEnumerator Wait(float seconds)
    { double until = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < until) yield return null; }
    static void Check(bool ok, string detail) { if (!ok) throw new InvalidOperationException(detail); evidence.Add("PASS | " + detail); }
    static void Click(Button button)
    { ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler); }

    static void Begin(string id)
    {
        End(); evidence.Add("VIEWPORT | " + id + " | reported=" + Screen.width + "x" + Screen.height);
        clip = id; frames = 0; clipStarted = EditorApplication.timeSinceStartup; nextFrame = clipStarted;
        pausedCapture = false;
        evidence.Add("BEGIN | " + id + " | Day=" + game.Day + " | Phase=" + game.CurrentPhase);
    }

    static void Capture()
    {
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            CheckDimensions(tex);
            if (encoder == null)
            {
                string format = tex.format == TextureFormat.RGB24 ? "rgb24" : tex.format == TextureFormat.RGBA32 ? "rgba" : null;
                if (format == null) throw new InvalidOperationException("Unsupported framebuffer " + tex.format);
                string output = Path.Combine(Take, clip + ".mp4");
                encoder = new Process { StartInfo = new ProcessStartInfo(Encoder,
                    "-hide_banner -loglevel error -n -f rawvideo -pixel_format " + format + " -video_size 1920x1080 -framerate 30 -i pipe:0 -an -vf vflip -c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p -movflags +faststart \"" + output + "\"")
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true } };
                encoder.ErrorDataReceived += (_, e) => { if (e.Data != null) File.AppendAllText(Path.Combine(Take, "Encoder.log"), e.Data + "\n"); };
                encoder.Start(); encoder.BeginErrorReadLine();
            }
            byte[] data = tex.GetRawTextureData<byte>().ToArray();
            encoder.StandardInput.BaseStream.Write(data, 0, data.Length); frames++;
            nextFrame += 1.0 / 30;
        }
        finally { UnityEngine.Object.DestroyImmediate(tex); }
    }
    static void CheckDimensions(Texture2D tex)
    {
        if (tex == null) throw new InvalidOperationException("Unity returned no completed framebuffer");
        if (tex.width != 1920 || tex.height != 1080)
            throw new InvalidOperationException("Framebuffer size changed to " + tex.width + "x" + tex.height);
    }

    static void End()
    {
        if (encoder == null) return;
        encoder.StandardInput.Close();
        if (!encoder.WaitForExit(15000)) throw new TimeoutException("FFmpeg did not finish");
        int code = encoder.ExitCode; encoder.Dispose(); encoder = null;
        evidence.Add("END | " + clip + " | frames=" + frames + " | encodedSeconds=" + (frames / 30f).ToString("F3")
            + " | wallSeconds=" + (EditorApplication.timeSinceStartup - clipStarted).ToString("F3") + " | exit=" + code);
        if (code != 0 || frames < 30) throw new InvalidOperationException("Capture failed: " + clip);
        File.WriteAllLines(Path.Combine(Take, "CaptureReport.txt"), evidence); clip = null;
    }

    static IEnumerator Drain(float delay = .18f)
    {
        int guard = 0;
        while (game.CurrentPhase == Phase.Narrative)
        {
            game.GetComponent<DDayUI>().AdvanceDialogueInput(); yield return Wait(delay);
            if (++guard > 250) throw new InvalidOperationException("Narrative did not finish");
        }
    }

    static IEnumerator Drag(int animal, int target)
    {
        var objects = (GameObject[])typeof(DDayGame).GetField("animalObjects", Hidden).GetValue(game);
        var renderers = (SpriteRenderer[])typeof(DDayGame).GetField("animalRenderers", Hidden).GetValue(game);
        Vector3 from = objects[animal].transform.position;
        Vector3 to = game.TutorialSeatWorldPoint(target);
        typeof(DDayGame).GetProperty("DragAnimal", Hidden).SetValue(game, animal);
        typeof(DDayGame).GetField("draggedStartPosition", Hidden).SetValue(game, from);
        renderers[animal].sortingOrder = 40;
        typeof(DDayGame).GetMethod("RefreshDeskProps", Hidden).Invoke(game, null);
        typeof(DDayGame).GetMethod("MarkSeatDragMoved", Hidden).Invoke(game, null);
        Check(game.DragAnimal == animal, "Editor automation began original drag state for " + SeatRules.Ids[animal]);
        for (int i = 1; i <= 30; i++)
        {
            float t = i / 30f; t = t * t * (3 - 2 * t);
            objects[animal].transform.position = Vector3.Lerp(from, to, t);
            yield return Wait(.033f);
        }
        typeof(DDayGame).GetMethod("FinishSeatDrag", Hidden).Invoke(game, new object[] { animal, target, true });
        yield return Wait(.25f);
        Check(game.Seats[animal] == target && game.DragAnimal == -1,
            "Original drop logic placed " + SeatRules.Ids[animal] + " at " + SeatRules.SeatCode(target));
    }

    static IEnumerator Sequence()
    {
        while (Screen.width != 1920 || Screen.height != 1080) yield return null;
        evidence.Clear(); yield return Wait(2);
        Begin("01_MenuFull"); yield return Wait(12); End();
        Begin("02_StartTransition"); Click(UnityEngine.Object.FindFirstObjectByType<DDayMainMenu>().start);
        yield return Wait(2); End();
        Check(game.CurrentPhase == Phase.Layout && game.Day == 1, "Original Day1 entered");
        game.GetComponent<DDayTutorialController>().Skip(); yield return Wait(3);
        Begin("03_D1OfficeWide"); yield return Wait(12); End();
        typeof(DDayPVRecording).GetMethod("Bookmark", Hidden).Invoke(null, null);
        Begin("04_D1SeatDrag"); yield return Wait(1);
        yield return Drag(1, 0); yield return Wait(1); yield return Drag(3, 1);
        yield return Wait(1); yield return Drag(6, 4); yield return Wait(1); yield return Drag(5, 2);
        yield return Wait(3); End();
        Check(game.SeatsReady, "Actual rules complete");
        Begin("05_D1RulesReady"); yield return Wait(10); End();
        Begin("06_D1Assignment"); game.ToggleTasks(); yield return Wait(2); game.AssignTask("report", 0);
        yield return Wait(4); yield return Drain(.8f); if (game.TasksOpen) game.ToggleTasks();
        yield return Wait(2); End();
        typeof(DDayPVRecording).GetMethod("Bookmark", Hidden).Invoke(null, null);
        game.StartWork();
        while (game.CurrentPagePanel == null) yield return null;
        Begin("07_D1_1815Storm"); yield return Wait(12); End();
        Begin("08_D1Collapse");
        while (game.CurrentPagePanel != null && game.CurrentPagePanel.id != "Day1_P03_collapse")
        { game.GetComponent<DDayUI>().AdvanceDialogueInput(); yield return Wait(1.3f); }
        Check(game.CurrentPagePanel?.id == "Day1_P03_collapse", "Original collapse reached");
        yield return Wait(8); End(); yield return Drain(.25f);
        Check(game.Day == 2 && game.Seats[0] == -1, "Original Day2 and empty SISY desk");
        Begin("09_D2EmptyDesk"); yield return Wait(10); End();
        typeof(DDayPVRecording).GetMethod("Bookmark", Hidden).Invoke(null, null);
        Begin("10_D2Investigation");
        Click(game.transform.Find("D-DAY Canvas/Explore wolf").GetComponent<Button>());
        Check(game.CurrentPhase == Phase.Narrative, "Actual ORIN exploration opened");
        yield return Wait(3);
        while (game.CurrentPhase == Phase.Narrative)
        { game.GetComponent<DDayUI>().AdvanceDialogueInput(); yield return Wait(2.5f); }
        yield return Wait(1); End();
        Check(game.Records.Any(r => r.id == "A2_R08"), "Actual inquiry record acquired");
        Begin("11_RecordUpdate"); game.ToggleBoard(1); yield return Wait(12); game.ToggleBoard(); End();
        int[] solution = { -1, 0, 5, 7, 2, 1, 4 };
        for (int i = 1; i < 7; i++) game.TryPlaceAnimal(i, solution[i]);
        Check(game.SeatsReady, "Day2 normal rule solution"); game.ToggleTasks();
        foreach (var task in game.Tasks.Cards) { game.AssignTask(task.id, 6); yield return Drain(); }
        if (game.TasksOpen) game.ToggleTasks(); game.StartWork();
        while (game.Day == 2)
        { if (game.CurrentPhase == Phase.Narrative) game.GetComponent<DDayUI>().AdvanceDialogueInput(); yield return Wait(.2f); }
        Check(game.Day == 3 && game.CurrentPhase == Phase.Layout, "Original flow reached Day3");
        typeof(DDayPVRecording).GetMethod("Bookmark", Hidden).Invoke(null, null);
        Begin("12_LaterSeating"); yield return Wait(2); yield return Drag(1, 3); yield return Wait(1); yield return Drag(3, 7);
        yield return Wait(6); End();
        game.ReturnToTitle(); yield return Wait(2);
        Begin("13_MenuClosing"); yield return Wait(10); End();
    }

    static void Finish(string result)
    {
        try { End(); } catch (Exception e) { result += " | " + e.Message; }
        evidence.Add(result); Directory.CreateDirectory(Take);
        File.WriteAllLines(Path.Combine(Take, "CaptureReport.txt"), evidence);
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick; stack.Clear();
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        Debug.Log("D-DAY PV production: " + result + " | " + Take);
    }
}
#endif
