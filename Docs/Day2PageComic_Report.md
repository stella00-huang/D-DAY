# 一周目 Day2 B2 七格漫画接入

验收时间：2026-10-12 11:56:57。主工程：`E:\聚光灯`，Unity 6000.0.62f1，场景：`Assets/Scenes/TodayWhereToSit.unity`。

## 本轮图片

采用最后提供的 `codex-clipboard-991fd39c-12dc-49b2-973b-f1aa37006ff2.png`。原文件未修改；导入文件与原附件 SHA256 完全一致：

`3C970B83C70029AC519B867FE46A91DC706469AE9ED57E2B19F5C430350A9F7C`

资源：`Assets/Resources/DDay/Story/Day2/Day2ComicPage.png`，保留 GUID。原生 1536×1024，Point、Clamp、无压缩、无 Mipmap。上一张 Day2 图片和对应配置留存于 `Backups/Day2PageComic_Before_20261012/`。

## 修改文件

| 文件 | 修改 |
|---|---|
| `Assets/Resources/DDay/Story/Day2/Day2ComicPage.png`、`.meta` | 最新整页漫画与导入设置 |
| `Assets/Resources/DDay/Story/Day2/Day2ComicPresentation.json`、`.meta` | Day2 独立七格边界、焦点、原定旁白与轻量效果 |
| `Tools/dday_day2_page.py` | 可重复生成 Day2 演出配置；不写 Excel、ABConfig、Day1 或图片 |
| `Assets/Scripts/DDay/DDayComicPage.cs` | 共用播放器按事件选配置，遮罩缓存区分 Day1/Day2，支持回忆局部柔光、冷色、低亮度与纸张声 |
| `Assets/Scripts/DDay/DDayDialogue.cs` | 一周目 Day2 B2 读取七格演出，复用原输入和最终淡出 |
| `Assets/Scripts/DDay/DDayCampaign.cs` | 存档保存当前漫画配置 ID；恢复对应分镜，兼容旧版文字进度 |
| `Assets/Editor/DDayArtImport.cs` | Day2 图片导入菜单 |
| `Assets/Editor/DDayABPlayVerification.cs` | Day2 七格、读档、Day3 推进、三种分辨率及 Day1/双周目回归；等待真实尺寸和输入保护帧 |

## 七格配置

完整页面位置及比例固定。依据新图的黑色边框逐格配置多边形，斜边不按等宽网格切割；当前格高亮，其余格以约 54% 黑色遮罩变暗，0.24 秒交叉淡化。沿用 Day1 的渐收柔光聚焦，未将单格放大全屏。

| 顺序 | 内容 | 焦点与效果 |
|---|---|---|
| 1 | 警方问询 ORIN | 左上格、ORIN 面部、冷色与低音量雨声 |
| 2 | 老板单独问询 | 上中格、右侧 ORIN、渐收柔光 |
| 3 | 恋情公开与回忆 | 右上格、ORIN 表情及 SISY 回忆区域柔光 |
| 4 | 同事安慰 | 中左格、人群焦点及轻量环境声 |
| 5 | 宣布明日重新分工 | 中右格、老板与白板、纸张声；不执行 Day3 分配 |
| 6 | 同事失落 | 左下格，包含新图右侧 ORIN，略降亮度 |
| 7 | ECHO 压力 | 右下格、ECHO 面部、纸张声，点击后 0.85 秒淡出进入 Day3 |

旁白使用用户约定的七段文字，不加编号、不重复播放旧版 Day2 B2。SISY 只在第三格回忆中出现，第五格白板兔子图案是工作安排图示；Day2 现实办公室的 SISY 对象保持缺席。

## 实际验收

`Docs/Day2PageComicVerification/Results.md` 最终为 PASS，844 条断言。Day2 七格每格保存 1920×1080、1600×900、1280×720 的 Game 视图截图，共 21 张；已人工查看全部七格截图。

- Day1 与 Day2 各七格依次播放，图片尺寸与高亮范围正确。
- GameView IMGUI 鼠标与空格事件实际推进分镜；其余镜头调用同一运行时输入接口。未注入 Windows 硬件鼠标/键盘。
- Day2 白天 B1 不出现 B2 漫画；任务完成后自动转入固定剧情，无工作完成确认页。
- 漫画中途保存/继续恢复准确分镜和任务负责人；完成后进入 Day3，读档不重播 Day2。
- Day2 宣布分工不预先分配 Day3 任务。
- 两周目顺序进入 Day1–Day7、其他固定剧情、生还条件与 HE/BE 分支通过回归。
- A2、记录册未读、手动/自动存档及任务交互回归通过；所有截图文本容器检查通过。
- 最终编译完成，本次最终运行 Console Error=0、Warning=0。

第一轮测试因 Game 视图切换尺寸未稳定中止，第二轮因测试输入保护帧时序中止，均保留失败报告于备份目录；修正测试等待后重跑通过，没有更改游戏输入规则。

## 保留与限制

Excel、ABConfig、Day1 图片及分镜配置经 SHA256 核对未改动：`Docs/Day2PageComic_SourceAudit.json`。未修改旧厕所游戏，未改其他日期配置，未新增玩家按钮。

雨声、人群声、纸张声沿用共用播放器的低音量合成占位音效，尚无正式录音素材。声音触发与 AudioSource 播放已验证，未进行人工听感验收。本轮未重建 Windows EXE。
