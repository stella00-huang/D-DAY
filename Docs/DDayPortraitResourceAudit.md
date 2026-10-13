# D-DAY 立绘与表情素材审计

普通立绘复用 `Assets/Resources/DDay/PixelV2/Employees.png`。
反馈表情复用 `Assets/Resources/DDay/PixelV2/TaskReactions.png`。
裁切配置与逐句演出：`Assets/Resources/DDay/DialoguePresentation.json`。
没有新增、替换或修改办公室角色图片。七种状态均可配置；缺失状态实际回退 neutral。

| 角色 | 已有真实素材 | 缺少的素材（运行时 neutral 回退） |
|---|---|---|
| 兔子 SISY | neutral、happy | sad、angry、surprised、nervous、serious |
| 灰狼 ORIN | neutral、nervous | happy、sad、angry、surprised、serious |
| 狐狸 NARCIS | neutral、serious | happy、sad、angry、surprised、nervous |
| 柴犬 ECHO | neutral、sad | happy、angry、surprised、nervous、serious |
| 黑猫 METIS | neutral、serious | happy、sad、angry、surprised、nervous |
| 棕熊 THANE | neutral、happy | sad、angry、surprised、nervous、serious |
| 熊猫 CASSIA | neutral、sad | happy、angry、surprised、nervous、serious |

现有素材共 14 个有效角色/表情组合，缺少 35 个组合。
情绪由独立演出配置指定，未指定为 neutral；没有按文本关键词自动猜测。
B2 原文中的描述性段落显示“旁白”，不会伪装成角色亲口说出的新对白。