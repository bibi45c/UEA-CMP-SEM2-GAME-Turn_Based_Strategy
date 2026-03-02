# Game Outline — Turn-Based Tactical RPG
# 游戏大纲 — 回合制战术 RPG

## 1. 产品定位

类型：回合制战术 RPG 垂直切片
主要参考：神界：原罪 2（战斗系统、地表联动、数据驱动）
次要参考：博德之门 3（表现层、自由移动手感、相机可读性）
引擎：Unity (URP)

目标：做出一个可维护的垂直切片，验证核心战斗循环和工程架构。

---

## 2. 核心玩法循环

```
探索场景 → 触发遭遇 → 进入战斗 → 回合制战斗 → 结算奖励 → 返回探索
```

Phase 1 只做战斗部分，Phase 2 补上探索和场景切换。

---

## 3. 网格与移动方案（方案 C：混合架构）

### 3.1 设计原则

底层用 hex grid 做逻辑权威层，表现层做自由化移动插值。
玩家看到的是自由流畅的移动，引擎跑的是离散的 grid 规则。

### 3.2 三层分离

```
逻辑层 (Grid)
├── hex cell 存储：surface 状态、occupancy、cover、高度
├── 寻路在 grid 上跑 A*
├── 范围查询、射线判定、AOE 区域全部基于 cell
└── 地表联动规则基于 cell 状态机

移动层 (Movement)
├── 寻路输出 cell 路径 → 转换为世界坐标路径点
├── 角色沿世界坐标做平滑插值移动（曲线/直线）
├── 移动过程中实时映射 worldPos → cell，触发地表效果
└── 视觉上无格子跳跃感

表现层 (Presentation)
├── surface 视觉用 decal / shader / VFX 柔化边界
├── 格子边界不强制显示，可选 debug overlay
└── 移动路径预览可以画平滑曲线而非格子连线
```

### 3.3 地表系统（Surface）

地表效果挂在 hex cell 上，每个 cell 持有一个 `SurfaceType`：

```
None | Oil | Fire | Water | Poison | Ice | Electricity | Blood
```

联动规则示例：
- Fire + Oil → Fire（油被点燃，扩散到相邻 oil cell）
- Fire + Water → Steam（蒸汽，可能造成遮蔽）
- Fire + Poison → Explosion（爆炸，范围伤害）
- Electricity + Water → Electrified（水面带电）
- Ice + Fire → Water（冰融化）

联动用一张静态查找表驱动，不硬编码 if-else：

```csharp
// 概念示意
Dictionary<(SurfaceType, SurfaceType), SurfaceReaction> reactionTable;
```

### 3.4 Grid 参数建议

- Hex cell 尺寸：约 1.2 ~ 1.5m（外接圆半径）
- 战斗区域：8×10 到 10×12 cells
- 足够精度处理地表效果，不会太细导致浪费

### 3.5 距离判定

战斗中的距离判定统一用**世界距离**，但提供 hex 距离的换算工具：

```
1 hex 外接圆半径 R = 0.75m（可配置），格子宽 1.5m
技能射程 = 世界距离 / hexSize，向下取整得到 hex 距离
```

这样技能编辑器里可以填 hex 数（清晰），运行时用世界距离算（精确）。

### 3.6 Hex Cell 尺寸与单位占格

**Hex 尺寸：R = 0.75m（紧凑），可配置**

```csharp
public float hexOuterRadius = 0.75f;  // R：中心→顶点
public float hexInnerRadius => hexOuterRadius * 0.866f; // r：中心→边中点 ≈ 0.65m
// 格子宽（顶点到顶点）= 1.5m
// 10×12 战场 ≈ 13×16m
```

**单位尺寸等级：方案 B — 多格占用**

```
Size 1 (Small/Medium)：1 cell          → 人形、小型生物
Size 2 (Large)：       7 cells         → 中心 + 6 邻居（≈ 直径 3m）
Size 3 (Huge)：        19 cells        → 中心 + 两圈（预留，Phase 1 不实现）
```

规则：
- 单位有一个锚点 cell（中心），占用格由 size 自动扩展
- 寻路：大型单位每步检查所有占用格是否可通行
- 近战：任何一个占用格与目标相邻即可攻击
- 被攻击：命中任意占用格均算命中
- AOE：大型单位更容易被范围技能覆盖

