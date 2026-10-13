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
    [MenuItem("D-DAY/Content/Verify Latest Workbook in Play Mode")]
    public static void VerifyLatest() => StartVerification(false, true);

    [Serializable] sealed class ContentFixture
    {
        public int loop, day;
        public string phase = "Layout";
        public bool sisyAlive;
        public CampaignRecord[] records = Array.Empty<CampaignRecord>();
        public string[] triggeredSeatEvents = Array.Empty<string>();
        public string[] completedFixedEvents = Array.Empty<string>();
        public int[] seats;
    }
    static string ContentClaim(ABClue c, int loop) => c.loop == 0 ? c.id + "@L" + loop : c.id;
    static void ContentLoad(DDayGame game, int loop, int day, string[] completed = null,
        CampaignRecord[] records = null, string[] claims = null)
    {
        var fixture = new ContentFixture { loop = loop, day = day, sisyAlive = loop == 2,
            completedFixedEvents = completed ?? Array.Empty<string>(), records = records ?? Array.Empty<CampaignRecord>(),
            triggeredSeatEvents = claims ?? Array.Empty<string>(),
            seats = day < 7 ? ABConfig.Current.SeatDay(loop, day).initialSeats : null };
        File.WriteAllText(Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE"), JsonUtility.ToJson(fixture));
        game.ContinueGame();
    }
    static string ContentNormal(string text) => Regex.Replace(text ?? "", @"\s+", "");
    static string ContentPayload(string text, bool narration=false) => narration ? text
        : Regex.Replace(text ?? "", @"^(?:SISY|ORIN|NARCIS|ECHO|METIS|CASSIA|THANE)[：:]|^(?:SISY|ORIN|NARCIS|ECHO|METIS|CASSIA|THANE)(?=“)", "").Trim();
    static void DrainContent(DDayGame game)
    {
        int guard = 0;
        while (game.CurrentPhase == Phase.Narrative && game.CurrentPagePanel == null)
        { game.AdvanceNarrative(); if (++guard > 1000) throw new Exception("Dialogue did not finish."); }
    }
    static int[] ContentWitness(DDayGame game, ABClue clue)
    {
        var match = typeof(DDayGame).GetMethod("SeatExplorationMatches", BindingFlags.Instance | BindingFlags.NonPublic);
        int[] found = null;
        bool[] used = new bool[8];
        Array.Fill(game.Seats, -1);
        Action<int> visit = null;
        visit = animal => {
            if (found != null) return;
            if (animal == 7)
            { if ((bool)match.Invoke(game, new object[] { clue })) found = (int[])game.Seats.Clone(); return; }
            for (int seat = 0; seat < 8; seat++)
            {
                if (used[seat] || game.Loop == 1 && game.Day > 1 && seat == 6
                    || game.Loop == 1 && game.Day == 1 && (animal == 0 ? seat != 6 : seat == 6)) continue;
                game.Seats[animal] = seat; used[seat] = true; visit(animal + 1); used[seat] = false;
            }
            game.Seats[animal] = -1;
        };
        visit(game.Loop == 1 && game.Day > 1 ? 1 : 0);
        return found;
    }
    static IEnumerator LatestContentSequence()
    {
        DDayGame game = null;
        while (game == null || game.Data == null)
        { yield return null; game = UnityEngine.Object.FindFirstObjectByType<DDayGame>(); }
        evidence.Clear(); answers.Clear();
        SetResolution(1280,720); yield return null;
        var config = ABConfig.Current;
        Check(config.clues.Length == 40 && config.tasks.Length == 24 && config.fixedEvents.Length == 10,
            "Latest workbook counts: A2=40, B1=24, B2=10");
        Check(config.tasks.All(t => string.IsNullOrEmpty(t.description)), "No internal B1 question in public descriptions");
        Check(config.TasksFor(1,7).Length == 0 && config.TasksFor(2,7).Length == 0, "Both Day7 have no B1 cards");
        int[] expected = {44,8,5,12,2,17,12,2,84,8,139,524}; int result = 0;
        for (int loop = 1; loop <= 2; loop++)
            foreach (DayData day in CampaignSchedule.MakeDays(game.Data, loop))
            {
                int[] answer = null;
                int count = CountSolutions(day, loop, day.day, ref answer);
                Check(count == expected[result++], "C# exhaustive A1 L"+loop+" D"+day.day+"="+count);
                answers[loop+"/"+day.day] = answer;
                yield return null;
            }
        // Every A2 record is tested through its actual bubble callback, including both common-loop claims.
        foreach (ABClue clue in config.clues)
        foreach (int loop in clue.loop == 0 ? new[] {1,2} : new[] {clue.loop})
        {
            string[] completed = string.IsNullOrEmpty(clue.requiredEvent) ? Array.Empty<string>() : new[] {clue.requiredEvent};
            ContentLoad(game, loop, clue.firstDay, completed);
            foreach (TaskCardData task in game.Tasks.Cards) game.Tasks.Assign(task.id,1,true);
            if (clue.condition == "desk_x_prereq")
                typeof(DDayGame).GetMethod("Record",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,
                    new object[]{"A2_R25","物品","test prerequisite","test prerequisite",false});
            int[] witness = ContentWitness(game, clue);
            Check(witness != null, "A2 attainable "+clue.id+" L"+loop);
            typeof(DDayGame).GetMethod("SetSeating",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{witness});
            var claims = (HashSet<string>)typeof(DDayGame).GetField("triggeredSeatEvents",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
            foreach (ABClue other in config.clues.Where(c => c.id != clue.id)) claims.Add(ContentClaim(other,loop));
            Check(game.FindSeatExplorations().Any(c=>c.id==clue.id), "A2 available in window "+clue.id+" L"+loop);
            int before = game.Records.Count;
            Check(game.OpenSeatExploration(game.ExplorationHost(clue)), "A2 bubble opens "+clue.id+" L"+loop);
            Check(game.Records.Count == before, "No record before A2 finishes "+clue.id);
            if(clue.id=="A2_R04" && loop==1) yield return Capture("Latest_A2_Dialogue");
            DialogueFrame[] renderedClue = DialoguePresentation.Current.Frames(clue.id,clue.dialogue,SeatRules.Ids[game.ExplorationHost(clue)],0);
            Check(ContentNormal(string.Concat(renderedClue.Select(f=>f.text)))
                == ContentNormal(string.Concat(clue.dialogue.Select(line=>ContentPayload(line)))),"A2 pagination retains all dialogue words "+clue.id);
            Check(ContentNormal(string.Concat(renderedClue.Select(f=>f.text)))
                != ContentNormal(clue.description) || clue.description.Length == 0, "A2 dialogue separated from record text "+clue.id);
            DrainContent(game);
            Check(game.Records.Count==before+1 && game.Records.Last().description==clue.description,
                "A2 exact record after dialogue "+clue.id+" L"+loop);
            Check(!game.FindSeatExplorations().Any(c=>c.id==clue.id), "A2 deduplicates "+clue.id+" L"+loop);
            game.SaveGame(); game.ReturnToTitle(); game.ContinueGame();
            Check(!game.FindSeatExplorations().Any(c=>c.id==clue.id) && game.HasUnreadRecords, "A2 save/read claim+unread "+clue.id);
            if(clue.id=="A2_R04" && loop==1)
            {
                game.ToggleBoard(1); yield return Capture("Latest_RecordBook");
                Check(!game.HasUnreadRecords,"Reading the acquired record clears its unread marker");
                game.ToggleBoard(); game.SaveGame(); game.ContinueGame();
                Check(!game.HasUnreadRecords,"Read status persists after save/load");
            }
            if (!string.IsNullOrEmpty(clue.requiredEvent))
            {
                ContentLoad(game, loop, clue.firstDay);
                typeof(DDayGame).GetMethod("SetSeating",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{witness});
                Check(!game.FindSeatExplorations().Any(c=>c.id==clue.id), "A2 future state blocked "+clue.id);
            }
            if(clue.lastDay>clue.firstDay)
            {
                ContentLoad(game,loop,clue.lastDay,completed);
                typeof(DDayGame).GetMethod("SetSeating",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{witness});
                Check(game.FindSeatExplorations().Any(c=>c.id==clue.id), "Cross-day window includes final date "+clue.id);
            }
            yield return null;
        }
        // Old global common IDs migrate to the loop in which their record was acquired.
        var common = config.clues.First(c=>c.id=="A2_R41");
        ContentLoad(game,1,1,records:new[]{new CampaignRecord{id=common.id,loop=1,day=1,category=common.category,title=common.title}});
        Check(!game.FindSeatExplorations().Any(c=>c.id==common.id),"Legacy book entry without trigger list still suppresses repeat claims");
        ContentLoad(game,2,1,records:new[]{new CampaignRecord{id=common.id,loop=1,day=1,category=common.category,title=common.title}},claims:new[]{common.id});
        Check(game.FindSeatExplorations().Any(c=>c.id==common.id), "Legacy common claim L1 does not suppress L2");
        int uniqueBefore=game.Records.Count;
        Check(game.OpenSeatExploration(game.ExplorationHost(common)), "Common L2 re-claim callback"); DrainContent(game);
        Check(game.Records.Count==uniqueBefore && game.Records.Single().loop==2 && game.HasUnreadRecords,
            "Common per-loop claim refreshes one unique book entry");
        // All authored owner branches use the real task assignment entry and shared dialogue UI.
        foreach (ABTask task in config.tasks.Where(t=>t.day<7 && !t.@fixed))
        {
            ContentLoad(game,task.loop,task.day);
            typeof(DDayGame).GetMethod("SetSeating",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{answers[task.loop+"/"+task.day]});
            game.ToggleTasks();
            foreach(int owner in Enumerable.Range(0,7).Where(i=>game.Seats[i]>=0))
            {
                game.AssignTask(task.id,owner);
                Check(game.CurrentPhase==Phase.Narrative && game.Tasks.Owner(task.id)==owner,"B1 assignment "+task.sourceRow+"/"+SeatRules.Ids[owner]);
                if(task.sourceRow==4 && owner==0) yield return Capture("Latest_B1_Key_Dialogue");
                ABTaskBranch branch=CampaignSchedule.ResponseBranch(task.loop,task.day,task.id,owner,owner==0);
                Check(branch==null || !game.NarrativeLine.Contains("【关键"),"No key-role annotations in B1 dialogue "+task.sourceRow);
                string response=CampaignSchedule.TaskResponse(task.loop,task.day,task.id,owner,owner==0);
                var rendered=DialoguePresentation.Current.Frames("B1_L"+task.loop+"_D"+task.day+"_"+task.id+"_"+SeatRules.Ids[owner],
                    DialoguePresentation.SplitTurns(response),SeatRules.Ids[owner],0);
                Check(ContentNormal(string.Concat(rendered.Select(f=>f.text)))==ContentNormal(string.Concat(
                    DialoguePresentation.SplitTurns(response).Select(line=>ContentPayload(line,branch?.narration==true)))),
                    "B1 pagination retains all owner-branch words "+task.sourceRow+"/"+SeatRules.Ids[owner]);
                DrainContent(game);
                Check(game.TasksOpen && game.CurrentPhase==Phase.Layout,"B1 returns to task panel "+task.sourceRow);
            }
            yield return null;
        }
        // Manual slots and automatic saves keep the existing task IDs, owners, and record data.
        ContentLoad(game,1,3);
        typeof(DDayGame).GetMethod("SetSeating",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{answers["1/3"]});
        game.ToggleTasks();
        foreach(var card in game.Tasks.Cards){game.AssignTask(card.id,1);DrainContent(game);}
        game.ToggleTasks();
        Check(game.SaveManualSlot(15,true),"Existing manual slot 15 can save latest content");
        string automatic=Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE");
        string manual=Path.Combine(Path.GetDirectoryName(automatic),"DDayManualSlot15.json");
        File.Copy(manual,automatic,true);game.ContinueGame();
        Check(game.Day==3 && game.Tasks.Cards.Length==3 && game.Tasks.Cards.All(c=>game.Tasks.Owner(c.id)==1),
            "Manual save restores independent Day3 task IDs and owners");
        game.StartGame(); DrainContent(game);
        for(int loop=1;loop<=2;loop++)
        {
            for(int day=1;day<=6;day++)
            {
                DrainContent(game); Check(game.Loop==loop && game.Day==day,"Sequential L"+loop+" D"+day);
                game.StartWork(); Check(game.CurrentPhase==Phase.Layout,"Unsolved A1 blocks work L"+loop+" D"+day);
                int[] answer=answers[loop+"/"+day];
                for(int i=loop==1?1:0;i<7;i++) game.TryPlaceAnimal(i,answer[i]);
                Check(game.SeatsReady,"All daily rules refresh after placement L"+loop+" D"+day);
                game.StartWork(); Check(game.TasksOpen && game.CurrentPhase==Phase.Layout,"B1 mandatory before work L"+loop+" D"+day);
                if(loop==1 && day==3)
                {
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720)})
                    { SetResolution(size.x,size.y); yield return Capture("Latest_TaskPanel_"+size.x); }
                    Check(game.Tasks.Cards.Length==3,"Three independent Day3 task cards");
                }
                foreach(TaskCardData card in game.Tasks.Cards) { game.AssignTask(card.id,1); DrainContent(game); }
                if(loop==2 && day==1)
                {
                    Check(DDayGame.SisySurvives(game.Seats,new[]{1}),"Three survival conditions true");
                    Check(!DDayGame.SisySurvives(game.Seats,new[]{0}),"SISY assignment fails survival");
                    int[] altered=(int[])game.Seats.Clone(); altered[1]=2;
                    Check(!DDayGame.SisySurvives(altered,new[]{1}),"ORIN non-adjacency fails survival");
                    altered=(int[])game.Seats.Clone(); altered[4]=1;
                    Check(!DDayGame.SisySurvives(altered,new[]{1}),"METIS coffee fails survival");
                }
                game.ToggleTasks(); game.StartWork();
                Check(game.CurrentPhase==Phase.Work,"A1+B1 allows work L"+loop+" D"+day);
                while(game.CurrentPhase==Phase.Work || game.CurrentPhase==Phase.AfterHours || game.CurrentPhase==Phase.Narrative && game.CurrentDialogue.eventId.StartsWith("B1_"))
                { if(game.CurrentPhase==Phase.Narrative)game.AdvanceNarrative(); yield return null; }
                if(config.FixedFor(loop,day)!=null)
                {
                    Check(game.CurrentPhase==Phase.Narrative && game.CurrentDialogue.eventId==config.FixedFor(loop,day).id,
                        "Correct B2 only after work L"+loop+" D"+day);
                    if(loop==1 && day==1) yield return Capture("Latest_Day1_Comic");
                    var page=game.CurrentPagePresentation;
                    if(page!=null)
                    {
                        if(config.FixedFor(loop,day).comicTexts?.Length==page.panels.Length)
                            Check(ContentNormal(string.Concat(page.Frames().Select(f=>f.text)))==ContentNormal(string.Concat(
                                config.FixedFor(loop,day).comicTexts.SelectMany(p=>p.lines))),"Full Excel B2 prose in comic L"+loop+" D"+day);
                        else
                        {
                            var fixedData=config.FixedFor(loop,day);
                            var originalFrames=DialoguePresentation.Current.Frames(fixedData.id,fixedData.lines,"",0)
                                .Where(f=>!Regex.IsMatch(fixedData.lines[f.sourceLineIndex],@"^[①-⑨]"));
                            Check(ContentNormal(string.Concat(page.Frames().Where(f=>page.panels[f.sourceLineIndex].sourceLines?.Length>0).Select(f=>f.text)))==ContentNormal(string.Concat(
                                originalFrames.Select(f=>f.text))),"Existing Day6 comic retains every authored dialogue and narration");
                        }
                        object saved=typeof(DDayGame).GetMethod("BuildSave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
                        var saveType=saved.GetType();
                        int index=Mathf.Min(2,page.Frames().Length-1);
                        saveType.GetField("fixedDialogueIndex").SetValue(saved,index);
                        File.WriteAllText(automatic,JsonUtility.ToJson(saved));game.ContinueGame();
                        Check(game.NarrativeIndex==index,"New comic save resumes exact caption L"+loop+" D"+day);
                        if(config.FixedFor(loop,day).comicTexts?.Length==page.panels.Length)
                        {
                            saveType.GetField("fixedPresentationId").SetValue(saved,page.presentationId);
                            saveType.GetField("fixedDialogueIndex").SetValue(saved,3);
                            File.WriteAllText(automatic,JsonUtility.ToJson(saved));game.ContinueGame();
                            Check(game.CurrentDialogue.sourceLineIndex==3,"Legacy comic save resumes original panel L"+loop+" D"+day);
                        }
                    }
                    if(loop==2 && day==6)
                        Check(!game.Records.Any(r=>r.id=="evidence_chain"),"No alliance before dinner sequence ends");
                    while(game.CurrentPhase==Phase.Narrative && game.Day==day)
                    { game.AdvanceNarrative(); yield return null; }
                    Check(game.CompletedFixedEvents.Contains(config.FixedFor(loop,day).id),"B2 completed once L"+loop+" D"+day);
                }
            }
            Check(game.Day==7 && game.Tasks.Cards.Length==0,"Day7 no normal work L"+loop);
            if(loop==1)
            {
                game.ChooseSuspect("wolf"); DrainContent(game);
                while(game.CurrentPhase==Phase.Replay)game.AdvanceReplay(); DrainContent(game);
            }
        }
        Check(game.CurrentPhase==Phase.FinalDecision,"Second loop Day7 existing decision UI");
        string savePath=Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE"), choices=File.ReadAllText(savePath);
        game.ChooseFinal(false); Check(game.CurrentPhase==Phase.BadEnding,"NO -> existing BE");
        File.WriteAllText(savePath,choices);game.ContinueGame();game.ChooseFinal(true);game.ChooseFinal(false);
        Check(game.CurrentPhase==Phase.BadEnding,"YES+NO -> existing BE");
        File.WriteAllText(savePath,choices);game.ContinueGame();game.ChooseFinal(true);game.ChooseFinal(true);
        Check(game.CurrentPhase==Phase.Epilogue,"YES+YES -> existing HE");
        Check(!evidence.Any(x=>x.StartsWith("TEXT_LAYOUT_FAIL")),"Captured text containers have no overflow");
    }
}
