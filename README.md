# Hex Tactics: Turn-Based Tactical RPG

# 六角战术：回合制战术 RPG

A vertical-slice turn-based tactical RPG built with Unity 6, inspired by Divinity: Original Sin 2 and Baldur's Gate 3. Features hex-grid combat, environmental surface interactions, exploration with party management, and a data-driven architecture.

使用 Unity 6 开发的回合制战术 RPG 垂直切片，灵感来自《神界：原罪 2》和《博德之门 3》。具备六边形网格战斗、环境地表联动、队伍探索系统和数据驱动架构。

---

## Screenshots / 截图

*Coming soon / 即将添加*

---

## Story / 背景故事

> 2:59 AM. A game studio, development department.
> 凌晨 2:59。某游戏公司，开发部。

Three years ago, **you** joined a mid-sized game studio as a programmer. Three long years of code. Yesterday, your game — the thing that kept you up through countless nights — finally shipped.

三年前，**你**加入了这家不大不小的游戏公司，做了三年程序。你的游戏——那个让你熬了无数个夜的东西——昨天终于上线了。

But the world didn't applaud.

但现实没有给你掌声。

Four hours after launch, negative reviews avalanched in: **1,243 one-star ratings. All of them about bugs.** At 6 PM, the boss snapped at the all-hands meeting:

发布后四小时，差评像雪崩一样涌来：**1,243 条一星。全是 bug。** 下午六点，老板在全组例会上失控爆发：

> *"If there's no hotfix by 9 AM tomorrow, the whole team is fired."*
> *"明早九点前没有修复版本，整个团队解散。"*

The meeting ended. Coworkers drifted away — some for a smoke, some straight home. No one said *"I'll stay and help."*

会议结束。同事们陆续散去——有人去抽烟，有人直接回家，没有人说"我留下来帮你"。

Only you, sitting at your desk, staring at a screen full of errors. You ate a bowl of instant noodles in the break room and pushed the empty bowl aside. At 3 AM you came back to your workstation. Opened the IDE. The energy drink had gone cold. Your eyelids grew heavier and heavier...

只有你，坐在工位前，对着满屏的报错发呆。你去休息室泡了碗面，把它吃完，把空碗推到一边。凌晨三点，你回到工位，打开 IDE。能量饮料凉透了，眼皮越来越重……

You fell asleep on the keyboard. Then a line lit up the screen:

键盘上，你睡了过去。屏幕突然亮起一行字——

```
WARNING: Developer has entered the runtime environment.
```

**You have entered your own game. / 你进入了你自己的游戏。**

---

## Features / 功能特性

### Combat System / 战斗系统

- **Hex Grid Combat / 六边形网格战斗** — A* pathfinding on hex grid with discrete height levels (0/1/2) and movement cost calculations
  六边形网格上的 A* 寻路，支持离散高度层和移动消耗计算

- **Turn-Based Action Economy / 回合制行动经济** — 1 move + 1 main action + optional bonus per turn, initiative-based turn order
  每回合 1 移动 + 1 主行动 + 可选附加行动，基于先攻值的回合顺序

- **Surface System / 地表系统** — Oil puddles, fire surfaces with visual effects; reaction table for element interactions (Oil + Fire = Burning)
  油池、火焰地表及视觉效果；元素联动反应表（油 + 火 = 燃烧）

- **Ability System / 技能系统** — Data-driven abilities via ScriptableObjects: melee attacks, ranged spear throw, fire bolt, healing
  数据驱动的技能系统（ScriptableObject）：近战攻击、投矛远程攻击、火球术、治疗术

- **AI System / AI 系统** — Enemy AI with scoring-based decision making: evaluates targets, positions, and available abilities
  敌方 AI 基于评分的决策系统：评估目标、位置和可用技能

- **Status Effects / 状态效果** — Burning, poisoned, defense buffs with per-turn tick and duration tracking
  燃烧、中毒、防御增益等状态效果，支持每回合触发和持续追踪

