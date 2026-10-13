# 一周目 Day4 / Day6 B2 漫画接入

主工程：`E:\聚光灯`。Unity 6000.0.62f1，运行场景：`Assets/Scenes/TodayWhereToSit.unity`。最终 Play 验收完成于 2026-10-12 13:22:12。

## 实际完成

- 使用本轮两张原图，Day4 七格、Day6 九格，按真实斜边与黑色分格边框配置，未按等宽网格切割。
- 复用 Day1/Day2 的整页播放器：整页固定、当前格高亮、其余格变暗、0.24 秒交叉淡化、渐收柔光聚焦、点击或空格推进、最后 0.85 秒淡出。
- Day4 宽幅原图等比居中；Day6 保持 3:2 比例。没有拉伸分格或生成新漫画。
- Day6 同一格可连续显示多段原对白，换对白时不会重新触发聚焦或音效。姓名与继续提示同排，下方保留完整两行文字高度。
- Day4 播放后进入 Day5；Day6 播放后进入 Day7。中途保存/继续恢复准确对白，播放完成后读档不重复播放旧 B2。

## 分镜配置

| Day4 顺序 | 画面 / 内容 | 表现 |
|---|---|---|
| 1 | 9月10日下班后的城市 | 环境柔光 |
| 2 | 老板公布晋升名单 | 人群焦点、环境声 |
| 3 | 宣布 NARCIS 晋升 | 老板焦点、纸张声 |
| 4 | NARCIS 的反应 | 静默反应镜头，不添加对白 |
| 5 | 要求 ECHO 招聘员工 | 白板焦点、纸张声 |
| 6 | 同事反应 | 静默反应镜头、略降亮度 |
| 7 | 9月12日聚餐通知 | 日历焦点、最后淡出 |

Day4 晋升、招聘和聚餐通知使用配置表现有文字；两格反应镜头保持无对白，不编造人物回应。

| Day6 顺序 | 画面 / 内容 | 表现 |
|---|---|---|
| 1 | 聚餐地点 | 环境柔光、低音量环境声 |
| 2 | 庆祝晋升 | 人群焦点，按原对白交流 |
| 3 | ECHO 与同事交谈 | 同一格连续播放原对白 |
| 4 | 谈论 SISY | 人群略降亮度，回忆区域柔光；SISY 仅在回忆中 |
| 5 | 聚餐结束 | 原对白结束聚餐 |
| 6 | CASSIA 离职 | 原离职对白与信件说明、纸张声 |
| 7 | THANE 丢弃信件 | 信件焦点、纸张声 |
| 8 | THANE 致电老板 | 电话焦点、拨号声、原通话内容 |
| 9 | 通话尾声 / 老板背影 | 原 THANE 停留旁白、最终淡出，不新增老板对白 |

Day6 的正式对白与动作文字按原顺序完整保留，仅去掉三个章节标题。未提前执行 B1、离职或下一天任务。

## 修改文件

| 文件 | 用途 |
|---|---|
| `Assets/Resources/DDay/Story/Day4/Day4ComicPage.png`、`.meta` | 原附件完整复制、等比无压缩导入 |
| `Assets/Resources/DDay/Story/Day4/Day4ComicPresentation.json`、`.meta` | 七格边界、焦点、文字、效果 |
| `Assets/Resources/DDay/Story/Day6/Day6ComicPage.png`、`.meta` | 原附件完整复制、无压缩导入 |
| `Assets/Resources/DDay/Story/Day6/Day6ComicPresentation.json`、`.meta` | 九格边界与原 B2 对白行映射 |
| `Assets/Scripts/DDay/DDayComicPage.cs` | 共用播放器增加两个事件配置；支持同格多句、等比显示与旧文字存档位置映射 |
| `Assets/Scripts/DDay/DDayDialogue.cs` | 使用当前对白的分镜索引读取画面，保留原推进逻辑 |
| `Assets/Scripts/DDay/DDayComicUI.cs` | Day6 显示原对白与角色名，修复两行文字容器高度 |
| `Assets/Editor/DDayArtImport.cs` | 仅针对新两图的导入菜单 |
| `Assets/Editor/DDayABPlayVerification.cs` | 新分镜、文字、三种分辨率、存档与原流程回归验收 |
| `Tools/dday_day4_day6_pages.py` | 生成独立演出 JSON，不写 Excel、ABConfig、图片或 Day1/Day2 配置 |

修改前脚本和保护文件哈希保存在 `Backups/Day4Day6PageComic_Before_20261012/`。

两张图片与原附件 SHA256 一致，保留原生尺寸：Day4 1677×938，Day6 1536×1024。导入使用 Point、Clamp、NPOT None、无压缩、无 Mipmap。`配置表AB.xlsx`、`ABConfig.json`、原通用 `ComicPresentation.json`、Day1/Day2 图片与配置、`DDayCampaign.cs` 均经修改前后哈希核对未变化，见 [来源审计](Day4Day6PageComic_SourceAudit.json)。未修改旧厕所游戏文件。

## 实际验收

[Play Mode 结果](Day4Day6PageComicVerification/Results.md)：最终 **PASS，1269 条 PASS 记录，0 条文字容器失败**。

| 检查 | 结果 |
|---|---|
| Day4 七格、Day6 九格按序播放 | 通过 |
| 整页比例、分格高亮、轻量效果和音效触发 | 通过 |
| 每段 Day4/Day6 文字容器尺寸 | 通过；Day6 原对白逐段测量 |
| GameView IMGUI 鼠标点击 / 空格 | 实际事件推进通过；其他对白调用同一运行时输入接口 |
| Day4 → Day5，Day6 → Day7 | 通过 |
| 中途保存/继续、完成后不重播 | 通过 |
| Day1/Day2 漫画回归 | 通过 |
| 双周目 Day1–Day7、12个 A1 排座日、A2/B1、HE/BE | 原有回归用例通过 |
| 1920×1080、1600×900、1280×720 | 每格均保存 Game 视图截图；Day4 21张、Day6 27张 |
| 编译与最终 Console | 编译完成，Error=0，Warning=0，见 [Editor 状态](Day4Day6PageComicVerification/FinalEditorState.json) |

已人工查看新漫画各格的代表截图，确认亮暗范围、文字留白、图像比例。第一轮捕获到 Day6 姓名/两行对白/继续提示高度不足，修复后完整重跑通过；第一轮失败报告保留为 `Results_FirstRun_TextHeight.md`。

### 运行截图

Day4：

![Day4 聚餐通知](E:/聚光灯/Docs/Day4Day6PageComicVerification/B2_Day4_Day4_P07_party_date_1600x900.png)

Day6：

![Day6 聚餐对白](E:/聚光灯/Docs/Day4Day6PageComicVerification/B2_Day6_Day6_P02_celebration_1920x1080.png)

## 限制

- 人群、纸张、拨号等声音继续使用共用播放器已有的合成占位音效；尚无正式录音，未做人工听感验收。
- 测试注入的是 Unity GameView IMGUI 事件，未注入 Windows 硬件鼠标/键盘，也未人工拖拽完整双周目。
- 本轮未重建 Windows EXE。
