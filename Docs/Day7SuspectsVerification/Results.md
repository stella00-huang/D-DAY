# Day7 suspect selection Play verification

2026-10-12T14:06:12

FAIL

System.InvalidOperationException: Native mouse selected all seven, switched and cancelled
  at DDayABPlayVerification.Check (System.Boolean pass, System.String description) [0x0001f] in E:\聚光灯\Assets\Editor\DDayABPlayVerification.cs:76 
  at DDayABPlayVerification+<Day7Sequence>d__25.MoveNext () [0x00be7] in E:\聚光灯\Assets\Editor\DDayDay7Verification.cs:131 
  at DDayABPlayVerification.Tick () [0x00074] in E:\聚光灯\Assets\Editor\DDayABPlayVerification.cs:59 

PASS | Main project Play Mode, not validation checkout
PASS | Exactly seven suspects in 4+3 order, no SISY
PASS | Correct animal/name mapping
PASS | Two distinct Sprites: fox
PASS | Identical uncompressed portrait dimensions: fox
PASS | Transparent circular mask: fox_neutral
PASS | Transparent circular mask: fox_surprised
PASS | Real expression pixel difference: fox
PASS | Two distinct Sprites: shiba
PASS | Identical uncompressed portrait dimensions: shiba
PASS | Transparent circular mask: shiba_neutral
PASS | Transparent circular mask: shiba_surprised
PASS | Real expression pixel difference: shiba
PASS | Two distinct Sprites: bear
PASS | Identical uncompressed portrait dimensions: bear
PASS | Transparent circular mask: bear_neutral
PASS | Transparent circular mask: bear_surprised
PASS | Real expression pixel difference: bear
PASS | Two distinct Sprites: wolf
PASS | Identical uncompressed portrait dimensions: wolf
PASS | Transparent circular mask: wolf_neutral
PASS | Transparent circular mask: wolf_surprised
PASS | Real expression pixel difference: wolf
PASS | Two distinct Sprites: panda
PASS | Identical uncompressed portrait dimensions: panda
PASS | Transparent circular mask: panda_neutral
PASS | Transparent circular mask: panda_surprised
PASS | Real expression pixel difference: panda
PASS | Two distinct Sprites: cat
PASS | Identical uncompressed portrait dimensions: cat
PASS | Transparent circular mask: cat_neutral
PASS | Transparent circular mask: cat_surprised
PASS | Real expression pixel difference: cat
PASS | Two distinct Sprites: boss
PASS | Identical uncompressed portrait dimensions: boss
PASS | Transparent circular mask: boss_neutral
PASS | Transparent circular mask: boss_surprised
PASS | Real expression pixel difference: boss
PASS | Initial selection empty and confirmation disabled
PASS | Empty confirmation / SISY rejected
GAME_VIEW | 1280x720
GAME_VIEW | 1920x1080
PASS | 16:9 live viewport Day7_Initial_1920x1080 actual=1920x1080
PASS | Rendered text fits containers: Day7_Initial_1920x1080
SCREENSHOT | Day7_Initial_1920x1080
GAME_VIEW | 1600x900
PASS | 16:9 live viewport Day7_Initial_1600x900 actual=1600x900
PASS | Rendered text fits containers: Day7_Initial_1600x900
SCREENSHOT | Day7_Initial_1600x900
GAME_VIEW | 1280x720
PASS | 16:9 live viewport Day7_Initial_1280x720 actual=1280x720
PASS | Rendered text fits containers: Day7_Initial_1280x720
SCREENSHOT | Day7_Initial_1280x720
GAME_VIEW | 1280x720
PASS | Selection does not begin ending: fox
PASS | Selected portrait 1.05 scale: fox
PASS | Pending selection survives Continue: fox
PASS | Click selected cancels: fox
PASS | Switch keeps exactly one selection: fox
GAME_VIEW | 1920x1080
PASS | 16:9 live viewport Day7_Selected_fox_1920x1080 actual=1920x1080
PASS | Rendered text fits containers: Day7_Selected_fox_1920x1080
SCREENSHOT | Day7_Selected_fox_1920x1080
GAME_VIEW | 1600x900
PASS | 16:9 live viewport Day7_Selected_fox_1600x900 actual=1600x900
PASS | Rendered text fits containers: Day7_Selected_fox_1600x900
SCREENSHOT | Day7_Selected_fox_1600x900
GAME_VIEW | 1280x720
PASS | 16:9 live viewport Day7_Selected_fox_1280x720 actual=1280x720
PASS | Rendered text fits containers: Day7_Selected_fox_1280x720
SCREENSHOT | Day7_Selected_fox_1280x720
GAME_VIEW | 1280x720
PASS | Existing non-unique culprit ending: fox
PASS | Confirmed ID and ending position survive Continue: fox
PASS | All suspects reach the same original causal replay: fox
PASS | Selection does not begin ending: shiba
PASS | Selected portrait 1.05 scale: shiba
PASS | Pending selection survives Continue: shiba
PASS | Click selected cancels: shiba
PASS | Switch keeps exactly one selection: shiba
PASS | Existing non-unique culprit ending: shiba
PASS | Confirmed ID and ending position survive Continue: shiba
PASS | All suspects reach the same original causal replay: shiba
PASS | Selection does not begin ending: bear
PASS | Selected portrait 1.05 scale: bear
PASS | Pending selection survives Continue: bear
PASS | Click selected cancels: bear
PASS | Switch keeps exactly one selection: bear
PASS | Existing non-unique culprit ending: bear
PASS | Confirmed ID and ending position survive Continue: bear
PASS | All suspects reach the same original causal replay: bear
PASS | Selection does not begin ending: wolf
PASS | Selected portrait 1.05 scale: wolf
PASS | Pending selection survives Continue: wolf
PASS | Click selected cancels: wolf
PASS | Switch keeps exactly one selection: wolf
PASS | Existing non-unique culprit ending: wolf
PASS | Confirmed ID and ending position survive Continue: wolf
PASS | All suspects reach the same original causal replay: wolf
PASS | Selection does not begin ending: panda
PASS | Selected portrait 1.05 scale: panda
PASS | Pending selection survives Continue: panda
PASS | Click selected cancels: panda
PASS | Switch keeps exactly one selection: panda
PASS | Existing non-unique culprit ending: panda
PASS | Confirmed ID and ending position survive Continue: panda
PASS | All suspects reach the same original causal replay: panda
PASS | Selection does not begin ending: cat
PASS | Selected portrait 1.05 scale: cat
PASS | Pending selection survives Continue: cat
PASS | Click selected cancels: cat
PASS | Switch keeps exactly one selection: cat
PASS | Existing non-unique culprit ending: cat
PASS | Confirmed ID and ending position survive Continue: cat
PASS | All suspects reach the same original causal replay: cat
PASS | Selection does not begin ending: boss
PASS | Selected portrait 1.05 scale: boss
PASS | Pending selection survives Continue: boss
PASS | Click selected cancels: boss
PASS | Switch keeps exactly one selection: boss
GAME_VIEW | 1920x1080
PASS | 16:9 live viewport Day7_Selected_boss_1920x1080 actual=1920x1080
PASS | Rendered text fits containers: Day7_Selected_boss_1920x1080
SCREENSHOT | Day7_Selected_boss_1920x1080
GAME_VIEW | 1600x900
PASS | 16:9 live viewport Day7_Selected_boss_1600x900 actual=1600x900
PASS | Rendered text fits containers: Day7_Selected_boss_1600x900
SCREENSHOT | Day7_Selected_boss_1600x900
GAME_VIEW | 1280x720
PASS | 16:9 live viewport Day7_Selected_boss_1280x720 actual=1280x720
PASS | Rendered text fits containers: Day7_Selected_boss_1280x720
SCREENSHOT | Day7_Selected_boss_1280x720
GAME_VIEW | 1280x720
PASS | Existing non-unique culprit ending: boss
PASS | Confirmed ID and ending position survive Continue: boss
PASS | All suspects reach the same original causal replay: boss
PASS | Original ending still unlocks loop2
PASS | Pre-change Day7 save compatible
PASS | Loop2 excludes suspect UI
PASS | Loop2 retains second APIS question
PASS | Loop2 unchanged votes True/True
PASS | Loop2 excludes suspect UI
PASS | Loop2 retains second APIS question
PASS | Loop2 unchanged votes True/False
PASS | Loop2 excludes suspect UI
PASS | Loop2 retains second APIS question
PASS | Loop2 unchanged votes False/True
PASS | Loop2 excludes suspect UI
PASS | Loop2 retains second APIS question
PASS | Loop2 unchanged votes False/False
GAME_VIEW | 1280x720
GAME_VIEW | 1920x1080
PASS | 16:9 live viewport Day7_MouseReady_1920x1080 actual=1920x1080
PASS | Rendered text fits containers: Day7_MouseReady_1920x1080
SCREENSHOT | Day7_MouseReady_1920x1080
GAME_VIEW | 1600x900
PASS | 16:9 live viewport Day7_MouseReady_1600x900 actual=1600x900
PASS | Rendered text fits containers: Day7_MouseReady_1600x900
SCREENSHOT | Day7_MouseReady_1600x900
GAME_VIEW | 1280x720
PASS | 16:9 live viewport Day7_MouseReady_1280x720 actual=1280x720
PASS | Rendered text fits containers: Day7_MouseReady_1280x720
SCREENSHOT | Day7_MouseReady_1280x720
GAME_VIEW | 1280x720
MOUSE_TEST_READY | Native Game View clicks required for all seven portraits, switch, cancel and confirm
NATIVE_MOUSE_STATE | none -> fox
NATIVE_MOUSE_STATE | fox -> bear
NATIVE_MOUSE_STATE | bear -> shiba
NATIVE_MOUSE_STATE | shiba -> fox
NATIVE_MOUSE_STATE | fox -> bear
NATIVE_MOUSE_STATE | bear -> wolf
NATIVE_MOUSE_STATE | wolf -> boss
NATIVE_MOUSE_STATE | boss -> cat
NATIVE_MOUSE_STATE | cat -> panda
NATIVE_MOUSE_STATE | panda -> cat
NATIVE_MOUSE_STATE | cat -> boss
NATIVE_MOUSE_STATE | boss -> bear

Day7 native mouse input was observed in the live Game View after automated state/save regression.
