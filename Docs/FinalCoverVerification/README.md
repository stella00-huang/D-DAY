# 正式主菜单封面验收（2026-10-13）

## 已完成

- 在原 `APISMainMenu.prefab` 和 `TodayWhereToSit.unity` 中替换背景，未创建另一套菜单。
- 使用实际附件 `C:/Users/stella/Downloads/DDAY_MainMenu_Final.png`，1677×938，原文件与导入文件 SHA256 完全一致。
- 原图中的 D-DAY、APIS、四个按钮和人物保持原样。移除旧手写 Logo 显示对象及独立标题/按钮文字，旧素材保留。
- 四个透明 Button 复用原开始、继续、设置、退出回调。坐标按图片比例归一化；背景和点击区共用 Cover 变换。
- 悬停使用 0.2 秒暖色渐变高亮，按下使用 0.1 秒淡阴影；特效不拦截射线，不移动或缩放画中的纸片。
- 1 秒入场淡入，约 1.2% 缓慢镜头呼吸和轻微暖色变化；开始/继续保留 0.8 秒淡出及原游戏入口。
- 修复原场景中旧 LayoutGroup 遗留的主菜单 RectTransform 覆盖值，保持 DDayGame 绑定。

## 修改文件

- 新增 `Assets/Art/MainMenu/APIS/DDAY_MainMenu_Final.png` 和 Unity `.meta`。
- 修改 `Assets/Scripts/DDay/DDayMainMenu.cs`。
- 修改 `Assets/Scripts/DDay/DDayPaperButton.cs`。
- 新增 `Assets/Scripts/DDay/DDayPaperHighlight.cs` 和 Unity `.meta`。
- 修改 `Assets/Editor/DDayMainMenuAuthoring.cs`。
- 修改 `Assets/Editor/DDayMainMenuVerification.cs`。
- 保存 `Assets/Prefabs/DDay/APISMainMenu.prefab` 和 `Assets/Scenes/TodayWhereToSit.unity`。
- 验证证据存于本目录；修改前备份位于 `Backups/FinalCover_20261013/`。

## 实际验证

- Unity 6000.0.62f1 编译完成，最终 Console 错误数为 0，原场景已保存。
- 原场景 Play Mode 自动检查 157 项通过，详见 `Results.txt`。
- 实际渲染并检查 1920×1080、1600×900、1280×720、1024×768、2560×1080 截图。非 16:9 居中等比显示，以黑色留边保留构图。
- 实际 UGUI 射线命中四个对应按钮，验证最大呼吸缩放下仍对齐；背景和特效不拦截点击。
- 自动派发 UGUI 鼠标/键盘事件验证悬停、按下、恢复；`Time.timeScale=0` 仍可运行。
- 开始进入一周目正式 Day1，现有教程启动；继续读取隔离测试存档中的二周目 Day4 和记录册数据；设置打开现有界面。
- 验证重复点击防护、单次点击 SFX、菜单 BGM 不因刷新/缩放重启、音频服务仅一份。
- 测试使用 `DDAY_SAVE_OVERRIDE` 隔离存档，未以正式玩家存档进行测试。
- 对修改前记录的 218 个玩法、教程、剧情、音频和既有美术文件核对 SHA256，均未改变。

## 人工检查范围

- 尚未进行真实硬件鼠标/手柄操作和音乐听感检查；自动测试覆盖的是 Unity 射线及事件回调。
- Editor 忽略 `Application.Quit()`；已验证原退出回调及存档保留，独立程序的实际关闭仍需人工试玩。

源封面 SHA256：`a7da908e98266907a9f54fff6168835e8fb12e3d038deb2a4807e89335c00638`。
