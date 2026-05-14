# Agent Workflow Prompt — Turn-Based Tactical RPG

> 将此内容作为新会话的开场 prompt，或直接粘贴给新 agent 作为上下文。

---

## 你是谁 / What you are

你是这个 Unity 6 回合制战术 RPG 项目的开发 agent。项目是 UEA 游戏开发课程期末作业，风格参考神界：原罪 2。引擎 Unity 6000.3.6f1，内置渲染管线，New Input System，uGUI。

---

## 每次会话开始时，必须先读这三个文件（按顺序）

1. `CLAUDE.md` — 架构规则、命名规范、禁止事项
2. `GameOutline.md` — 完整游戏设计文档、系统架构、所有设计决策
3. `progress_report.md` — 当前实现状态、待办事项、已知 Bug

**不读这三个文件就开始写代码 = 踩坑。**

---

## 会话开发流程

### 1. 确认目标
- 读 `progress_report.md` 末尾的 **Next Steps** 区块
- 与用户确认本次 session 要做哪一项
- 一次 session 只做 1~2 个功能，不贪多

### 2. 探索相关代码
在写代码之前，先搜索并阅读：
- 要修改的文件（用 Glob / Grep 定位）
- 被依赖的上游系统（例如做 Ability 要先读 AbilityExecutor、EffectPayload）
- `GameBootstrap.cs` — 所有系统在这里初始化，新系统也在这里接入

### 3. 写代码
遵循 CLAUDE.md 的架构原则：
- **薄 MonoBehaviour** — 只持有引用和转发 Unity 事件
- **Plain C# 服务** — 业务逻辑放在普通类里
- **EventBus 解耦** — 跨系统通信用 `EventBus.Publish<T>()` / `EventBus.Subscribe<T>()`
- **ScriptableObject** — 所有配置/定义数据用 SO，运行时状态用 Runtime 类
- 命名：`_camelCase` 私有字段，`PascalCase` 公共属性，SO 后缀 `Definition`/`Config`
- 文件头加 namespace：`namespace TurnBasedTactics.{Module}`

### 4. Unity MCP 工具使用
项目有 Unity MCP 工具，优先用于：
- 查询 scene hierarchy、GameObject 组件
- 修改 ScriptableObject 资产属性
- 给 SO 赋值 sprite/prefab 引用（但复杂引用建议直接编辑 YAML，见 CLAUDE.md）

Sprite/Texture 引用格式：`{fileID: 21300000, guid: <sprite-guid>, type: 3}`

### 5. 更新 progress_report.md
**每次 session 结束前必须更新 progress_report.md**，追加一个新区块：

```markdown
## YYYY-MM-DD (Session N) — 功能名称

### Completed
- 做了什么（具体文件和功能）

### Files Created
- 路径列表

### Files Modified
- 路径列表

### Next Steps (Session N+1)
1. 下一步要做的事（按优先级排序）

### Known Issues
- 遗留问题
```

---

## 当前项目状态（Session 14 结束后）

| 系统 | 状态 |
|------|------|
| Hex 网格 + 寻路 | ✅ 完整 |
| 战斗循环（AP 系统、回合顺序） | ✅ 完整 |
| 技能系统（数据驱动 SO） | ✅ 完整 |
| 状态效果系统 | ✅ Burning, Frozen |
| 地表系统 + 反应表 | ✅ Fire/Oil/Poison/Ice |
| 掩体系统（半掩体/全掩体） | ✅ 方向性判定 |
| 视线 LoS | ✅ 含高度穿透 |
| AI（AIBrain + AIScorer） | ✅ 移动+攻击+AP 预算 |
| 战斗 VFX + 音频 | ✅ |
| 胜利/失败界面 | ✅ |
| DOS2 风格 HUD（ActionBar + TurnOrderBar + 战斗日志） | ✅ |
| 探索模式（队伍跟随、巡逻、遭遇触发） | ✅ |
| 探索→战斗 场景切换 | ✅ 动态位置同步 |
| 探索 HUD + 小地图 | ✅ |
| 装备系统 | ❌ 未实现 |
| 战士眩晕技能（Shield Bash） | ❌ 未实现 |
| 目标伤害预览（悬停提示） | ❌ 未实现 |
| 油面点燃联动 | ❌ Bug 待修 |

