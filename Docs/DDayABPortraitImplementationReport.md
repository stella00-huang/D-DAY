# D-DAY AB 与立绘对话定向修复报告

## 主版本与保护范围

- 当前 Editor：`E:/聚光灯`，Unity `6000.0.62f1`；运行场景 `Assets/Scenes/TodayWhereToSit.unity`。
- 现有隔离副本实际位于 `E:/聚光灯/_DDayValidationProject`；用户此前提到的 `E:/聚光灯_DDayValidationProject` 本机未找到。本轮只同步已有副本，不新建项目。
- 修改前备份：`Backups/DDayAB_BeforeImport_20261011`、`Backups/DDayA1A2Bubble_BeforeFix_20261011`；副本备份：`Backups/DDayValidationBeforeABPortrait_20261011`。
- 原厕所游戏、办公室背景和动物贴图未改动；现有场景和 Prefab 文件未改写。本轮 UI 沿用现有运行时构建方式。
- Excel 原文 SHA256：`97e00e7a2755e8f81433db1e92ccd6069a5ce75d90e6e5e606fed919e21cdffe`；接入后仍一致。

## 四表审计与接入

| 系统 | 读取/接入范围 | 逻辑 |
|---|---|---|
| A1 | 12 个排座日、全部在场员工条件 | 全部满足且手动确认后才能开启 B1 |
| A2 | 40 个唯一事件/记录 ID | 周目、日期窗口、空间条件、前置事件、领取状态检查；主动点击气泡后播放，结束才记录 |
| B1 | 22 行原任务配置，按任务合并角色分支 | 分配负责人后立即通过共享立绘界面反馈；返回任务面板；固定 ORIN 问询不作为任意负责人任务 |
| B2 | 10 个有原文的固定事件 | 工作结束后播放；Day6 聚餐在白天工作后，二周目结盟在聚餐后；Day7 无排座/B1 |

## A1 空间定义与求解结果

2×4：前排 A1–A4，后排 B1–B4。上下左右相邻，曼哈顿距离 1；靠近人物 ≤2；远离人物 ≥3。
一周目 Day1 SISY 固定 B3；Day2–6 SISY 缺席，B3 保留为空，所以此阶段是 6 人/8 桌（两个空位），没有补入幽灵员工。

| 功能/属性 | 格子集合 |
|---|---|
| 边缘 | A1、A4、B1、B4 |
| 中心 | A2、A3、B2、B3 |
| 咖啡区 | A1–A4 |
| 远离咖啡 | B1、B4 |
| 打印区 | A3、A4、B3 |
| 休息区 | B1、B2、B3 |
| 老板区 | B2、B3、B4 |
| 远离老板 | A1、A2、A3、B1 |

原范围：休息区 B1/B2、老板区 B3/B4，在多个日期无解。本轮仅分别扩展到 B3、B2，使影响范围可重叠；没有移走设施、删条件、改 Excel 或减少在场员工。细节及基线无解天见 `DDayABA1Audit.json`。

初始布局为独立随机打乱后导出的可复现预设（种子 20261011+周目×100+日期）；验证至少 3 条未满足，不使用求解器示例答案作为初始局面。

| 周目/Day | 合法解数 | 初始未满足条数 | 一个可行解（A1 A2 A3 A4 / B1 B2 B3 B4） |
|---|---:|---:|---|
| 1/1 | 44 | 4 | ORIN · ECHO · THANE · METIS / CASSIA · NARCIS · SISY · 空 |
| 1/2 | 8 | 5 | ORIN · THANE · METIS · 空 / CASSIA · NARCIS · 空 · ECHO |
| 1/3 | 5 | 4 | CASSIA · THANE · METIS · ORIN / 空 · NARCIS · 空 · ECHO |
| 1/4 | 12 | 4 | ECHO · METIS · THANE · 空 / CASSIA · NARCIS · 空 · ORIN |
| 1/5 | 2 | 6 | ORIN · THANE · METIS · 空 / CASSIA · NARCIS · 空 · ECHO |
| 1/6 | 17 | 3 | ORIN · THANE · METIS · 空 / CASSIA · NARCIS · 空 · ECHO |
| 2/1 | 12 | 8 | SISY · THANE · 空 · ECHO / ORIN · CASSIA · NARCIS · METIS |
| 2/2 | 2 | 5 | SISY · ECHO · THANE · METIS / ORIN · NARCIS · CASSIA · 空 |
| 2/3 | 84 | 5 | SISY · ORIN · THANE · METIS / 空 · NARCIS · ECHO · CASSIA |
| 2/4 | 8 | 5 | SISY · METIS · THANE · ECHO / 空 · ORIN · CASSIA · NARCIS |
| 2/5 | 139 | 6 | SISY · NARCIS · METIS · CASSIA / ECHO · ORIN · THANE · 空 |
| 2/6 | 524 | 3 | SISY · ORIN · NARCIS · METIS / THANE · ECHO · CASSIA · 空 |

Python 穷举和 Unity 中的 C# 完整规则穷举结果一致。

## 立绘、表情与共享对话

