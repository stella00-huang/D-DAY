# AB / Portrait Play verification

2026-10-11T22:57:58

FAIL

System.InvalidOperationException: Game View Space reveals text before advancing
  at DDayABPlayVerification.Check (System.Boolean pass, System.String description) [0x0001f] in E:\聚光灯\Assets\Editor\DDayABPlayVerification.cs:69 
  at DDayABPlayVerification+<Sequence>d__14.MoveNext () [0x0086f] in E:\聚光灯\Assets\Editor\DDayABPlayVerification.cs:143 
  at DDayABPlayVerification.Tick () [0x00060] in E:\聚光灯\Assets\Editor\DDayABPlayVerification.cs:52 

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
PASS | B1 blocked before A1 solved and confirmed
PASS | Start Work blocked by A1
SCREENSHOT | 01_Day1_Office
PASS | A2 UGUI bubble shown
PASS | Bubble callback opens portrait; no record before dialogue completion
PASS | A2 portrait connected
SCREENSHOT | 02_A2_Portrait

Real hardware mouse/keyboard input was not injected; callbacks and runtime methods were tested.