Phase 1 分步：先全部 size=1 跑通战斗，后期加入 size=2 验证一个大型 boss。

### 3.8 高低差系统（离散高度层）

**决策：方案 B — 离散高度层，终版方案**

每个 HexCell 持有 `int heightLevel`，取值 0 / 1 / 2 三档：

```
heightLevel 0 → 地面（默认）
heightLevel 1 → 台阶、矮墙、废墟平台
heightLevel 2 → 塔顶、高台
```

战术规则：
- 高地攻击低地：射程 +1 或伤害 +15%（二选一，待调参）
- 低地攻击高地：命中率 -15% 或伤害 -10%
- 跨 1 层高度差：额外消耗 1 移动力
- 跨 2 层高度差：不可直接通行（需要经过标记为 ramp/stair 的 cell）

寻路整合：
```
moveCost = baseCost + abs(heightDiff) * heightMovePenalty
if abs(heightDiff) > 1 && !targetCell.hasRamp → impassable
```

Phase 1 可以只实现 0 和 1 两档，Phase 2 按需加入 2。

heightLevel 精度可按需细分（如 0~10），本质仍是离散高度，不需要升级为连续地形。

### 3.9 视野与战争迷雾

**Phase 1 决策：方案 A — 全开视野，无迷雾**

战斗中所有单位和地形始终可见，不做迷雾渲染。

**Phase 1 增强（核心循环稳定后加入）：方案 B — 视线判定 (Line of Sight)**

地图仍然全部可见，但技能/攻击需要视线通畅才能释放：

```
从 attackerCell 到 targetCell 沿 hex line 遍历
沿途检查每个 cell：
  if cell.coverType == FullCover && cell.heightLevel >= attacker.heightLevel
    → 视线被阻挡，技能不可释放
```

与高度系统联动：
- 高地单位可以越过矮掩体看到低地目标
- 低地单位被高墙完全遮挡

这是纯逻辑层判定，不需要任何迷雾渲染，实现成本低但战术价值高。

**Phase 2 可选：方案 C — 探索场景战争迷雾**

仅在探索场景中考虑迷雾（未探索/已探索/视野内），战斗场景不需要。

### 3.10 掩体系统

**Phase 1 决策：方案 B — 二级掩体（半掩体/全掩体）**

每个 HexCell 持有 `CoverType`：

```
None      → 无掩体（开阔地）
HalfCover → 半掩体（矮墙、木箱、断柱）→ 受到伤害 -25%
FullCover → 全掩体（高墙、大石柱、厚门）→ 远程攻击完全阻挡，近战不受影响
```

方向性判定 — 掩体只在攻击路径上才生效：
```
从 attackerCell 到 targetCell 画 hex line
检查 target 相邻 cell 中，攻击路径是否穿过 cover cell
  穿过 HalfCover → 伤害 -25%
  穿过 FullCover → 远程不可命中（近战无视）
  未穿过 cover   → 无保护（侧翼/绕后）
```

与已有系统联动：
- 高度：高地攻击可越过半掩体（从上往下打，矮墙挡不住）
- LoS：全掩体同时阻挡视线（3.7 节已定义）
- 地表：掩体可被破坏（如火烧木箱 → 半掩体消失 + 产生火焰地表）
- AI：评分系统考虑掩体位置（躲在掩体后得分更高）

**Phase 2 增强：方案 C — 可破坏掩体**

掩体拥有 HP，受到足够伤害后降级：
```
FullCover (HP) → 受损 → HalfCover (HP) → 摧毁 → None
爆炸类技能对掩体伤害加倍
```

Phase 1 不实现掩体破坏，只需后续给 cell 加 `coverHP` 字段即可。

---

## 4. 系统模块架构

参考架构图和 GameInfo 的模块划分，整理如下：

### 4.1 Core — 核心模块
- `GameBootstrap`：场景启动、系统初始化顺序
- `GameSession`：会话级状态（跨场景持久数据）
- `EventBus`：全局事件总线（解耦模块间通信）

