# AB / Portrait Play verification

2026-10-11T23:49:05

FAIL

System.InvalidOperationException: Rendered text fits containers: 02d_RecordBook_1280x720 拖动物到这里
可以交换座位 [needs 51.44, has 47]
  at DDayABPlayVerification.Check (System.Boolean pass, System.String description) [0x0001f] in E:\聚光灯\Assets\Editor\DDayABPlayVerification.cs:69 
  at DDayABPlayVerification+<Capture>d__17.MoveNext () [0x000a6] in E:\聚光灯\Assets\Editor\DDayABPlayVerification.cs:371 
  at DDayABPlayVerification.Tick () [0x00060] in E:\聚光灯\Assets\Editor\DDayABPlayVerification.cs:52 

GAME_VIEW | 1280x720
PASS | Unity 6 main project Play Mode
PASS | A1 1/1 exhaustive C# solutions=44
PASS | A1 1/1 initial unmet=4
PASS | A1 1/2 exhaustive C# solutions=8
PASS | A1 1/2 initial unmet=5
PASS | A1 1/3 exhaustive C# solutions=5
PASS | A1 1/3 initial unmet=4
PASS | A1 1/4 exhaustive C# solutions=12
PASS | A1 1/4 initial unmet=4
PASS | A1 1/5 exhaustive C# solutions=2
PASS | A1 1/5 initial unmet=6
PASS | A1 1/6 exhaustive C# solutions=17
PASS | A1 1/6 initial unmet=3
PASS | A1 2/1 exhaustive C# solutions=12
PASS | A1 2/1 initial unmet=8
PASS | A1 2/2 exhaustive C# solutions=2
PASS | A1 2/2 initial unmet=5
PASS | A1 2/3 exhaustive C# solutions=84
PASS | A1 2/3 initial unmet=5
PASS | A1 2/4 exhaustive C# solutions=8
PASS | A1 2/4 initial unmet=5
PASS | A1 2/5 exhaustive C# solutions=139
PASS | A1 2/5 initial unmet=6
PASS | A1 2/6 exhaustive C# solutions=524
PASS | A1 2/6 initial unmet=3
PASS | Portrait real asset rabbit/neutral
PASS | Portrait texture loaded
PASS | Portrait real asset rabbit/happy
PASS | Portrait texture loaded
PASS | Portrait real asset wolf/neutral
PASS | Portrait texture loaded
PASS | Portrait real asset wolf/nervous
PASS | Portrait texture loaded
PASS | Portrait real asset fox/neutral
PASS | Portrait texture loaded
PASS | Portrait real asset fox/serious
PASS | Portrait texture loaded
PASS | Portrait real asset shiba/neutral
PASS | Portrait texture loaded
PASS | Portrait real asset shiba/sad
PASS | Portrait texture loaded
PASS | Portrait real asset cat/neutral
PASS | Portrait texture loaded
PASS | Portrait real asset cat/serious
PASS | Portrait texture loaded
PASS | Portrait real asset bear/neutral
PASS | Portrait texture loaded
PASS | Portrait real asset bear/happy
PASS | Portrait texture loaded
PASS | Portrait real asset panda/neutral
PASS | Portrait texture loaded
PASS | Portrait real asset panda/sad
PASS | Portrait texture loaded
PASS | Missing expression honestly falls back to neutral
PASS | Explicit per-line expression + two-speaker switch
PASS | Day1 starts as a puzzle, no A2 autoplay
PASS | L1 Day1 SISY locked B3
PASS | B1 blocked before A1 solved
PASS | Start Work blocked by A1
PASS | Rendered text fits containers: 01_Day1_Office 
SCREENSHOT | 01_Day1_Office
PASS | A2 UGUI bubble shown
PASS | Bubble callback opens portrait; no record before dialogue completion
PASS | A2 portrait connected
PASS | Rendered text fits containers: 02_A2_Portrait 
SCREENSHOT | 02_A2_Portrait
PASS | Dialogue input handler reveals text before advancing
PASS | Second dialogue input advances exactly one line
PASS | Actual A2 frame switches speaking portrait
PASS | Rendered text fits containers: 02b_A2_SecondSpeaker 
SCREENSHOT | 02b_A2_SecondSpeaker
PASS | A2 completion records once
PASS | A2 completion only shows short record update, with unread state
PASS | Record button unread badge visible
PASS | Rendered text fits containers: 02c_RecordUpdate 
SCREENSHOT | 02c_RecordUpdate
PASS | A2 acquired bubble removed and deduplicated
PASS | Legal seating move does not show a drag toast
PASS | Unclaimed adjacency bubble appears after move
PASS | Unclaimed bubble disappears when condition is lost
PASS | Move never autoplays/grants A2 and dialogue closing does not start a drag
PASS | Future B2-dependent clues blocked
PASS | Bottom four actions in one HorizontalLayoutGroup
PASS | Sequential day entered 1/1
PASS | Unsolved daily start 1/1
PASS | Actual swap/move 1/1 wolf
PASS | Actual swap/move 1/1 fox
PASS | Actual swap/move 1/1 shiba
PASS | Actual swap/move 1/1 cat
PASS | Actual swap/move 1/1 bear
PASS | Actual swap/move 1/1 panda
PASS | Every employee A1 satisfied 1/1
PASS | Manual save created
PASS | Manual overwrite requires confirmation
PASS | Continue restores seats/A2 records/unread
PASS | Start Work opens B1 directly with solved A1 and unassigned tasks
PASS | Unread persists across manual/autosave reload

Real hardware mouse/keyboard input was not injected; callbacks and runtime methods were tested.