- 新增 `DDayDialogue.cs`：集中立绘资源、逐句演出数据、说话者、表情 ID、左右位置、淡入、分页与输入防穿透状态。
- 新增 `DDayDialogueUI.cs`：下方统一 VN 对话框；单人当前立绘，双人左右立绘、非说话者变暗；按发言顺序切换。
- A2 气泡 → 对话 → 记录；B1 选负责人 → 对话 → 任务面板；B2 工作结束 → 顺序对话 → 日期推进。
- 每句默认 neutral；显式配置表情，不按关键词猜情绪。原文描述性段落标为旁白，通过 focusId 展示对应人物，不编造角色对白。
- 字幕逐字显示，第一次输入显全文，第二次输入下一句；长原文分页但不替换文案。对话期间拖拽/底部操作隐藏或禁用，结束后有帧级输入保护。
- `DialoguePresentation.json` 共 7 个角色配置、348 条明确演出配置。七种表情 ID 可配置，但现有真实素材只有每人 neutral+一种反馈表情；剩余 35 个组合回退 neutral。
- 详细素材矩阵见 `DDayPortraitResourceAudit.md`。没有把同一贴图登记为七种“已完成”表情，也没有新增/改画办公室人物。

## UI 修复

- A2 气泡使用 UGUI，每只动物最多一个，跟随可见头部锚点；修正透明贴图留白导致的悬浮过高。
- 右侧任务/座位规则面板移到打印机下方；规则条目按实际文本高度排版，允许滚动，不裁切长条件。
- 底部记录册/存档/设置/开始工作位于一个 HorizontalLayoutGroup，统一间距 24、统一高度，主操作略宽。
- 卫生间 Hover 和调试提示删除，原背景保留；休息/咖啡/打印/老板 Hover 保留。
- 任务名称与负责人分成独立文本行，避免负责人被按钮裁切。

## 修改文件

| 文件 | 作用 |
|---|---|
| Assets/Scripts/DDay/DDayABConfig.cs | 四表数据类型、周目/日期/事件查询 |
| Assets/Scripts/DDay/DDayGame.cs | 未解初始局面、确认/任务门槛、拖拽、统一对话入口、负责人反馈 |
| Assets/Scripts/DDay/DDayLogic.cs | 2×4 图与功能区规则 |
| Assets/Scripts/DDay/DDaySeatExploration.cs | A2 只检测/气泡点击/完成记录/前置 B2 状态 |
| Assets/Scripts/DDay/DDayCampaign.cs | AB 每日任务与固定事件、三项生还条件、两次 YES、存档兼容 |
| Assets/Scripts/DDay/DDayArt.cs | 可见头部锚点，不修改图片 |
| Assets/Scripts/DDay/DDayOfficeHud.cs | UGUI 气泡、单组底部按钮 |
| Assets/Scripts/DDay/DDayUI.cs | 打印机避让、规则文字、任务文字、统一对话渲染入口 |
| Assets/Scripts/DDay/DDayDialogue.cs | 统一逐句对话/立绘配置与播放状态 |
| Assets/Scripts/DDay/DDayDialogueUI.cs | VN 立绘、表情、输入反馈 |
| Assets/Resources/DDay/ABConfig.json | 完整 Excel 导出，原文不改 |
| Assets/Resources/DDay/DialoguePresentation.json | 独立演出配置，不改 Excel |
| Tools/dday_ab_solver.py | 全排列规则求解与无解审计 |
| Tools/dday_ab_export.py | 全量四表转换、初始局面验证 |
| Tools/dday_dialogue_presentation.py | 可重复生成演出配置及缺失素材报告 |
| Assets/Editor/DDayABPlayVerification.cs | 主工程真实 Play Mode 回归、独立存档、运行截图 |

## 验收证据与边界

运行证据：`Docs/ABPlayVerification/Results.md`；主要截图同目录。
已执行：Unity 主工程导入编译；12 日 C# 穷举；双周目 Day1–Day7；移动交换/空位模型、A1门槛；UGUI PointerClick 气泡事件；A2记录/去重/条件失效；B1立绘与改派；B2推进/聚餐；三项生还判定；15槽框架中的槽1保存/覆盖、继续；四个 YES/NO 组合及 HE；14 个真实立绘组合加载；双人切换；文本输入处理器先显全文再推进。
真实 Game 视图截图约1190×669（16:9），已人工检查立绘与文字分离、打印机可见、底部统一排列和气泡位置。
未验证：操作系统级真实鼠标拖拽、悬停和物理空格键。GameView.SendEvent 合成键事件未进入 Player，失败尝试保存在 `SyntheticGameViewInput_NotSupported.md`；未把它标为 PASS。当前通过的是共享输入处理器及 UGUI PointerClick 回调测试。
未验证：其他窗口 Scale / 非16:9、完整35种缺失表情的演出、隔离副本重新导入、最新 Windows 构建。此前 BatchMode 遇到许可客户端失败，所以没有宣称 BatchMode 编译通过。

## 从标题演示

1. 在 `TodayWhereToSit.unity` 点 Play → 开始游戏 → 读每日简报。
2. 拖动/交换员工；可点击头顶气泡看立绘对白，完成后记录册增加。
3. 对照右侧条件完成 A1 → 确认座位 → 分配任务 → 选负责人 → 看反馈后回任务面板。
4. 返回办公室 → 开始工作 → 下班 → B2 固定剧情 → 下一天。A2 不要求集齐。
5. 一周目 Day7 选择促成因素并重看第一天，进入二周目。
6. 二周目 Day1 同时满足 ORIN/SISY相邻、METIS远离咖啡、不分配任务给SISY；之后继续到 Day7，两次 YES 为 HE，其余为 BE。

## 剩余素材

35 个表情变体未制作。完整缺失列表与已实现的每个角色表情见独立素材报告。无需新增对白或替换办公室角色即可继续补图。
最终主工程 Play Mode：338 项检查 PASS；Console 新增 Error 0、Warning 0。测试完成后已退出 Play Mode。