### 4.2 Grid — 网格模块
- `HexGridMap`：hex 网格生成、cell 数据存储
- `HexCell`：单个格子的数据（坐标、surface、occupancy、walkable、cover、**heightLevel**）
- `HexGridHelper`：坐标转换（cube ↔ offset ↔ world）、邻居查询、范围查询
- `AStarPathfinder`：A* 寻路，输出 cell 路径，高度差影响移动消耗
- `SurfaceSystem`：地表状态管理、联动反应表、扩散逻辑
- `HeightSystem`：高度规则（高地加成、低地惩罚、跨层移动消耗、不可通行判定）

### 4.3 Combat — 战斗模块
- `CombatSceneController`：战斗场景总控，管理战斗生命周期
- `TurnManager`：回合顺序（先攻排序）、回合循环、当前行动者追踪
- `ActionSystem`：行动经济（移动行动 + 主行动 + 附加行动）
- `DamageResolver`：伤害计算、抗性、暴击
- `CombatResult`：胜负判定、结算数据

### 4.4 Units — 单位模块
- `UnitDefinition` (SO)：角色模板（基础属性、技能列表、装备预设）
- `UnitRuntime`：运行时状态（当前 HP、AP、位置、状态列表）
- `UnitStats`：属性聚合（base + equipment + buff = final）
- `TeamManager`：阵营管理、玩家队伍 vs 敌方队伍

### 4.5 Abilities — 技能模块
- `AbilityDefinition` (SO)：技能模板（名称、射程、消耗、冷却、效果列表）
- `AbilityExecutor`：技能执行管线（选目标 → 检查消耗 → 执行效果 → 播表现）
- `EffectPayload`：效果数据（伤害/治疗/施加状态/制造地表）
- `TargetingRule`：目标规则（单体/AOE/直线/自身）

### 4.6 Status & Buff — 状态系统
- `StatusDefinition` (SO)：状态模板（类型、持续回合、每回合效果）
- `StatusInstance`：运行时状态实例（剩余回合、叠加层数）
- `BuffManager`：状态的添加、移除、每回合 tick、过期清理

### 4.7 Items — 物品模块
- `ItemDefinition` (SO)：物品基类模板
  - `EquipmentDefinition`：装备（槽位、属性加成）
  - `ConsumableDefinition`：消耗品（使用效果）
- `Inventory`：背包容器（增删查、堆叠）
- `EquipmentSlots`：装备槽管理（主手/副手/头/胸/饰品）

### 4.8 Dice — 骰子与随机模块

**Phase 1 决策：方案 A — 战斗无骰子，探查/对话预留骰子接口**

战斗阶段：
- 伤害 = 固定公式（攻击力 - 防御）+ 小浮动（±10%）
- 命中 = 100%（仅特殊状态如致盲会导致 miss）
- 暴击 = Wits 派生暴击率，触发时伤害 ×1.5

探查/对话（Phase 2）：
- d20 + 属性修正 vs 难度阈值（DC）

架构要求 — 预留随机层接口：
- `IDamageRandomizer`：伤害计算的随机策略接口
- Phase 1 实现 `FixedDamageRandomizer`（固定公式 + ±10%）
- 终版可替换为 `DiceDamageRandomizer`（武器骰 + 伪随机保底）
- `DiceRoller`：通用骰子工具类（d4/d6/d8/d12/d20），Phase 1 可先写好备用

终版保底机制（待后续实现）：
- 连续 miss → 累积计数器 → 下次命中率隐性提升
- 连续低伤 → 保底不低于中位数
- 伪随机保底，保证手感不被纯运气惩罚

### 4.9 AI — 敌方 AI
- `AIBrain`：决策主循环
- `AIScorer`：目标评分、位置评分
- `AIAction`：AI 选出的行动数据（移动到哪 + 对谁用什么技能）
- 第一阶段追求稳定可预测，不搞复杂行为树

### 4.10 Camera — 相机模块
- `TacticalCamera`：战术相机（平移、缩放、可选旋转）
- `CameraFollow`：跟随当前行动者
- `CameraFocus`：聚焦技能目标、战斗事件

### 4.11 UI — 界面模块
- `CombatHUD`：战斗主界面容器
- `TurnOrderBar`：回合顺序栏
- `ActionBar`：行动栏（移动/攻击/技能/物品/结束回合）
- `UnitInfoPanel`：选中单位信息面板
- `TargetPreview`：目标预览（命中率、预计伤害）
- `DamagePopup`：伤害飘字
- `SurfaceTooltip`：地表效果提示

