# D-DAY 最新内容映射审计

## 来源与范围

- 权威输入：`C:/Users/stella/Desktop/配置表AB.xlsx`（本次附件；请求正文称《配置表A.xlsx》）。完整读取四张工作表，未修改工作簿。SHA256：`46140348e0a3e983454f6d2d56bff4270c321d8211c4908254d63f5b59414c34`。
- 工程：`E:/聚光灯`；原场景：`Assets/Scenes/TodayWhereToSit.unity`。修改前备份：`Backups/ContentUpdate_20261012`。
- A2：40条。B1：24条（21项自由任务、1项固定 ORIN 问询、2条 Day7 无任务配置）。B2：10条，其中两条 Day7 为原有选择流程指令，不作为旁白显示。
- 完整原表和内部字段保存在 `WorkbookFull.json`；逐条 ID、窗口、前置条件、负责人映射与检查结果见 `MappingAudit.json`。

## 已接入

- A2 正式探索叙述／对白与记录册正文分离；有效位置才显示气泡；对话完成后入册。跨日窗口、晋升后状态、X 项目前置、首次领取和去重使用真实运行条件。
- “两周目”线索分周目保存领取状态；记录册沿用原来的跨周目唯一条目，不重复计数。旧存档的通用线索 ID 根据已获得条目的周目迁移；已获得正文更新，未获得内容保持隐藏。
- B1 长场景按实际负责人播放，逐句／分页保留原文。多任务不覆盖；任务 ID 保持兼容；一周目 Day2 的 ORIN 问询在工作结束后、B2 恋情公开前固定播放。
- B1 核心疑问、推理作用、策划备注未绑定任何玩家 UI；任务公开描述为空，任务界面只展示名称、负责人及分配状态。A1 设计理由也未导入玩家文本。
- 现有一周目 Day1／Day2／Day4 漫画绑定完整 Excel 正文；长段落在同一分镜内分页。原图、分镜范围、特效参数不变。Day6 原文仍沿用原有分镜行映射。
- 新漫画断点使用带内容版本的演出 ID；旧漫画存档按原分镜恢复。HE／BE 资源、动画参数、字幕原稿及第二次“是”逃跑交互保持原样。

## 用户确认与处理

- 二周目 Day5 招聘中的 JD 创建日期保持现有 **9月5日**。
- A2 工作表第12、20、21、39行的记录册正文原本为空；已获用户授权，根据对应正式对白补充简短记录，未增添新秘密。内容见导出脚本 `RECORD_SUPPLEMENTS`。
- A2 第14行末尾与第36行末尾、B1 第24行末尾含明确演出／设计指令，未当作玩家对白；原文完整留存在审计数据。
- A2 第29行注明 Day4 晋升后、可顺延 Day5；实际窗口设为 Day5，需先完成 Day4 B2。未提前播放晋升后对白。

## A1 规则与解数

最新 A1 与修改前配置一致：所有在场员工、规则、初始座位保持不变。共12天均有解；距离和区域集合、示例解详见 `A1Audit.json`。

| 周目 | Day1 | Day2 | Day3 | Day4 | Day5 | Day6 |
|---|---:|---:|---:|---:|---:|---:|
| 一周目 | 44 | 8 | 5 | 12 | 2 | 17 |
| 二周目 | 12 | 2 | 84 | 8 | 139 | 524 |

## 未完成／待确认

- B1 第22行（二周目 Day4 历史资料）没有 ECHO 专属剧情：允许分配，使用已有“已完成分配的工作。”，未擅自扩写。可由策划后续补充正式反馈。
- B2 一周目 Day7 表列出四名促成者，当前已确认 UI 保留七个嫌疑选项及原结局回溯。本轮不缩减选项，也不将选择判为被证实的直接凶手；如需缩减名单，须另行确认。
- 二周目 Day1／Day2／Day4／Day6 当前仍使用已有背景＋角色立绘的固定叙事，未发现正式整页漫画与播放器的绑定。本轮保持现有素材与演出，未新增或替换美术。
- 本轮验证 Day7 三条选择路径，不重跑完整 HE／BE 动画及真实鼠标追逐。此前结局验收曾记录 HE 完成后的继续游戏失败；本轮未修改结局演出，不能宣称该项已修复。

## 验收与复现

- 实际 Unity 6 Play Mode 最终结果：**PASS，1116项运行断言**。45次 A2 领取测试（40条、5条通用分别验收两周目），138次真实任务负责人分配，双周目顺序到达 Day7；三条结局选择路径正确。
- 563项静态原文／配置／文件保留检查通过。1920×1080、1600×900、1280×720 实际 Game View 任务面板均通过容器检查；共保存7张运行截图并人工查看。记录册阅读、未读保存、手动槽15、自动存档及新旧漫画断点通过。
- 最终 Unity 错误日志0条；测试结束后退出 Play Mode，原场景无未保存修改。HE／BE 完整动画及硬件输入仍按上述未验证项记录。
- 静态原文／配置／保留文件核对：运行 `Tools/dday_content_audit.py`；只读 Excel。`--update-directions` 仅同步 B1 逐句演出元数据，保留角色资源及 A2／B2 配置。
- 重新导入：先运行 `Tools/dday_ab_export.py`，再运行上述审计脚本及 `--update-directions`；不运行旧的全量立绘资源生成脚本。
- Unity 菜单：`D-DAY > Content > Verify Latest Workbook in Play Mode`。使用独立测试存档，检查40条 A2（通用线索两周目分别测试）、所有可分配角色反馈、双周目顺序、Day7 三条选择、自动存档、手动槽和漫画断点。
- 实际结果及截图：`PlayVerification/Results.md`、`PlayVerification/*.png`。运行测试调用真实游戏回调；未注入硬件鼠标／键盘，不宣称人工试玩通过。

## 本轮修改文件

- 数据：`Assets/Resources/DDay/ABConfig.json`、`DialoguePresentation.json`。
- 运行脚本：`Assets/Scripts/DDay/DDayABConfig.cs`、`DDayCampaign.cs`、`DDayGame.cs`、`DDaySeatExploration.cs`、`DDayDialogue.cs`、`DDayComicPage.cs`、`DDayUI.cs`。
- 导入／审计：`Tools/dday_ab_export.py`；新增 `Tools/dday_content_audit.py`。
- 编辑器验收：新增 `Assets/Editor/DDayLatestContentVerification.cs`；更新 `DDayABPlayVerification.cs`，同步 `DDayAudioVerification.cs` 的记录回调参数；`DDayDualEndingVerification.cs` 增加停止旧验收任务的开发菜单。
- 交付文档：本目录审计 JSON、审计说明、运行结果与截图。原场景、Prefab、角色图、漫画图及结局演出配置未改动。
