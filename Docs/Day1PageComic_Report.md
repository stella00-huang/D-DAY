# 一周目 Day1：整页七格漫画

## 接入

- 主工程：`E:\聚光灯`，Unity 6000.0.62f1，运行场景 `Assets/Scenes/TodayWhereToSit.unity`。
- 触发：一周目 Day1 B1 完成 → 原有下班淡入 → `B2_L1_D1` 整页漫画 → 末格淡出 → Day2。
- 使用本次明确提供的七段旁白，独立保存于演出配置；不修改 Excel 或 ABConfig，不重复播放旧 Day1 B2 文字。
- 完整漫画始终等比例显示，保留原分格和黑边。遮罩按真实斜边定义，不按等宽网格分割。旁白位于漫画下方。
- 每次点击或空格推进一格；不增加按钮、确认页或工作结束弹窗。
- 新存档保存演出版本与镜头位置；旧版本漫画存档按原文行位置迁移到相应新镜头。已存在的记录不删除。
- 地面照片是叙事视角，不在本段演出完成时自动授予记录册物品。

## 七格

坐标为原图 1536×1024 像素，左上为原点。完整四边形见 JSON。

| 顺序 | 真实边界范围 | 聚焦 / 效果 |
|---|---|---|
| 1 暴雨将至 | x8–1527，y5–296 | 窗外渐收亮光、短暂闪电、合成风声与雷声 |
| 2 SISY 疲惫 | x9–542，y308–634，右斜边 | 面部柔和亮光，环境压暗、降低风声 |
| 3 突然倒下 | x507–1051，y309–634，双斜边 | 0.26秒内衰减微震，最大2.3参考像素；撞击声与短暂环境静默 |
| 4 照片掉落 | x1011–1526，y309–634，左斜边 | 风声、地面与纸张局部亮光，无原画改动 |
| 5 大学旧照 | x9–489，y644–1015，右斜边 | 只高亮地面照片；原图人物保持暗部，无拾取/发现动作 |
| 6 紧急求救 | x437–1042，y644–1015，双斜边 | ORIN 快速柔光聚焦、拨号声、低音量合成环境声 |
| 7 救护车 | x1032–1526，y645–1015，左斜边 | 红蓝柔和光晕、雨声/远处警笛；点击后0.85秒淡出，进入Day2 |

轻微聚焦使用亮光范围变化，完整漫画不放大裁切。镜头遮罩以0.24秒交叉淡化切换。

## 修改文件

| 文件 | 内容 |
|---|---|
| `Assets/Scripts/DDay/DDayComicPage.cs`（新增） | 分镜数据、整页绘制、斜边遮罩、简单特效、音效替换接口与合成占位音 |
| `Assets/Resources/DDay/Story/Day1/Day1ComicPage.png`（新增） | 附件的原始字节副本 |
| `Assets/Resources/DDay/Story/Day1/Day1ComicPresentation.json`（新增） | 七格边界、焦点、旁白、效果与音效ID |
| `Assets/Scripts/DDay/DDayComicUI.cs` | 接入整页绘制和下方旁白，统一点击/空格 |
| `Assets/Scripts/DDay/DDayDialogue.cs` | 仅 Day1 读取整页演出；末格淡出状态 |
| `Assets/Scripts/DDay/DDayDialogueUI.cs` | 共用输入接口，整页旁白一次点击推进 |
| `Assets/Scripts/DDay/DDayGame.cs` | 最后一格等待淡出后调用既有剧情完成回调 |
| `Assets/Scripts/DDay/DDayCampaign.cs` | 镜头存档版本、旧存档位置迁移；不把照片展示当作现场发现 |
| `Assets/Scripts/DDay/DDayUI.cs` | 在既有UI对象上加载漫画组件 |
| `Assets/Editor/DDayArtImport.cs` | 原尺寸、无压缩、Point、无Mipmaps导入 |
| `Assets/Editor/DDayABPlayVerification.cs` | 三种分辨率、七格、瞬时效果截图、Game视图输入、存档与双周目回归 |
| `Tools/dday_day1_page.py`（新增） | 生成独立演出配置；不写Excel或图片 |
| `Tools/dday_comic_presentation.py` | 说明旧Day1配置仅用于迁移，不覆盖新整页演出 |

Unity自动创建了相应新增资源的 `.meta`。没有修改场景、旧厕所游戏或其他日期的漫画演出配置。

## 实际验证

最终结果：[Play Mode 回归记录](Day1PageComicVerification/Results.md)，2026-10-12 01:11:00，PASS。

| 项目 | 结果 |
|---|---|
| 1920×1080 / 1600×900 / 1280×720 | 七格各保存三张实际Game画面；整页可见，文字容器测量无溢出 |
| 顺序 / 高亮 | 七格均实际播放，斜边遮罩与焦点载入正常；已逐张查看截图 |
| 闪电 / 震动 / 警示光 | 运行时瞬时效果截图保存为 `Effect_lightning.png`、`Effect_impact.png`、`Effect_ambulance.png` |
| 点击 / 空格 | 向真实Unity Game视图发送IMGUI鼠标与空格事件，均推进一格；未注入Windows硬件输入 |
| 声音 | thunder/impact/dial/siren触发，wind/crowd/rain的AudioSource播放状态通过；未进行听觉音质验收 |
| 工作结束 / 下一天 | 无WorkSummary弹窗，B1状态保留，末格淡出后进入Day2，SISY缺席与B3空位正确 |
| 中途继续游戏 | 恢复同一镜头ID及B1负责人状态，未重播旧Day1 prose |
| 其他B2 / 双周目 / 结局 | 其余7个既有漫画事件、Day1–Day7双周目、三项生还条件、YES/YES及其他三种选择分支通过 |
| A1 / A2 / 存档 | 原12日解数不变；A2点击后入册、去重、未读状态、手动槽与继续流程回归通过 |
| Console | 最终检查错误0、警告0 |

实际截图全部位于 `Docs/Day1PageComicVerification/`。第一次验证发现继续提示容器高度不足，已增加到36并复测通过；首次失败记录保存在备份目录。

## 保留与风险

- 备份：`Backups/Day1PageComic_Before_20261012/`。
- Excel SHA256：`97E00E7A2755E8F81433DB1E92CCD6069A5CE75D90E6E5E606FED919E21CDFFE`，保持不变。
- ABConfig SHA256：`F0FCB9D08BE3AC303D30099D418D07E279DBC79CBF1F7D53D2620FFE88BCA3A7`，保持不变。
- 原附件与导入PNG SHA256均为：`8AF5604A7E41A67FEB95EBF435C5B7F66F02046A4DD7A0AF45FB8CB53F96BEDF`。
- 旧 `ComicPresentation.json` 与备份哈希相同，其余日期配置未修改。
- **原图第五格含灰狼靠近/看向照片，与“现场无人发现照片”的文字意图有歧义。** 当前只高亮地面照片、人物压暗，不添加发现/拾取演出，不授予照片物品；原画未改。是否需要替换原图仍需作者确认。
- 缺少正式风声、雷声、撞击、纸张风声、电话拨号、人群、雨声及警笛录音。当前使用低音量合成占位音；可将同名AudioClip导入 `Assets/Resources/DDay/Story/Day1/Audio/`（wind/thunder/impact/dial/crowd/rain/siren）替换，无需改剧情。
- 新版镜头中途续播已实测；旧 prose 存档迁移实现已检查，但未使用用户实际旧存档逐份验收。
- 本轮未重新生成Windows构建，完成的是主工程Editor内导入、运行与验收。