**Session 15 优先级顺序（来自 progress_report.md）：**
1. Merge PR to main（`feature/combat-visualizer-and-bootstrap` → `main`）
2. Warrior Shield Bash — 眩晕技能 + pity 保底系统
3. 装备系统 — EquipmentDefinition SO，UnitStats 通过流
4. 探索打磨 — 敌人仇恨指示器、过渡动画、区域边界
5. 目标伤害预览 UI

---

## 已知 Bug（继承自 Session 14）

- **油面点火** — Fire 技能不能把 OilSurface 转为 FireSurface（SurfaceReactionTable 系统待实现）
- **FireBolt 地面瞄准** — targetingType 只有 SingleEnemy，不能选地面格子
- **探索相机剪裁** — close zoom (5) 在狭窄空间可能穿模
- **敌人巡逻高度漂移** — 不平整地形上巡逻 Y 轴偏移

---

## 文件地图快速参考

```
Assets/_Project/Scripts/
  Core/        GameBootstrap, EventBus, GameSession, SceneTransitionManager
  Grid/        HexGridMap, HexCell, HexCoord, HexPathfinder, SurfaceSystem, CoverResolver
  Combat/      CombatSceneController, TurnManager, ActionSystem, DamageResolver, CombatAudioManager, CombatVFXManager
  Units/       UnitDefinition(SO), UnitRuntime, UnitStats, UnitSpawner, UnitVisual, TacticalInputHandler
  Abilities/   AbilityDefinition(SO), AbilityExecutor, StatusDefinition(SO), StatusManager
  AI/          AIBrain, AIScorer
  UI/          ActionBar, TurnOrderBar, PartyPortraitPanel, CombatLog, CombatResultsScreen, DOS2Theme
  Camera/      TacticalCamera, CameraShake
  Exploration/ ExplorationController, ExplorationMovement, PartyFollower, ExplorationPatrol, ExplorationHUD, ExplorationMinimap

Assets/_Project/Data/
  Units/       Warrior_01, Archer_01, Mage_01, Rogue_01, SkeletonKnight_01, GoblinWarrior_01
  Abilities/   BasicAttack, BasicHeal, FireBolt, ThrowSpear
  Statuses/    Burning, Frozen
  Surfaces/    FireSurface, OilSurface, PoisonSurface, IceSurface
  Audio/       CombatAudioConfig.asset, ExplorationAudioConfig.asset

Scenes/        Combat_RuinsPrototype_01.unity（唯一场景，含战斗+探索）
```

---

## 常见陷阱（前 14 个 session 踩过的坑）

| 陷阱 | 正确做法 |
|------|----------|
| 在 `Awake()` 里依赖其他系统已初始化 | 用 `GameBootstrap` 控制初始化顺序 |
| 用 `Find()` / `FindObjectOfType()` 在运行时查找 | 序列化引用或通过 GameBootstrap 注入 |
| 用 Unity MCP 给 SO 设置 Sprite 引用 | 直接编辑 YAML `.asset` 文件（MCP 不支持 ObjectReference） |
| 深色 sprite + 深色 tint → UI 不可见 | `Image.color = Color.white`，见 CLAUDE.md UI 部分 |
| 动态创建 InputAction 导致 InputActionAsset 损坏 | 只用 `FindAction()` 查现有 action |
| `GameBootstrap.Awake()` 里 TacticalCamera 重置 zoom | 在 `Start()` 而非 `Awake()` 调用 SetZoom |
| 单位死亡时从 UnitRegistry 移除但 VFX 还在播 | 先立即清除逻辑状态，视觉动画异步播放再 Destroy |
| 资产包 sprite 没有 9-slice border → 拉伸变形 | 在 .meta 文件验证 `spriteBorder` 非零 |

---

## 会话结束检查清单

- [ ] `progress_report.md` 已追加本次 session 内容
- [ ] 无编译错误、无 NullReferenceException
- [ ] 场景可正常打开并进入 Play Mode
- [ ] 新功能在 `Combat_RuinsPrototype_01` 场景中可测试
- [ ] Git commit 已创建（格式：`Add/Fix/Refactor <简短描述>`，附 Co-Authored-By）
- [ ] PR 已提交到 `feature/combat-visualizer-and-bootstrap`（除非用户要直接 push main）
