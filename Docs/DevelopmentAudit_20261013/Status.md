# D-DAY 实际开发状态与本轮交付

工程：`E:/聚光灯`。Unity：6000.0.62f1。原场景：`Assets/Scenes/TodayWhereToSit.unity`。
检查依据：实际 C#、原场景/Prefab、Resources 配置和素材，以及真实 Play Mode 结果；不按聊天指令数量判断完成度。

## 最近任务状态

| 任务 | 实际状态 | 证据 / 剩余事项 |
|---|---|---|
| APIS 分层主菜单、红色手写 Logo | 已完成 | 原背景、四个独立纸片按钮、Sprite Swap、悬停/按下动画、0.8秒进入办公室转场。顶部 TMP 标题和制作组对象已删除。主菜单 Play Mode 77项断言通过。 |
| 七首 BGM 与 Kenney SFX | 已完成接入 | 配置/资源/路由均存在；本轮89项 Play Mode检查通过。包含日期、双周目、结局单次播放、主菜单恢复、拖拽成功/失败/取消、独立音量保存。听感需人工试听。 |
| 双结局动态插画、正式旁白、黑幕标题分离 | 已完成 | 两组各三张正式插画及共用播放器存在；完整实时演出复验通过。开场标题各只出现一次，正文与标题分离；玩家界面无HE/BE标签；结束只显示D-DAY和返回菜单。自动播放HE约94.87秒、BE约99.82秒。 |
| Day7两次选择及第二次“是”移动 | 已完成代码与运行验证 | 两次YES进入HE，其余路径BE；第二次YES移动、停顿、减速、边界、实际Raycast和键盘Submit通过。原生鼠标追逐手感未人工试玩。 |
| 一周目Day1新手教程 | 本轮完成 | 直接进入正式Day1，SISY引导真实操作，配置化台词、公开角色介绍、真实排座/分配、工作前记录册、跳过和存档恢复。75项不同的运行检查通过，12张截图。 |
| 最新Excel内容接入、A1/A2/B1/B2 | 已接入 | 上轮原场景完整验证：A2 40条、B1 24记录、B2 10条；1116项运行检查、563项静态检查。原表和运行剧情配置本轮哈希不变；本轮复验Day1，不宣称重新测完全部40条。详见ContentUpdate_20261012。 |
| 办公室操作栏/设置/任务区、记录册、UI视觉对齐 | 已完成主要调整 | 当前画面四个底部按钮、直接显示座位要求、记录册两栏、物品/情报数量、任务头像、真实未读状态；后续VisualAlignment验收已覆盖较早UISimplification报告中的文本溢出失败。 |
| 一周目Day1/2/4/6整页漫画、Day7嫌疑选择头像 | 已接入 | 实际资源和ForEvent映射存在；历史Play验证通过。本轮重新完整播放Day1，之后正常Day2；七嫌疑对象及14张正常/诧异头像仍在原目录。 |
| 二周目Day1/2/4/6用户提供的整页漫画 | 尚未开始整页接入 | 当前ForEvent只绑定一周目四页。二周目已有固定剧情与立绘演出，但这四张新整页漫画未导入/绑定；不能标为漫画完成。 |
| 全套七种角色表情 | 部分完成 | 对话系统支持七种ID，但实际普通立绘/反馈素材只有14个角色-表情组合，35个缺失组合回退neutral。Day7嫌疑头像为独立选择素材。详见DDayPortraitResourceAudit.md。 |
| 美术导出ZIP | 已完成当时快照，尚未更新近期资源 | 现有Exports/D-DAY_Art_Assets.zip包含71个美术文件，完整性报告存在；后续新增主菜单/结局等资源未重新打包。 |

### 仍需策划确认的旧内容

- B1二周目Day4历史资料没有ECHO专属剧情，沿用已有简短通用反馈；未编造。
- 一周目Day7表列四名促成者，已确认游戏界面保留七个嫌疑选项和原结局；本轮未改名单。
- JD的9月5日日期已按用户确认保留；A2四条原空白记录册正文已按授权补充。
- 本轮的教程适配已获明确确认，无新的教程设计冲突。

## 本轮修改文件

### 主菜单

- `Assets/Art/MainMenu/APIS/D-day_Logo.png`及Unity生成的配套.meta：源PNG已经透明，按原字节导入；Sprite区域仅排除外围透明留白，保留完整字形和长笔画。
- `Assets/Scripts/DDay/DDayMainMenu.cs`：四按钮、Logo淡入/轻微亮度呼吸，保留原进入/继续转场。
- `Assets/Editor/DDayMainMenuAuthoring.cs`：增量更新原Prefab和场景，不重建背景/音频管理器。
- `Assets/Prefabs/DDay/APISMainMenu.prefab`、`Assets/Scenes/TodayWhereToSit.unity`：正式引用已保存。
- `Assets/Editor/DDayMainMenuVerification.cs`：更新为四按钮及红色Logo验收。

### Day1教程

