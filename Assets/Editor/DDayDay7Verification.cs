using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DDay;
using UnityEditor;
using UnityEngine;

public static partial class DDayABPlayVerification
{
    [MenuItem("D-DAY/Verify Day7 Suspects in Play Mode")]
    public static void RunDay7() => StartVerification(true);

    static void EnterDay7(DDayGame game, int loop)
    {
        typeof(DDayGame).GetProperty("Loop").SetValue(game, loop);
        typeof(DDayGame).GetMethod("BeginDay", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, new object[] { 7 });
        while (game.CurrentPhase == Phase.Narrative) game.AdvanceNarrative();
    }

    static IEnumerator Day7Sequence()
    {
        DDayGame game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        while (game == null || game.Data == null)
        { yield return null; game = UnityEngine.Object.FindFirstObjectByType<DDayGame>(); }
        evidence.Clear();
        Check(Application.dataPath.Replace('\\', '/').EndsWith("聚光灯/Assets"), "Main project Play Mode, not validation checkout");
        Check(Day7Suspects.Ids.SequenceEqual(new[] { "fox", "shiba", "bear", "wolf", "panda", "cat", "boss" }),
            "Exactly seven suspects in 4+3 order, no SISY");
        Check(Day7Suspects.Name("wolf") == "灰狼 ORIN" && Day7Suspects.Name("cat") == "黑猫 METIS"
            && Day7Suspects.Name("boss") == "马 老板", "Correct animal/name mapping");
        foreach (string id in Day7Suspects.Ids)
        {
            Sprite normal = Day7Suspects.Portrait(id, false), surprised = Day7Suspects.Portrait(id, true);
            Check(normal != null && surprised != null && normal != surprised, "Two distinct Sprites: " + id);
            Check(normal.rect.size == new Vector2(320,320) && normal.rect.size == surprised.rect.size,
                "Identical uncompressed portrait dimensions: " + id);
            foreach (Sprite sprite in new[] { normal, surprised })
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                var tex = new Texture2D(2,2); tex.LoadImage(File.ReadAllBytes(path));
                Check(tex.GetPixel(0,0).a == 0 && tex.GetPixel(160,160).a == 1
                    && tex.GetPixel(160,0).a == 0, "Transparent circular mask: " + sprite.name);
                UnityEngine.Object.Destroy(tex);
            }
            Check(!File.ReadAllBytes(AssetDatabase.GetAssetPath(normal)).SequenceEqual(
                File.ReadAllBytes(AssetDatabase.GetAssetPath(surprised))), "Real expression pixel difference: " + id);
        }
        game.StartGame();
        EnterDay7(game,1);
        Check(!game.CanConfirmDay7Suspect && game.CurrentPhase == Phase.Suspects, "Initial selection empty and confirmation disabled");
        game.ConfirmDay7Suspect(); game.SelectDay7Suspect("rabbit");
        Check(game.CurrentPhase == Phase.Suspects && game.Day7SelectedSuspectId == null, "Empty confirmation / SISY rejected");
        SetResolution(1280,720);
        yield return Capture("Day7_Initial");
        game.SaveGame();
        string savePath = Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE");
        string initialSave = File.ReadAllText(savePath);
        foreach (string id in Day7Suspects.Ids)
        {
            File.WriteAllText(savePath,initialSave); game.ContinueGame();
            game.SelectDay7Suspect(id);
            Check(game.Day7SelectedSuspectId == id && game.CanConfirmDay7Suspect
                && game.SuspectId == null && game.CurrentPhase == Phase.Suspects, "Selection does not begin ending: " + id);
            int index = Array.IndexOf(Day7Suspects.Ids,id);
            Check(Mathf.Abs(Day7Suspects.PortraitRect(index,true).width / Day7Suspects.Diameter - 1.05f) < .0001f,
                "Selected portrait 1.05 scale: " + id);
            game.ReturnToTitle(); game.ContinueGame();
            Check(game.Day7SelectedSuspectId == id && game.SuspectId == null && game.CanConfirmDay7Suspect,
                "Pending selection survives Continue: " + id);
            game.SelectDay7Suspect(id);
            Check(game.Day7SelectedSuspectId == null && !game.CanConfirmDay7Suspect, "Click selected cancels: " + id);
            game.SelectDay7Suspect(Day7Suspects.Ids[(index+1)%7]); game.SelectDay7Suspect(id);
            Check(game.Day7SelectedSuspectId == id, "Switch keeps exactly one selection: " + id);
            if(id=="fox" || id=="boss") yield return Capture("Day7_Selected_"+id);
            game.ConfirmDay7Suspect();
            Check(game.SuspectId == id && game.CurrentPhase == Phase.Narrative && game.NarrativeCount==2
                && game.NarrativeLine.Contains("没有证据表明存在唯一的直接凶手"), "Existing non-unique culprit ending: " + id);
            game.AdvanceNarrative();
            game.ReturnToTitle(); game.ContinueGame();
            Check(game.SuspectId==id && game.NarrativeIndex==1 && game.CurrentPhase==Phase.Narrative,
                "Confirmed ID and ending position survive Continue: " + id);
            while(game.CurrentPhase==Phase.Narrative) {game.AdvanceNarrative();yield return null;}
            Check(game.CurrentPhase==Phase.Replay, "All suspects reach the same original causal replay: " + id);
        }
        while(game.CurrentPhase==Phase.Replay || game.CurrentPhase==Phase.Narrative)
        {if(game.CurrentPhase==Phase.Replay)game.AdvanceReplay();else game.AdvanceNarrative();yield return null;}
        Check(game.Loop==2 && game.Day==1, "Original ending still unlocks loop2");
        // Missing optional fields in older saves must deserialize without blocking Day7.
        string legacy = Regex.Replace(initialSave, ",\"(?:suspectId|selectedSuspectId|day7DialogueIndex)\":(?:null|\"[^\"]*\"|[0-9]+)", "");
        File.WriteAllText(savePath,legacy); game.ContinueGame();
        Check(game.CurrentPhase==Phase.Suspects && game.Day7SelectedSuspectId==null, "Pre-change Day7 save compatible");
        EnterDay7(game,2); game.SaveGame(); string loop2Save=File.ReadAllText(savePath);
        foreach(var votes in new[]{new[]{true,true},new[]{true,false},new[]{false,true},new[]{false,false}})
        {
            File.WriteAllText(savePath,loop2Save); game.ContinueGame();
            game.SelectDay7Suspect("boss");game.ConfirmDay7Suspect();
            Check(game.CurrentPhase==Phase.FinalDecision && !game.CanConfirmDay7Suspect, "Loop2 excludes suspect UI");
            game.ChooseFinal(votes[0]);
            Check(game.FinalQuestion==1 && game.CurrentPhase==(votes[0]?Phase.FinalDecision:Phase.BadEnding),
                "First NO goes to BE; first YES retains second APIS question");
            game.ChooseFinal(votes[1]);
            Check(game.CurrentPhase==(votes[0]&&votes[1]?Phase.Epilogue:Phase.BadEnding),
                "Loop2 unchanged votes " + votes[0]+"/"+votes[1]);
        }
        EnterDay7(game,1); SetResolution(1280,720);
        yield return Capture("Day7_MouseReady");
        var observed = new HashSet<string>();
        string previous=null; bool cancelled=false,switched=false;
        evidence.Add("MOUSE_TEST_READY | Native Game View clicks required for all seven portraits, switch, cancel and confirm");
        while(game.CurrentPhase==Phase.Suspects)
        {
            string id=game.Day7SelectedSuspectId;
            if(id!=previous)
            {
                if(id!=null) observed.Add(id);
                if(previous!=null && id==null) cancelled=true;
                if(previous!=null && id!=null) switched=true;
                evidence.Add("NATIVE_MOUSE_STATE | "+(previous??"none")+" -> "+(id??"none"));
                ScreenCapture.CaptureScreenshot(Path.Combine(Output,"Mouse_"+(id??"Cancelled")+".png"));
                previous=id;
            }
            File.WriteAllText(Path.Combine(Output,"LiveMouseState.txt"),
                "READY | phase="+game.CurrentPhase+" | selected="+(id??"none")+" | confirm="+game.CanConfirmDay7Suspect
                +" | observed="+string.Join(",",observed)+" | cancelled="+cancelled+" | switched="+switched);
            yield return null;
        }
        Check(observed.SetEquals(Day7Suspects.Ids) && cancelled && switched, "Native mouse selected all seven, switched and cancelled");
        Check(game.CurrentPhase==Phase.Narrative && Day7Suspects.Contains(game.SuspectId), "Native confirmation enters existing ending");
        File.WriteAllText(Path.Combine(Output,"LiveMouseState.txt"),"CONFIRMED | "+game.SuspectId);
        yield return Capture("Day7_MouseConfirmed");
        Check(!evidence.Any(x=>x.StartsWith("TEXT_LAYOUT_FAIL")),"All Day7 three-resolution text containers fit");
    }
}