- **Damage System / 伤害系统** — Formula-based damage (Strength/Finesse/Intelligence scaling), critical hits from Wits, +/-10% variance
  基于公式的伤害计算（力量/敏捷/智力缩放），感知属性暴击，+/-10% 浮动

### Exploration Mode / 探索模式

- **Party System / 队伍系统** — Leader + followers with automatic follow behavior and hysteresis-based movement
  队长 + 跟随者，支持滞后缓冲的自动跟随行为

- **Point-and-Click Movement / 点击移动** — Right-click to move the party leader, followers auto-follow with spacing
  右键点击移动队长，跟随者自动保持间距跟随

- **Enemy Patrols / 敌人巡逻** — Enemies patrol within radius of spawn point with random idle pauses
  敌人在出生点半径内巡逻，随机停顿

- **Encounter Trigger / 遭遇触发** — Proximity-based combat transition: exploration positions convert to nearest walkable hex cells
  基于距离的战斗触发：探索位置自动转换为最近的可通行六边形格子

### UI / 用户界面

- **DOS2-Style HUD / DOS2 风格界面** — Dark gothic theme with gold accents: action bar, turn order bar, party portraits, unit info panel
  暗黑哥特风格金色主题：行动栏、回合顺序栏、队伍头像、单位信息面板

- **Combat Log / 战斗日志** — Scrolling combat log with color-coded events (damage, healing, movement, death)
  滚动战斗日志，按颜色区分事件类型（伤害、治疗、移动、死亡）

- **Hotkey Support / 快捷键支持** — Number keys (1-6) for abilities, Space for end turn, C for cancel
  数字键（1-6）选择技能，空格结束回合，C 取消操作

- **Exploration HUD / 探索界面** — Party status panel with HP bars + radar-style minimap with friend/foe indicators
  队伍状态面板含血条 + 雷达小地图显示敌我标记

- **Damage Popups / 伤害飘字** — Floating damage/healing numbers with animation
  浮动伤害/治疗数字动画

### Audio / 音效

- **Combat Audio / 战斗音效** — Sword hits, bow shots, spell casts, footsteps, UI feedback sounds (music & SFX volumes balanced for ambient play)
  刀剑命中、弓箭射击、法术释放、脚步声、UI 反馈音效（音乐与音效音量平衡至环境聆听）

- **Exploration Audio / 探索音效** — Background music with crossfade, ambient layers, event-driven SFX
  探索阶段背景音乐与交叉淡入、环境音层、事件驱动音效

### Story Cutscenes / 故事过场

- **Four-cutscene narrative / 四段式叙事** — Opening (5 frames) on first load, Sleep (8 frames) when interacting with the desk, Dungeon-entry (7 frames) bridging into combat, Victory (7 frames) after the final battle
  开场动画（5 帧自动播放）、桌前互动触发睡前动画（8 帧）、衔接战斗场景的地牢入场动画（7 帧）、最终胜利动画（7 帧）

- **Async scene preload / 异步场景预加载** — Combat scene loads in the background during the dungeon cutscene; activation deferred so the new scene's BGM never fires early
  地牢动画播放期间后台异步加载战斗场景，延迟激活避免 BGM 提前响

- **Click-to-quit credits / 点击退出片尾** — Final canvas after the ending: "Made by Jiayu Jiang" + "In collaboration with Opus 4.7 & GPT 5.5"
  胜利动画后片尾画布："Made by Jiayu Jiang" + "In collaboration with Opus 4.7 & GPT 5.5"，点击关闭游戏

- **Bilingual captions / 中英双语字幕** — All 26 cutscene captions written in English; storyboard doc keeps Chinese alongside
  26 帧字幕全英文显示，分镜文档 `Docs/Storyboard_Complete.md` 保留中英文对照

### Settings & Pause Menu / 设置与暂停菜单