- 新增 `Assets/Scripts/DDay/DDayTutorialController.cs`：事件驱动步骤、输入限制、45秒无操作提示、跳过、真实发现提示。
- 新增 `Assets/Scripts/DDay/DDayTutorialUI.cs`：复用原DDayUI绘制SISY说明、公开头像、实际目标高亮、设置中的静态玩法回顾。
- 新增 `Assets/Resources/DDay/Tutorial/Day1Tutorial.json`：全部教学文案和步骤链。
- 修改 `Assets/Scripts/DDay/DDayGame.cs`：角色/座位/任务/记录册操作事件；复用原B1反馈函数；首次Day1进入教学。
- 修改 `Assets/Scripts/DDay/DDayCampaign.cs`：原CampaignSave增加tutorial字段；工作与真实获得事件；保存/恢复教程与Day1未播完B1反馈，恢复Day1工作播放。
- 修改 `Assets/Scripts/DDay/DDayOfficeHud.cs`、`DDayUI.cs`：复用原按钮和气泡，限制无关点击；已有弹窗打开时隐藏相冲突的教学提示。
- 新增 `Assets/Editor/DDayTutorialAuthoring.cs`：把同一个控制器保存到原场景。
- 新增 `Assets/Editor/DDayTutorialVerification.cs`：隔离存档的真实Day1流程、恢复、跳过及渲染验收。

### 验收与文档

- `Assets/Editor/DDayDualEndingVerification.cs`：只修正Game视图分辨率切换后的测试等待，不改结局播放器或动画。
- 本目录审计、保护文件哈希，以及四个Verification目录的运行记录与截图。
- 备份：`Backups/FinalMenu_20261012_234635`、`Backups/Tutorial_20261013`。

## 教程实际步骤与保存

| 阶段 | 实际完成条件 |
|---|---|
| T00/T01 | SISY开场与真实今日任务说明，鼠标/空格推进。 |
| T02 | 点击至少一名真实员工头像，显示原公开介绍；已读红点按位保存，不强制全看。 |
| T03 | 介绍实际工位、全部座位要求及SISY固定B3。 |
| T03a | ORIN发生真实移动，并满足正式边缘规则；高亮从同一个判定器和世界坐标计算。 |
| T03b | 自由调整，全部正式A1条件满足；可选气泡仍可探索，未探索不阻塞。 |
| T04 | 从原任务区点击负责人，播放完整正式B1反馈后返回原任务面板；无新增确认分工。 |
| T08 | 开始工作前打开记录册，查看物品/情报两个分类并关闭；空记录册也可完成。 |
| T09/T05 | 简短回顾后点击原开始工作按钮，且原工作流程真实启动。 |
| T06 | 教学遮罩隐藏，原工作正常结束时保存done；原下班转场与18:15 B2自行继续。 |

A2首次发现只监听真实入册事件，短暂提示；不发放虚构线索。纯说明可以空格推进，操作步骤不能空格代替。跳过只保存done并解除教学限制，原座位、任务和日期不变。

状态跟随原自动存档和15个手动存档槽，包含步骤/句子、已查看员工、已查看分类、真实拖拽完成、首次发现与done。Day1未结束的正式B1反馈保存句索引和任务ID，恢复时不重新分配、不重复奖励。旧存档兼容，Day2及二周目均不启动教程。

与附件的适配：现有右侧任务区替代公开栏，点选负责人替代拖任务卡；记录册提前到开始工作前；没有效率结算或工作结束教学弹窗。已获用户确认。

## 本轮真实验收

- 原Unity编辑器实际编译、Play Mode运行。主菜单77项断言（67条不同检查）、音频89条、双结局97条不同检查、教程75条不同检查均PASS。
- 主菜单和双结局：1920×1080、1600×900、1280×720实际Game View。教程：1280×720及1920×1080；角色/记录册/任务/B1/工作/18:15/B2均实际截图并人工查看。
- 教程Space事件实际通过GameView.SendEvent；拖拽使用真实释放回调，按钮使用真实UGUI监听；没有宣称硬件鼠标操作通过。
- 中途保存恢复：角色红点、自由排座、记录册分类、B1具体反馈句、工作阶段；开场/排座/末尾/B1播放中的跳过均通过。
- 原Day1仍44个座位解；没有新教学任务、奖励或独立教学关卡。
- 原B2正常播放至Day2；SISY缺席和B3空置正常，漫画照片没有自动加入记录册。
- Excel、正式剧情、漫画/结局配置、HEEndingPlayer、DDayFinalDecisionUI及音频等98项保护文件SHA256均不变。
- 正式存档没有用于测试；测试使用Docs内独立DDAY_SAVE_OVERRIDE路径。原场景退出Play后无未保存修改。

运行报告：`Docs/MainMenuVerification/Results.txt`、`Docs/AudioIntegration/Results.txt`、`Docs/DualEndingVerification/Results.txt`、`Docs/TutorialVerification/Results.txt`。

## 预览与人工检查

- 打开原TodayWhereToSit场景，Play后点击开始游戏即可进入真实Day1教程。
- 开发验收入口：`D-DAY > Tutorial > Verify Real Day1 in Play Mode`；不会显示在正式玩家UI里。
- 人工检查：实体鼠标拖拽手感、第二次YES追逐手感、歌曲听感/循环接缝、Windows独立运行版退出行为。
- 二周目整页漫画与缺少的表情属于旧待完成任务；本轮按最新范围只完成主菜单/教程并复验音频和结局，未擅自批量改造其他日期。
