using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DDay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DDayAudioVerification
{
    const string Key = "DDay.AudioVerification";
    static string Output => Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Docs/AudioIntegration");
    static string Save => Path.Combine(Output,"TestAutoSave.json");
    static IEnumerator sequence;
    static readonly List<string> evidence = new List<string>();
    static DDayGame game;
    static DDayAudioManager audio;
    static DateTime began;
    static string stage;
    static double nextStatus;
    static DDayAudioVerification() { if (SessionState.GetBool(Key,false)) EditorApplication.update += Tick; }
    [MenuItem("D-DAY/Audio/Verify Audio in Play Mode")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before verification.");
        Directory.CreateDirectory(Output); evidence.Clear(); sequence=null;
        SessionState.SetString(Key+".OldOverride",Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE")??"");
        foreach(string pref in new[]{"DDay.BgmPercent","DDay.SfxPercent"})
        {
            SessionState.SetBool(Key+pref+".Exists",PlayerPrefs.HasKey(pref));
            SessionState.SetInt(Key+pref,PlayerPrefs.GetInt(pref,0));
        }
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE",Save);
        SessionState.SetBool(Key,true); EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            if(sequence==null){sequence=Run();began=DateTime.UtcNow;}
            if((DateTime.UtcNow-began).TotalSeconds>240)throw new Exception("Audio test timeout");
            if(!sequence.MoveNext()){Finish(true,"Runtime routing, fades, loop blending, one-shot endings, settings and SFX callbacks verified. Physical listening/native mouse not claimed.");return;}
            if(Time.realtimeSinceStartupAsDouble>nextStatus)
            {nextStatus=Time.realtimeSinceStartupAsDouble+2;File.WriteAllText(Path.Combine(Output,"LiveStatus.txt"),stage??"Starting");}
        }
        catch(Exception e){Finish(false,e.ToString());}
    }
    static IEnumerable<object> Wait(float seconds)
    {double until=Time.realtimeSinceStartupAsDouble+seconds;while(Time.realtimeSinceStartupAsDouble<until)yield return null;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);evidence.Add("PASS | "+message);}
    static void Load(int loop,int day,Phase phase)
    {
        File.WriteAllText(Save,"{\"loop\":"+loop+",\"day\":"+day+",\"phase\":\""+phase+"\",\"sisyAlive\":"
            +(loop==2?"true":"false")+",\"seats\":["+(loop==1&&day>1?"-1":"6")+",0,1,2,3,4,5]}");
        game.ContinueGame();
    }
    static void Call(string name,params object[] args)=>typeof(DDayGame).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
    static IEnumerator Run()
    {
        do{yield return null;game=UnityEngine.Object.FindFirstObjectByType<DDayGame>();}while(game==null||game.Data==null);
        audio=game.GetComponent<DDayAudioManager>();
        Check(audio!=null&&UnityEngine.Object.FindObjectsByType<DDayAudioManager>(FindObjectsSortMode.None).Length==1,"Exactly one campaign audio manager");
        Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"Exactly one enabled AudioListener");
        string[] names={"main_menu","day1_2","day3_4","day5_6","day_7","he","be"};
        foreach(var entry in audio.Configuration.music)
        {
            Check(entry.clip!=null&&entry.clip.length>0&&entry.clip.name==names[(int)entry.cue],"Correct imported clip for "+entry.cue);
            var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(entry.clip)) as AudioImporter;
            Check(importer.defaultSampleSettings.loadType==AudioClipLoadType.Streaming,"Streaming BGM "+entry.cue);
            Check(entry.loop==((int)entry.cue<5),"Configured loop policy "+entry.cue);
        }
        foreach(var entry in audio.Configuration.sounds)Check(entry.clip!=null&&entry.clip.length>0,"SFX clip exists "+entry.cue);
        foreach(var t in Wait(2))yield return t;
        Check(audio.CurrentCue==BgmCue.MainMenu&&audio.ActiveMusic.isPlaying,"Main menu music playing");
        for(int loop=1;loop<=2;loop++)for(int day=1;day<=7;day++)
        {
            stage="Routing loop "+loop+" day "+day;
            Load(loop,day,day==7?(loop==2?Phase.FinalDecision:Phase.Suspects):Phase.Layout);
            foreach(var t in Wait(1.8f))yield return t;
            Check(audio.CurrentCue==DDayAudioManager.Route(loop,day,game.CurrentPhase)&&audio.ActiveMusic.isPlaying,"Actual date route L"+loop+" D"+day);
            Check(!audio.IsCrossfading&&!audio.OutgoingMusic.isPlaying,"Outgoing music stops within fade L"+loop+" D"+day);
        }
        Load(2,3,Phase.Layout);foreach(var t in Wait(1.8f))yield return t;
        int starts=audio.MusicStarts;float time=audio.ActiveMusic.time;
        game.ToggleBoard();game.SaveGame();foreach(var t in Wait(.35f))yield return t;game.ToggleBoard();
        Check(audio.MusicStarts==starts&&audio.ActiveMusic.time>time,"UI/auto save does not restart same BGM");
        audio.ActiveMusic.time=audio.ActiveMusic.clip.length-1.4f;
        foreach(var t in Wait(1.9f))yield return t;
        Check(audio.MusicStarts==starts+1&&audio.ActiveMusic.isPlaying&&!audio.OutgoingMusic.isPlaying,"Normal song tail/start blend completes once");
        foreach(bool bad in new[]{true,false})
        {
            stage=bad?"BE music":"HE music";
            Load(2,7,bad?Phase.BadEnding:Phase.Epilogue);
            // HE valid branch uses its existing two affirmative decisions.
            if(!bad){File.WriteAllText(Save,"{\"loop\":2,\"day\":7,\"phase\":\"Epilogue\",\"yesCount\":2,\"seats\":[0,1,2,3,4,5,6]}");game.ContinueGame();}
            foreach(var t in Wait(1.8f))yield return t;
            Check(audio.CurrentCue==(bad?BgmCue.BE:BgmCue.HE)&&!audio.ActiveMusic.loop,"Ending has correct non-looping music "+stage);
            starts=audio.MusicStarts;var player=game.GetComponent<HEEndingPlayer>();
            if(bad)player.PlayBEPreview();else player.PlayPreview();
            player.PreviewAt(player.Configuration.StartOf(1)+1);foreach(var t in Wait(.2f))yield return t;
            player.PreviewAt(player.Configuration.StartOf(2)+1);foreach(var t in Wait(.2f))yield return t;
            Check(audio.MusicStarts==starts,"Shot changes never restart "+stage);
            audio.ActiveMusic.time=audio.ActiveMusic.clip.length-.2f;foreach(var t in Wait(.6f))yield return t;
            Check(!audio.ActiveMusic.isPlaying&&audio.MusicStarts==starts,"Ending track finishes without restart "+stage);
            player.PreviewAt(player.Configuration.Duration+1);foreach(var t in Wait(.2f))yield return t;
            player.ReturnToMenu();game.ReturnToTitle();
            foreach(var t in Wait(1.8f))yield return t;
            Check(audio.CurrentCue==BgmCue.MainMenu&&audio.ActiveMusic.isPlaying,"Return restores menu music "+stage);
        }
        stage="SFX callbacks";Load(2,2,Phase.Layout);foreach(var t in Wait(1.8f))yield return t;
        foreach(SfxCue cue in Enum.GetValues(typeof(SfxCue)))
        {
            int before=audio.SoundPlays;audio.PlaySound(cue);audio.PlaySound(cue);
            foreach(var t in Wait(.25f))yield return t;
            Check(audio.LastSound==cue&&audio.SoundPlays==before+1,"One rate-limited SFX "+cue);
        }
        int sounds=audio.SoundPlays;
        typeof(DDayGame).GetField("draggingMoved",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,false);
        Call("MarkSeatDragMoved");Call("MarkSeatDragMoved");foreach(var t in Wait(.25f))yield return t;
        Check(audio.LastSound==SfxCue.Pickup&&audio.SoundPlays==sounds+1,"Actual drag movement callback picks up once");
        sounds=audio.SoundPlays;Call("FinishSeatDrag",1,2,true);foreach(var t in Wait(.25f))yield return t;
        Check(audio.LastSound==SfxCue.Drop&&audio.SoundPlays==sounds+1,"Valid placement callback plays drop once");
        sounds=audio.SoundPlays;Call("FinishSeatDrag",1,9,true);foreach(var t in Wait(.25f))yield return t;
        Check(audio.LastSound==SfxCue.Error&&audio.SoundPlays==sounds+1,"Invalid placement callback plays error once");
        sounds=audio.SoundPlays;Call("FinishSeatDrag",1,-1,false);foreach(var t in Wait(.25f))yield return t;
        Check(audio.SoundPlays==sounds,"Simple click/cancel does not play successful drop");
        sounds=audio.SoundPlays;Call("Record","audio_test","情报","test","test",false);foreach(var t in Wait(.25f))yield return t;
        Check(audio.LastSound==SfxCue.Clue&&audio.SoundPlays==sounds+1,"First acquired record plays discovery");
        Call("Record","audio_test","情报","test","test",false);foreach(var t in Wait(.25f))yield return t;
        Check(audio.SoundPlays==sounds+1,"Duplicate record does not replay discovery");
        float scale=Time.timeScale;Time.timeScale=0;audio.PlaySound(SfxCue.Back);
        foreach(var t in Wait(.25f))yield return t;
        Check(audio.LastSound==SfxCue.Back,"Pause menu SFX works at timeScale zero");Time.timeScale=scale;
        DDaySettings.SetBgmPercent(0);DDaySettings.SetSfxPercent(27);foreach(var t in Wait(.2f))yield return t;
        Check(audio.ActiveMusic.volume==0&&Mathf.Abs(audio.SoundSource.volume-.27f)<.001f,"Independent saved volume buses");
        DDaySettings.Load();Check(DDaySettings.BgmPercent==0&&DDaySettings.SfxPercent==27,"Audio settings reload correctly");
        DDaySettings.SetBgmPercent(70);DDaySettings.SetSfxPercent(35);
        Load(2,2,Phase.Layout);game.GetComponent<DDayUI>().OpenOfficeSettings();
        foreach(var t in Wait(.3f))yield return t;
        ScreenCapture.CaptureScreenshot(Path.Combine(Output,"Settings.png"));
        foreach(var t in Wait(.3f))yield return t;
    }
    static void Finish(bool success,string message)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);Time.timeScale=1;
        if(game!=null)game.ReturnToTitle();
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE",SessionState.GetString(Key+".OldOverride",""));
        foreach(string pref in new[]{"DDay.BgmPercent","DDay.SfxPercent"})
            if(SessionState.GetBool(Key+pref+".Exists",false))PlayerPrefs.SetInt(pref,SessionState.GetInt(Key+pref,0));else PlayerPrefs.DeleteKey(pref);
        PlayerPrefs.Save();DDaySettings.Load();
        File.WriteAllLines(Path.Combine(Output,"Results.txt"),new[]{success?"PASS":"FAIL",message}.Concat(evidence));
        sequence=null;EditorApplication.isPlaying=false;Debug.Log("D-DAY audio verification "+(success?"PASS":"FAIL"));
    }
}