- **ESC Pause Menu / ESC 暂停菜单** — Pauses `Time.timeScale`. Options: BGM/SFX volume sliders, graphics quality (Low/Med/High), target frame rate (30/60/120/Max), vsync, game speed (0.5x/1x/2x)
  ESC 暂停 `Time.timeScale`。包含 BGM/SFX 音量、画质（低/中/高）、帧率上限、垂直同步、游戏速度选项

- **Persistent settings / 设置持久化** — All values stored via PlayerPrefs, applied on every startup
  全部通过 PlayerPrefs 持久化，启动时自动还原

- **FPS Counter / 帧率显示** — Top-right rolling 0.5s average; toggleable in pause menu
  右上角 0.5 秒滚动平均帧率，可在暂停菜单中开关

---

## Tech Stack / 技术栈

| Component / 组件 | Technology / 技术 |
|---|---|
| Engine / 引擎 | Unity 6000.3.6f1 (Unity 6) |
| Render Pipeline / 渲染管线 | Built-in Render Pipeline |
| Language / 语言 | C# (.NET Standard 2.1) |
| Input / 输入 | Unity Input System 1.18.0 |
| UI | uGUI (Canvas-based) |
| Shader | ShaderGraph 17.3.0 |
| Platform / 平台 | PC (Windows) |

---

## Project Structure / 项目结构

```
Assets/
  _Project/
    Scripts/
      Core/            # Bootstrap, EventBus, Session / 启动、事件总线、会话
      Grid/            # HexGrid, Cell, Pathfinding, Surface / 六边形网格、寻路、地表
      Combat/          # TurnManager, ActionSystem, DamageResolver / 回合、行动、伤害
      Units/           # UnitRuntime, UnitStats, UnitDefinition / 单位运行时、属性、定义
      Abilities/       # AbilityDefinition, AbilityExecutor / 技能定义、技能执行
      AI/              # AIBrain, AIScorer / AI 决策、AI 评分
      Camera/          # TacticalCamera / 战术相机
      UI/              # HUD, ActionBar, CombatLog, etc. / 界面组件
      Exploration/     # ExplorationController, PartyFollower / 探索控制、队伍跟随
      Office/          # OfficeBootstrap, CutsceneController, OfficeInteractable / 办公室场景与过场动画
      Items/           # Inventory, Equipment (placeholder) / 背包、装备（预留）
    Data/              # ScriptableObject assets / 数据资产
      Units/           # UnitDefinition SOs (Mage, Archer, Rogue, enemies)
      Abilities/       # AbilityDefinition SOs (attacks, spells, heals)
      Audio/           # CombatAudioConfig, ExplorationAudioConfig
      Encounters/      # EncounterDefinition SOs
    Scenes/
      Office/          # Office_01.unity (start scene + cutscenes)
      Combat/          # Combat_RuinsPrototype_01.unity (hex grid combat)
    Art/
      Cutscenes/       # frame3 dungeon + frameE ending images
      Materials/       # NightBackdrop_Black.mat, etc.
    Prefabs/           # Unit prefabs, VFX prefabs / 单位预制体、特效预制体
    Audio/             # SFX clips and audio mixer / 音效和混音器
  Cutscene/            # frame1 opening + frame2 sleep images
  ThirdParty/          # Third-party asset packs (Synty) / 第三方资源包
```

---

## Architecture / 架构设计

The project follows a **Three-Layer Rule** for clean separation of concerns:

项目遵循**三层分离规则**确保关注点分离：

```
1. Unity Binding Layer (MonoBehaviour)
   Unity 绑定层 — Serialized refs, Unity events, forward to services
                    序列化引用、Unity 事件、转发至服务层

2. Domain Logic Layer (Plain C#)
   领域逻辑层 — Game rules, state, calculations (no MonoBehaviour)
                  游戏规则、状态、计算（纯 C# 类）

3. Presentation Layer (Visual)
   表现层 — VFX, UI updates, camera, animations
              视觉效果、UI 更新、相机、动画
```