---

## 5. 数值系统

### 5.1 基础属性
| 属性 | 英文 | 影响 |
|------|------|------|
| 力量 | Strength | 近战伤害、负重 |
| 敏捷 | Finesse | 远程伤害、闪避 |
| 智力 | Intelligence | 魔法伤害、技能效果 |
| 体质 | Constitution | 最大 HP、物理抗性 |
| 感知 | Wits | 先攻、暴击率、侦测 |

### 5.2 派生数值
- 最大 HP = f(Constitution, Level)
- 物理护甲 = f(Constitution, Equipment)
- 魔法抗性 = f(Intelligence, Equipment)
- 移动力 = 基础值 + Equipment 修正
- 行动点 = 每回合固定（1 移动 + 1 主行动）
- 先攻 = f(Wits) + 随机修正
- 命中率 = f(对应主属性, 目标闪避)
- 暴击率 = f(Wits)

派生公式集中在 `UnitStats` 中管理，UI 和战斗脚本不直接计算。

---

## 6. 行动经济

每个单位每回合拥有：
- **1 移动行动**：在移动力范围内移动
- **1 主行动**：攻击 / 使用技能 / 使用物品
- **0~1 附加行动**：可选（如切换装备、查看环境）
- **结束回合**：显式操作，始终可用

移动和主行动的顺序不限（可以先打再走，或先走再打）。

---

## 7. Phase 1 内容规模

| 类别 | 数量 |
|------|------|
| 战斗场景 | 1 |
| 玩家单位 | 3（前排/远程/辅助） |
| 敌方单位 | 3~5 |
| 技能总数 | 6~10 |
| 物品 | 5~8 |
| 装备 | 5~10 |
| 状态效果 | 4~6（燃烧/中毒/防御提升/减速/...） |
| 地表类型 | 2~3（油/火/可选水） |
| 遭遇战 | 1 |

---

## 8. Phase 1 实现顺序

1. 新建战斗场景 `Combat_RuinsPrototype_01`，确定根对象层级
2. 搭建 `GameBootstrap` 和场景启动流程
3. 实现 hex grid 生成、cell 数据、坐标转换
4. 实现 A* 寻路 + 世界坐标路径转换
5. 实现 `UnitRuntime`、选中系统、移动（grid 寻路 + 表现层平滑插值）
6. 实现回合顺序 `TurnManager` 和行动经济
7. 实现基础攻击、伤害结算、死亡处理
8. 实现地表系统 `SurfaceSystem`（oil + fire 联动作为首个案例）
9. 实现 1 个辅助技能 + 1 个控制技能 + 状态系统
10. 实现战斗 UI（回合栏、行动栏、单位面板、目标预览）
11. 实现敌方 AI 基础决策
12. 实现遭遇战结算（胜负判定、奖励）
13. 补上背包、装备、消耗品
14. 打磨完整的一局战斗体验

---

## 9. Phase 2 扩展方向（暂不实现）

- 探索场景 `World_RuinsOutskirts_01`
- 遭遇触发和场景切换
- 跨场景状态持久化（队伍 HP、背包、战斗结果）
- 对话与任务（最简）

---

## 10. 待讨论事项

- [x] 骰子系统：Phase 1 用固定公式 + ±10% 浮动，预留 `IDamageRandomizer` 接口，终版加保底伪随机
- [x] 高低差：方案 B 离散高度层（int heightLevel 0/1/2），高地加成 + 低地惩罚 + 移动消耗，终版方案无需升级到连续高度
- [x] 视野/战争迷雾：Phase 1 全开视野，核心循环稳定后加入 LoS 视线判定（纯逻辑），Phase 2 探索场景可选迷雾
- [x] 掩体系统：方案 B 二级掩体（Half -25%伤害 / Full 阻挡远程），方向性判定，Phase 2 加入可破坏掩体
- [x] 自由移动精确度：方案 A — snap 到 cell 中心，视觉加微偏移（±0.1m）避免棋子感
- [x] Hex cell 大小：R=0.75m（紧凑），可配置；单位尺寸 Size 1/2/3 多格占用，Phase 1 先全部 size=1
