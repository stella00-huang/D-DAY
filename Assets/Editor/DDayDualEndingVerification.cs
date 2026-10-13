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
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DDayDualEndingVerification
{
    const string Key = "DDay.DualEndingVerification";
    static string Output => Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Docs","DualEndingVerification");
    static string Save => Path.Combine(Output,"TestAutoSave.json");
    static IEnumerator run;
    static DDayGame game;
    static HEEndingPlayer player;
    static DDayFinalDecisionUI choice;
    static readonly List<string> evidence = new List<string>();
    static DateTime began;
    static float nextStatus;
    static string stage;

    static DDayDualEndingVerification() { if (SessionState.GetBool(Key,false)) EditorApplication.update += Tick; }
    [MenuItem("D-DAY/Endings/Cancel Verification")]
    public static void Cancel()
    {
        if (SessionState.GetBool(Key,false)) Finish(false,"Cancelled for the latest workbook content audit.");
    }
    [MenuItem("D-DAY/Endings/Verify Both Endings in Play Mode")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play before verification."); return; }
        Directory.CreateDirectory(Output); evidence.Clear(); run = null;
        SessionState.SetString(Key+".SaveOverride",Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE")??"");
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE",Save);
        SessionState.SetBool(Key,true); EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (run == null) { run = Sequence(); began = DateTime.UtcNow; }
            if ((DateTime.UtcNow-began).TotalSeconds>480) throw new Exception("Ending verification timeout.");
            if (!run.MoveNext()) { Finish(true,"Both endings completed in real-time Play Mode. Native mouse automation was stopped; no native mouse PASS claimed."); return; }
            if (game!=null && Time.realtimeSinceStartup>nextStatus)
            {
                nextStatus=Time.realtimeSinceStartup+2;
                File.WriteAllText(Path.Combine(Output,"LiveStatus.txt"),stage+" | phase="+game.CurrentPhase
                    +" | endingTime="+player.Elapsed.ToString("0.0")+" | shot="+player.ShotIndex);
            }
        }
        catch(Exception e) { Finish(false,e.ToString()); }
    }
    static void Check(bool pass,string text)
    { if(!pass)throw new Exception(text); evidence.Add("PASS | "+text); }
    static void Capture(string name)
    { ScreenCapture.CaptureScreenshot(Path.Combine(Output,name+".png")); evidence.Add("SCREENSHOT | "+name); }
    static string Normalize(string text)=>Regex.Replace(text,@"\s+","");
    static void LoadChoice()
    {
        File.WriteAllText(Save,"{\"loop\":2,\"day\":7,\"phase\":\"FinalDecision\",\"sisyAlive\":true,\"seats\":[0,1,2,3,4,5,6]}");
        game.ContinueGame();
    }
    static IEnumerator Sequence()
    {
        do { yield return null; game=UnityEngine.Object.FindFirstObjectByType<DDayGame>(); }while(game==null||game.Data==null);
        player=game.GetComponent<HEEndingPlayer>(); choice=game.GetComponent<DDayFinalDecisionUI>();
        Check(player!=null&&player.HEConfiguration!=null&&player.BEConfiguration!=null,"Shared player loads both three-image configurations");
        ValidateConfig(player.HEConfiguration,DDayDualEndingAuthoring.HEText,"如露亦如电，应作如是观。");
        ValidateConfig(player.BEConfiguration,DDayDualEndingAuthoring.BEText,"一切有为法，如梦幻泡影。");
        SetResolution(1280,720); LoadChoice(); yield return null;
        stage="First question";
        Vector2 start=choice.YesButton.GetComponent<RectTransform>().anchoredPosition;
        float until=Time.realtimeSinceStartup+1.2f;
        while(Time.realtimeSinceStartup<until)
        {Check(choice.YesButton.GetComponent<RectTransform>().anchoredPosition==start&&choice.MoveCount==0,"First question remains fixed");yield return null;}
        choice.NoButton.onClick.Invoke(); yield return null;
        Check(game.CurrentPhase==Phase.BadEnding&&game.FinalQuestion==1,"First NO enters BE directly through the actual Button listener");
        game.ReturnToTitle(); yield return null;
        LoadChoice(); yield return null;
        choice.YesButton.onClick.Invoke(); yield return null;
        Check(game.FinalQuestion==1&&choice.IsTransitioning,"First YES fades into second question");
        stage="Second question motion";
        Vector2 min=start,max=start; bool pause=false;
        while(choice.MovingTime<16.5f || choice.IsTransitioning)
        {
            CheckChoiceBounds();
            Vector2 p=choice.YesButton.GetComponent<RectTransform>().anchoredPosition;
            min=Vector2.Min(min,p);max=Vector2.Max(max,p);pause|=choice.IsPaused&&choice.MoveCount>1;
            yield return null;
        }
        Check((max-min).magnitude>500&&choice.MoveCount>1,"Second YES visibly traverses large screen area");
        Check(pause&&choice.CurrentSpeed<=140,"Catch pauses and 15-second slowdown remain active");
        Check(EventSystem.current.currentSelectedGameObject==choice.YesButton.gameObject,"Moving YES retains keyboard focus");
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,choice.YesButton.transform.position)};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Check(hits.Count>0&&hits[0].gameObject==choice.YesButton.gameObject,"Moving YES retains its real raycast/click target");
        Capture("Choice_MovingYES_1280x720");
        float captureWait=Time.realtimeSinceStartup+.15f;while(Time.realtimeSinceStartup<captureWait)yield return null;
        choice.NoButton.onClick.Invoke();yield return null;
        Check(game.CurrentPhase==Phase.BadEnding&&game.FinalQuestion==2,"YES/NO enters BE through actual UI listeners");
        foreach(bool bad in new[]{true,false})
        {
            if(!bad)
            {
                game.ReturnToTitle();yield return null;LoadChoice();yield return null;
                choice.YesButton.onClick.Invoke();yield return null;
                while(choice.IsTransitioning)yield return null;
                ExecuteEvents.Execute(choice.YesButton.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
                yield return null;
                Check(game.CurrentPhase==Phase.Epilogue&&game.YesCount==2,"YES/YES enters HE through UGUI submit/navigation");
            }
            var config=bad?player.BEConfiguration:player.HEConfiguration;
            string label=bad?"BE":"HE"; stage=label+" natural playback";
            Check(player.Configuration==config,"Correct branch selects matching configuration "+label);
            int titleEntries=0;bool wasOpening=false,saved=false;var seen=new HashSet<string>();var captured=new HashSet<int>();
            while(!player.Completed)
            {
                if(player.IsOpening&&!wasOpening)titleEntries++;
                wasOpening=player.IsOpening;
                if(player.IsOpening&&player.Elapsed>2.1f&&player.Elapsed<2.4f&&!captured.Contains(-1))
                { Capture(label+"_OpeningTitle");captured.Add(-1); }
                if(player.SubtitleAlpha>.9f&&player.Subtitle==player.FullSubtitle&&player.FullSubtitle.Length>0)
                {
                    seen.Add(Normalize(player.FullSubtitle));ValidateTextLayout();
                    if(!captured.Contains(player.ShotIndex)){Capture(label+"_Shot"+(player.ShotIndex+1));captured.Add(player.ShotIndex);}
                    if(player.ShotIndex==2&&player.FullSubtitle==config.Pages(2).Last().text&&!captured.Contains(4))
                    {Capture(label+"_LastSentence");captured.Add(4);}
                }
                if(!saved&&player.Elapsed>config.StartOf(1)+2)
                {
                    float position=player.Elapsed;game.SaveGame();game.ReturnToTitle();
                    float frameWait=Time.realtimeSinceStartup+.12f;while(Time.realtimeSinceStartup<frameWait)yield return null;
                    game.ContinueGame();frameWait=Time.realtimeSinceStartup+.12f;while(Time.realtimeSinceStartup<frameWait)yield return null;
                    Check(Mathf.Abs(player.Elapsed-position)<.5f,"Mid-ending Continue restores timeline "+label);
                    saved=true;
                }
                yield return null;
            }
            Check(titleEntries==1,"Opening scripture title appears exactly once "+label);
            foreach(int i in new[]{0,1,2})foreach(var page in config.Pages(i))Check(seen.Contains(Normalize(page.text)),"Every formal subtitle rendered fully: "+label+" "+page.text);
            Check(Mathf.Abs(player.Elapsed-config.Duration)<.3f,"Natural playback duration "+label+" = "+config.Duration.ToString("0.00")+" seconds");
            Check(bad?game.BECompleted:game.CurrentPhase==Phase.TrueEnding,"Ending completion is saved "+label);
            while(player.Elapsed<config.Duration+1.1f)yield return null;
            var titleRoot=player.transform.Find("HE Ending Canvas/Ending title");
            string[] finalText=titleRoot.GetComponentsInChildren<Text>().Select(t=>t.text).ToArray();
            Check(finalText.SequenceEqual(new[]{"D-DAY","返回主菜单"}),"Final screen shows D-DAY and return only "+label);
            Capture(label+"_EndScreen");
            float titleFrameWait=Time.realtimeSinceStartup+.15f;while(Time.realtimeSinceStartup<titleFrameWait)yield return null;
            game.SaveGame();game.ReturnToTitle();
            titleFrameWait=Time.realtimeSinceStartup+.12f;while(Time.realtimeSinceStartup<titleFrameWait)yield return null;
            game.ContinueGame();titleFrameWait=Time.realtimeSinceStartup+.12f;while(Time.realtimeSinceStartup<titleFrameWait)yield return null;
            Check(player.Completed&&!player.IsOpening,"Completed save returns to final screen without title/replay "+label);
            titleRoot.Find("Return to main menu").GetComponent<Button>().onClick.Invoke();yield return null;
            Check(game.CurrentPhase==Phase.Title&&!player.OwnsUI,"Return button restores existing main menu "+label);
        }
        string untouched=File.ReadAllText(Save);
        foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720)})
        foreach(bool bad in new[]{false,true})
        {
            SetResolution(resolution.x,resolution.y);if(bad)player.PlayBEPreview();else player.PlayPreview();
            float sizeWait=Time.realtimeSinceStartup+3;
            while((Screen.width!=resolution.x||Screen.height!=resolution.y)&&Time.realtimeSinceStartup<sizeWait)yield return null;
            sizeWait=Time.realtimeSinceStartup+.25f;while(Time.realtimeSinceStartup<sizeWait)yield return null;
            var config=player.Configuration;
            foreach(int index in new[]{0,1,2})
            {
                var page=config.Pages(index).OrderByDescending(p=>p.text.Length).First();
                player.PreviewAt(config.StartOf(index)+page.start+1.6f);yield return null;yield return null;
                ValidateTextLayout();CheckImages();
                Check(Screen.width==resolution.x&&Screen.height==resolution.y,"Live 16:9 viewport "+resolution);
                Capture((bad?"BE":"HE")+"_"+resolution.x+"x"+resolution.y+"_Shot"+(index+1));yield return null;
            }
            // Reveal/advance only one caption; it cannot skip an entire ending.
            var first=config.Pages(0)[0];player.PreviewAt(config.StartOf(0)+first.start+.3f);yield return null;
            player.AdvanceSubtitle();yield return null;
            float captionWait=Time.realtimeSinceStartup+.3f;while(Time.realtimeSinceStartup<captionWait)yield return null;
            player.AdvanceSubtitle();yield return null;
            Check(player.Elapsed<config.StartOf(1)&&!player.Completed,"Subtitle acceleration cannot skip whole ending");
            player.PreviewAt(config.Duration);yield return null;player.ReturnToMenu();yield return null;
            Check(game.CurrentPhase==Phase.Title&&File.ReadAllText(Save)==untouched,"Editor preview leaves campaign/save intact");
        }
        game.Restart();game.StartGame();yield return null;
        Check(game.Loop==1&&game.Day==1&&!player.OwnsUI&&!choice.OwnsUI,"First-run ordinary flow unaffected");
        game.ReturnToTitle();
    }
    static void ValidateConfig(HEEndingConfig config,string[] expected,string title)
    {
        Check(config.shots.Length==3&&config.openingTitle==title&&config.endingSubtitle=="","Three illustrations and separate opening title "+config.name);
        for(int i=0;i<3;i++)
        {
            Check(config.shots[i].image!=null&&config.shots[i].image.width==1672&&config.shots[i].image.height==941,"Original full-resolution illustration "+config.shots[i].id);
            Check(config.shots[i].narration==expected[i],"Formal narration is unchanged "+config.shots[i].id);
            Check(Normalize(string.Concat(config.Pages(i).Select(p=>p.text)))==Normalize(expected[i]),"Pagination retains every original character "+config.shots[i].id);
            Check(!config.shots[i].narration.Contains(title)&&config.Pages(i).All(p=>p.duration>=3),"No scripture in subtitles; enough reading time "+config.shots[i].id);
        }
        evidence.Add("DURATION | "+config.name+" = "+config.Duration.ToString("0.00")+" seconds (automatic, no acceleration)");
    }
    static void ValidateTextLayout()
    {
        var text=player.transform.Find("HE Ending Canvas/Subtitle/Subtitle text").GetComponent<Text>();
        text.font.RequestCharactersInTexture(player.FullSubtitle,text.fontSize);
        Check(player.FullSubtitle.All(c=>char.IsWhiteSpace(c)||text.font.HasCharacter(c)),"All subtitle glyphs are available");
        Check(text.preferredHeight<=text.rectTransform.rect.height+1&&!text.resizeTextForBestFit&&text.transform.localScale==Vector3.one,"Subtitle fits 1-3 lines without deformation");
        Check(!Regex.IsMatch(text.text,@"\b(HE|BE|Happy Ending|Bad Ending)\b")&&!text.text.Contains("好结局")&&!text.text.Contains("坏结局"),"No player-visible ending classification");
    }
    static void CheckImages()
    {
        Vector2 view=player.CanvasRect.rect.size;
        foreach(var image in player.GetComponentsInChildren<RawImage>().Where(i=>i.name=="Illustration"||i.name=="Crossfade illustration"))
        {
            if(image.color.a<.01f||image.texture==null)continue;
            Vector2 size=image.rectTransform.rect.size,pan=image.rectTransform.anchoredPosition;
            Check(size.x*.5f-Mathf.Abs(pan.x)>=view.x*.5f-.1f&&size.y*.5f-Mathf.Abs(pan.y)>=view.y*.5f-.1f,"Illustration covers screen without black edges");
            Check(Mathf.Abs(size.x/size.y-image.texture.width/(float)image.texture.height)<.001f,"Original artwork aspect ratio retained");
        }
    }
    static void CheckChoiceBounds()
    {
        var moving=choice.YesButton.GetComponent<RectTransform>();var fixedButton=choice.NoButton.GetComponent<RectTransform>();
        Vector2 p=moving.anchoredPosition,half=moving.sizeDelta*.5f;
        Check(p.x-half.x>=choice.CanvasRect.rect.xMin&&p.x+half.x<=choice.CanvasRect.rect.xMax&&p.y-half.y>=choice.CanvasRect.rect.yMin&&p.y+half.y<=choice.CanvasRect.rect.yMax,"YES remains inside Canvas");
        Check(fixedButton.anchoredPosition==new Vector2(270,-120),"NO remains fixed");
        Check(!new Rect(p-half,half*2).Overlaps(new Rect(fixedButton.anchoredPosition-half,half*2))&&p.y+half.y<choice.QuestionRect.anchoredPosition.y-50,"YES avoids NO and question");
    }
    static void SetResolution(int width,int height)
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        var assembly=typeof(EditorWindow).Assembly;var type=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);
        var selected=type.GetProperty("selectedSizeIndex",flags);
        if(!SessionState.GetBool(Key+".SizeStored",false)){SessionState.SetInt(Key+".OriginalSize",(int)selected.GetValue(view));SessionState.SetBool(Key+".SizeStored",true);}
        var sizes=assembly.GetType("UnityEditor.GameViewSizes");var singleton=sizes.BaseType.GetProperty("instance",flags).GetValue(null);
        var group=sizes.GetProperty("currentGroup",flags).GetValue(singleton);var get=group.GetType().GetMethod("GetGameViewSize",flags);
        for(int i=0;i<(int)group.GetType().GetMethod("GetTotalCount",flags).Invoke(group,null);i++)
        {var size=get.Invoke(group,new object[]{i});var st=size.GetType();if((int)st.GetProperty("width",flags).GetValue(size)==width&&(int)st.GetProperty("height",flags).GetValue(size)==height){selected.SetValue(view,i);view.Focus();view.Repaint();return;}}
        throw new Exception("Missing Game View resolution.");
    }
    static void Finish(bool success,string message)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
        // Return while the test save override is still active: Play exit cannot write production progress.
        if(game!=null)game.ReturnToTitle();
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE",SessionState.GetString(Key+".SaveOverride",""));
        if(SessionState.GetBool(Key+".SizeStored",false))
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(EditorWindow.GetWindow(type),SessionState.GetInt(Key+".OriginalSize",0));
            SessionState.SetBool(Key+".SizeStored",false);
        }
        File.WriteAllLines(Path.Combine(Output,"Results.txt"),new[]{success?"PASS":"FAIL",message}.Concat(evidence.Distinct()));
        run=null;EditorApplication.isPlaying=false;Debug.Log("D-DAY dual ending verification "+(success?"PASS":"FAIL"));
    }
}