**Key design patterns / 关键设计模式:**

- **Event Bus** — Decoupled module communication via `EventBus<T>` / 通过事件总线解耦模块通信
- **ScriptableObject Definitions** — Data-driven unit, ability, and item templates / 数据驱动的单位、技能、物品模板
- **Runtime Instances** — Mutable state separated from immutable definitions / 运行时可变状态与不可变定义分离
- **Composition over Inheritance** — Prefer component composition / 优先使用组合而非继承

---

## How to Run / 如何运行

### Prerequisites / 前置条件

- **Unity 6000.3.6f1** (Unity 6) — Download from [Unity Hub](https://unity.com/download)
  从 Unity Hub 下载安装

### Steps / 步骤

1. **Clone the repository / 克隆仓库**
   ```bash
   git clone https://github.com/<your-username>/UEA-CMP-SEM2-GAME-Turn_Based_Strategy.git
   ```

2. **Open in Unity Hub / 在 Unity Hub 中打开**
   - Add the project folder in Unity Hub
     在 Unity Hub 中添加项目文件夹
   - Ensure Unity version **6000.3.6f1** is installed
     确保安装了对应的 Unity 版本

3. **Open the start scene / 打开起始场景**
   - Open `Assets/_Project/Scenes/Office/Office_01.unity`
     打开 Office_01 场景文件
   - Press Play in the Unity Editor
     在编辑器中按下 Play

4. **Full game loop / 完整游戏流程**
   - Opening cutscene plays automatically (5 frames, left-click to advance)
     开场动画自动播放（5 帧，左键继续）
   - Walk around the office with WASD; approach the desk and press **E** to trigger the sleep cutscene
     WASD 在办公室探索，靠近办公桌按 **E** 触发睡前动画
   - After the dungeon cutscene, you enter combat (exploration → encounter → grid combat)
     地牢动画结束后进入战斗（探索 → 遭遇 → 网格战斗）
   - Win the final battle to see the victory cutscene and credits
     战胜最终战斗后观看胜利动画与片尾

> **Note / 注意**: Third-party asset packs (Synty) are required but not included in the repository due to licensing. See [Asset Packs](#asset-packs--资源包) section.
> 第三方资源包（Synty）因许可限制未包含在仓库中，详见资源包章节。

---

## Controls / 操控方式

### Office Mode / 办公室模式

| Input / 输入 | Action / 操作 |
|---|---|
| WASD | Walk / 走动 |
| E | Interact with desk (trigger sleep cutscene) / 与办公桌互动（触发睡前动画） |
| Left Click / 左键 | Advance cutscene frame / 推进动画帧 |
| ESC | Pause menu / 暂停菜单 |

### Exploration Mode / 探索模式

| Input / 输入 | Action / 操作 |
|---|---|
| Right Click / 右键 | Move party leader / 移动队长 |
| Mouse Wheel / 鼠标滚轮 | Zoom camera / 缩放相机 |
| WASD | Pan camera / 平移相机 |
| ESC | Pause menu / 暂停菜单 |

### Combat Mode / 战斗模式

| Input / 输入 | Action / 操作 |
|---|---|
| Left Click / 左键 | Select unit / target cell / 选中单位或目标格子 |
| Right Click / 右键 | Move to cell (during move mode) / 移动到格子 |
| 1 | Move mode / 移动模式 |
| 2-6 | Select ability / 选择技能 |
| C | Cancel current action / 取消当前操作 |
| Space | End turn / 结束回合 |
| Mouse Wheel / 鼠标滚轮 | Zoom camera / 缩放相机 |
| WASD | Pan camera / 平移相机 |

---

## Units / 单位

### Player Party / 玩家队伍

| Unit / 单位 | Role / 职责 | Key Stat / 核心属性 | Abilities / 技能 |
|---|---|---|---|
| Mage / 法师 | Magic DPS | Intelligence 16 | Basic Attack, Fire Bolt, Heal |
| Archer / 弓箭手 | Ranged DPS | Finesse 14 | Throw Spear, Basic Attack |
| Rogue / 盗贼 | Melee DPS | Finesse 14 | Basic Attack |

### Enemies / 敌方

| Unit / 单位 | Type / 类型 | Key Stat / 核心属性 |
|---|---|---|
| Skeleton Knight / 骷髅骑士 | Melee Tank | Strength 14, Constitution 14 |
| Goblin Warrior / 哥布林战士 | Melee DPS | Finesse 12 |

---

## Asset Packs / 资源包

This project uses the following Synty Studios asset packs (purchased separately, not included in repo):

本项目使用以下 Synty Studios 资源包（需单独购买，未包含在仓库中）：

| Pack / 资源包 | Purpose / 用途 |
|---|---|
| POLYGON Dungeon Realms | Main combat arena environment / 主战斗场景环境 |
| POLYGON Dungeon | Dungeon environment pieces / 地下城环境组件 |
| POLYGON Office | Office_01 scene environment / 办公室场景环境 |
| POLYGON Fantasy Rivals | Character models / 角色模型 |
| POLYGON Knights | Knight armor and weapons / 骑士盔甲和武器 |
| POLYGON Particle FX | Combat VFX / 战斗特效 |
| Sidekick Characters | Modular humanoid characters / 模块化人形角色 |
| Animation - Base Locomotion | Movement animations / 移动动画 |
| Animation - Sword Combat | Combat animations / 战斗动画 |
| Dark Fantasy HUD | Font assets (PirataOne, Grenze, MarkaziText, Texturina) / 字体资源 |
| Big Fantasy RPG Music Bundle | Background music tracks / 背景音乐 |
| Action RPG SFX | Combat sound effects / 战斗音效 |

---

## Development / 开发记录

This project was developed across 17 iterative sessions. Full development history is documented in [`progress_report.md`](progress_report.md).

本项目经过 17 次迭代开发。完整开发历史记录在 [`progress_report.md`](progress_report.md) 中。

### Key Milestones / 关键里程碑

| Session | Milestone / 里程碑 |
|---|---|
| 1-3 | Hex grid, pathfinding, unit spawning / 六边形网格、寻路、单位生成 |
| 4-5 | Turn system, action economy, basic combat / 回合系统、行动经济、基础战斗 |
| 6-7 | Ability system, surface effects, AI / 技能系统、地表效果、AI |
| 8-9 | Combat UI, damage popups, status effects / 战斗界面、伤害飘字、状态效果 |
| 10-11 | Audio, combat log, hotkeys, UI polish / 音效、战斗日志、快捷键、界面打磨 |
| 12-13 | Exploration mode, party system, minimap / 探索模式、队伍系统、小地图 |
| 14 | Exploration audio, coursework report & video / 探索音效、课程报告与视频 |
| 15 | ESC pause menu, FPS counter, game settings / ESC 暂停菜单、帧率显示、游戏设置 |
| 16 | Office cutscene pipeline, async combat preload / 办公室过场动画、异步战斗场景预加载 |
| 17 | Victory ending, credits, bilingual captions, hex grid gating / 胜利动画、片尾、双语字幕、网格门控 |

---

## Known Issues / 已知问题

- Oil surfaces cannot be ignited by fire abilities (SurfaceReactionTable incomplete)
  油地表无法被火技能点燃（地表反应表未完善）

- Fire Bolt can only target enemies, not ground tiles
  火球术只能对敌人释放，不能对地面格子释放

- Exploration camera may clip through geometry at close zoom
  探索模式近距离相机可能穿模

---

## License / 许可

This project is an academic coursework submission for UEA CMP 6056B (Game and Mobile App Development).
Third-party assets are used under their respective licenses and are not redistributable.

本项目为 UEA CMP 6056B（游戏与移动应用开发）课程作业。
第三方资源按其各自许可协议使用，不可再分发。
