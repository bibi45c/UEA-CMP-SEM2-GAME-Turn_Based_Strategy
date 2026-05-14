# 完整过场动画设计文档
## 背景故事 + 全镜头分镜 + 风格指南

**项目**：Turn-Based Tactical RPG — 《Debug: Runtime》  
**文档版本**：v1.0 | 2026-05-13  
**状态**：开发中，AI 辅助生图参考

---

## 一、完整背景故事

### 标题：《最后期限》

> 凌晨 2:59。某游戏公司，开发部。

三年前，**你**加入了这家不大不小的游戏公司，做了三年程序。  
你的游戏——那个让你熬了无数个夜的东西——昨天终于上线了。

但现实没有给你掌声。

发布后四小时，差评像雪崩一样涌来：  
**1,243 条一星。全是 bug。**

下午六点，老板在全组例会上失控爆发：

> *"明早九点前没有修复版本，整个团队解散。"*

会议结束。同事们陆续散去——有人去抽烟，有人直接回家，  
没有人说"我留下来帮你"。

只有你，坐在工位前，对着满屏的报错发呆。

你去休息室泡了碗面，把它吃完，把空碗推到一边。  
凌晨三点，你回到工位。打开 IDE。  
能量饮料凉透了，眼皮越来越重……

键盘上，你睡了过去。

屏幕突然亮起一行字——

```
WARNING: Developer has entered the runtime environment.
```

**你进入了你自己的游戏。**

---

### 世界观双轨结构

| 层次 | 场景 | 氛围 | 风格 |
|------|------|------|------|
| 现实世界 | 公司办公室（深夜） | 压抑、疲惫、孤独 | 赛博朋克低多边形 · 冷蓝调 |
| 游戏内世界 | 地牢 / 战术战斗场 | 危险、奇异、自由 | Hades 风格 · 暖红金 · 手绘感 |

---

## 二、视觉风格指南

### 现实世界（办公室段落）

```
关键词：
low poly 3D character, cyberpunk neon style, dark cold atmosphere,
office at 3am, cinematic composition, flat color blocks,
chromatic aberration, subtle neon rim light, no text overlay,
stylized indie game cutscene
```

**色彩规范**

| 用途 | 颜色 | 描述 |
|------|------|------|
| 主光源 | `#1A3A5C` 冷蓝 | 窗外月光 / 显示器蓝光 |
| 辅光 | `#FFB347` 暖黄 | 台灯、走廊应急灯 |
| 霓虹点缀 | `#00FFCC` 青色 | Rim light，轮廓光 |
| 暗部 | `#050508` 近黑 | 深度感，压迫感 |
| 屏幕红 | `#FF2244` 报错红 | 满屏差评、ERROR 文字 |

---

### 游戏内世界（地牢段落）

```
关键词：
Hades game art style, isometric dungeon, stylized 2.5D,
warm red and gold lighting, Greek underworld aesthetic,
hand-painted textures, dramatic lighting, dark stone floor,
glowing runes, dynamic shadows, painterly style
```

**色彩规范**

| 用途 | 颜色 | 描述 |
|------|------|------|
| 主光源 | `#FF6B2B` 深橙红 | 岩浆、火把光 |
| 辅光 | `#FFD700` 金黄 | 法阵、符文发光 |
| 阴影 | `#1A0A00` 焦黑 | 极深暖黑 |
| 魔法 | `#7B2FBE` 紫 | 敌方技能、异常光效 |
| 角色轮廓 | `#FFFFFF` 白色描线 | Hades 风格硬边 |

---

## 三、全镜头分镜

### 总时间轴

| 镜头 | 触发 | 时长 | 内容摘要 | 对应 SlideArray |
|------|------|------|----------|-----------------|
| 镜头 1 | 进入场景自动播放 | ~12s | 休息室吃饭 → 起身 → 走向工位 | `_openingSlides` (frame1-A~E) |
| 镜头 2 | 走到办公桌互动触发 | ~22s | 三屏工位 → 困意 → 报错洪流 → WARNING | `_sleepSlides` (frame2-A~H 待制作) |
| 镜头 3 | 第二幕结束后衔接播放 | ~18s | 眩晕 → 爬起 → 看手 → 望向地牢 → 捡起法杖 → 认出自己的游戏 → 系统提示 | `_dungeonSlides` (frame3-A~E + 黑屏过渡 + 系统提示) |

**总时长**：约 50 秒（不含玩家在办公室自由探索时间）

---

## 镜头 1：凌晨的休息室

**所属段落**：现实世界 · 序幕  
**时长**：约 12 秒  
**任务**：建立时间（凌晨3点）和孤独感

---

### Frame 1-A｜钟特写（2s）

```
┌─────────────────────────────────┐
│                                 │
│         ╔══════════╗            │
│         ║  [时钟]  ║            │
│         ║  3:00 ←  ║            │
│         ╚══════════╝            │
│     (背景虚化：黑暗走廊)         │
│                                 │
└─────────────────────────────────┘
```

**AI Prompt**
```
Close-up of a round wall clock showing 3:00 AM, dark blurry office
corridor in background, cold blue neon rim light on clock frame,
low poly stylized art, cyberpunk color grading, chromatic aberration,
cinematic depth of field, no characters visible
```

**台词**
> 三年前，你加入了这家公司。
> 做了三年程序。
> 昨天，你的游戏终于上线了。
>
> *Three years ago, you joined the studio.*
> *Three years as a programmer.*
> *Yesterday, your game finally shipped.*

---

### Frame 1-B｜拉开交代环境（3s）

```
┌─────────────────────────────────┐
│  [钟]         [PROFITS海报]     │
│                                 │
│     ┌──────┐   ┌──────┐        │
│     │圆桌  │   │圆桌  │        │
│     └──┬───┘   └──┬───┘        │
│        │  [主角背影坐着]        │
│        │  (泡面/零食摊开)       │
│      地板延伸向黑暗走廊         │
└─────────────────────────────────┘
```

**AI Prompt**
```
Dark office break room at 3am, lone developer sitting at round table
eating instant noodles, back facing camera, empty chairs surrounding,
wall clock visible in background, cold blue ambient light from windows,
warm yellow light from one desk lamp, low poly stylized 3D art,
cyberpunk atmosphere, slow camera pull-back
```

**台词**
> 但现实没有给你掌声。
> 只有凌晨 3:07 的休息室，还亮着灯。
>
> *But there was no applause waiting for you.*
> *Only the break room at 3:07 AM was still lit.*

---

### Frame 1-C｜侧面主角脸部（2.5s）

```
┌─────────────────────────────────┐
│   [窗外城市夜景/黑暗]           │
│        ╔════════╗               │
│        ║主角侧脸║ ← 表情疲惫   │
│        ╚════╤═══╝               │
│          [泡面碗空了]           │
│          [能量饮料半罐]         │
└─────────────────────────────────┘
```

**AI Prompt**
```
Side profile of a low poly male developer, glasses and headphones,
wearing a coding t-shirt with </> symbol, staring at empty instant
noodle bowl, half-empty energy drink can beside it, city night view
through dark window, tired expression, cold blue side lighting,
neon cyan rim light, cyberpunk color palette, cinematic close shot
```

**台词**
> 泡面已经凉了。
> 工位上的报错，还在等你回去。
>
> *The instant noodles have gone cold.*
> *The errors at your desk are still waiting.*

---

### Frame 1-D｜站起来（1.5s）

```
┌─────────────────────────────────┐
│   [黑暗走廊方向 →→→]           │
│      ╔═══════════╗              │
│      ║  主角起身 ║              │
│      ║  推开椅子 ║              │
│      ╚═══════════╝              │
│    [椅子向后滑动]               │
└─────────────────────────────────┘
```

**AI Prompt**
```
Low angle shot of a low poly developer standing up from chair,
pushing chair backward, grabbing energy drink can, dark break room,
determined posture, cold neon blue backlight from window behind,
silhouette effect, low poly stylized art, cyberpunk office at night
```

**台词**
> 你把椅子推开。
> 再撑一下，就能把它修完。
>
> *You push the chair back.*
> *Just a little longer. You can fix this.*

---

### Frame 1-E｜走向黑暗（3s）

```
┌─────────────────────────────────┐
│  (空桌子+空碗留在画面左侧)      │
│                    ████         │
│                   █主角█ →→→  │
│                    ████         │
│              (走向黑暗走廊)     │
└─────────────────────────────────┘
```

**AI Prompt**
```
Wide shot of dark office break room, developer walking away toward
dark corridor, back to camera, empty table with instant noodle bowl
and energy drink in foreground, cold blue ambient light, long shadows,
low poly stylized art, cyberpunk night atmosphere, cinematic wide angle
```

**台词**
> 你走回工位。
> 这可能是最后一次提交。
>
> *You head back to your desk.*
> *This might be your last commit.*

---

## 镜头 2：工位 → 入睡

**所属段落**：现实世界 · 触发点 (Cutscene 2)
**时长**：约 22 秒
**触发条件**：玩家走到办公桌前互动 → `OfficeBootstrap.TriggerSleepSequence()`
**任务**：建立 3 屏工作环境 → 困意袭来 → WARNING 转场到地牢

---

### 3 块屏幕的内容分工

主角工位是三显示器配置，从左到右承担不同的叙事职责：

| 屏幕 | 内容 | 叙事作用 |
|------|------|----------|
| **左屏** | Steam 风格商品页，1,243 条一星差评，14% 好评 | 「玩家的反应」——外部压力 |
| **中屏** | VS Code 风格 IDE，自己写的游戏代码 + 红色波浪线 | 「你要修的东西」——核心任务 |
| **右屏** | Unity Editor，Scene 视图 + 满屏红色 Console 报错 | 「你做的游戏」——失败的产物 |

**构图三角**：代码（中）→ 游戏（右）→ 反馈（左），形成 "你写的 → 跑起来 → 被骂" 的故事闭环。

---

### Frame 2-A｜工位全景，建立 3 屏构图（3s）

```
┌─────────────────────────────────┐
│  ┌──────┐  ┌──────┐  ┌──────┐  │
│  │左屏  │  │中屏  │  │右屏  │  │
│  │差评  │  │ IDE  │  │Unity │  │
│  └──────┘  └──────┘  └──────┘  │
│      [主角背影，刚坐下]         │
│  [台灯 + 三屏冷光交错]          │
│  [桌面：饮料罐、便签、外卖盒]   │
└─────────────────────────────────┘
```

**AI Prompt**
```
Wide shot from behind of a developer just sitting down at a triple-monitor
workstation, three screens each glowing with different content—left shows
red Steam-like review page with 1-star ratings, center shows code editor
with red error squiggles, right shows Unity Editor with red console errors,
warm desk lamp pool of light on keyboard, energy drink cans and sticky notes
scattered, cold blue ambient from window behind, low poly stylized 3D,
cyberpunk office at night, cinematic establishing shot
```

**台词**
> 三块屏幕还亮着。
> 它们比你更清醒。
>
> *Three monitors still glowing.*
> *They're more awake than you are.*

---

### Frame 2-B｜左屏特写：商品页差评（3s）

```
┌─────────────────────────────────┐
│  STEAM-LIKE STORE PAGE          │
│  ─────────────────────────────  │
│  Overall: 14% Positive   ❗      │
│  Recent:  1,243 reviews         │
│  ─────────────────────────────  │
│  ★☆☆☆☆ WORST GAME EVER         │
│  ★☆☆☆☆ 全是 bug，垃圾退款        │
│  ★☆☆☆☆ Crashes on launch        │
│  ★☆☆☆☆ 开发商跑路了吗?           │
└─────────────────────────────────┘
```

**AI Prompt**
```
Close-up of a Steam-style game store page on a monitor, "Overwhelmingly
Negative" red banner, 14% Positive rating, "1,243 reviews" counter, scrolling
list of 1-star reviews mixing Chinese and English text ("WORST GAME EVER",
"全是bug退款", "Crashes on launch", "refund now"), dominant red coloring,
CRT scan line overlay, chromatic aberration, low poly stylized UI aesthetic,
cyberpunk color grading, harsh red glow
```

**台词**
> 发布页面上，全是红色。
> 1,243 条评价，14% 好评。
>
> *The store page is bleeding red.*
> *1,243 reviews. 14% positive.*

---

### Frame 2-C｜中屏特写：IDE 代码（3s）

```
┌─────────────────────────────────┐
│  // VS Code-style dark theme    │
│  ─────────────────────────────  │
│   12  void OnAttack(Unit u) {    │
│   13      var target = ~~null~~; │ ← 红波浪线
│   14      target.~~TakeDamage~~();│ ← 红波浪线
│   15      Log("hit");            │
│   16  }                          │
│  ─────────────────────────────  │
│  ❌ Errors: 14  ⚠ Warnings: 47  │
│  光标在 13 行闪烁                │
└─────────────────────────────────┘
```

**AI Prompt**
```
Close-up of VS Code-style IDE on monitor, C# game code visible with multiple
red squiggly underlines indicating compilation errors, "Errors: 14
Warnings: 47" status bar at bottom, dark editor theme with syntax highlighting,
blinking text cursor on broken line, tooltip showing "NullReferenceException
possible", subtle CRT scan line overlay, monitor glow on developer's
shoulder in foreground bokeh, low poly stylized art, cyberpunk aesthetic
```

**台词**
> 代码没有回你。
> 只有红线一条接一条亮起来。
>
> *The code gives you nothing back.*
> *Only red squiggles, one after another.*

---

### Frame 2-D｜右屏特写：Unity Editor（3s）

```
┌─────────────────────────────────┐
│  UNITY EDITOR                   │
│  ┌─────────────┐ ┌──────────┐  │
│  │  Scene View │ │Inspector │  │
│  │  [游戏画面]  │ │   ...    │  │
│  └─────────────┘ └──────────┘  │
│  ┌─────────────────────────┐   │
│  │ Console:                │   │
│  │ ❌ NullReferenceException│   │
│  │ ❌ MissingComponent...   │   │
│  │ ❌ Stack overflow        │   │
│  │ ❌ ... (滚动中)          │   │
│  └─────────────────────────┘   │
└─────────────────────────────────┘
```

**AI Prompt**
```
Close-up of Unity Editor interface on monitor, Scene View in upper portion
showing a fantasy dungeon prototype with grid overlay, Console panel at
bottom filled with red stack-traced NullReferenceException and
MissingComponentException entries scrolling continuously, Inspector on
right side, hierarchy on left, dark UI theme, CRT scan line effect, ominous
red glow from console, monitor glow on protagonist in soft foreground blur,
low poly stylized art, cyberpunk aesthetic
```

**台词**
> Unity 控制台还在刷错。
> 像是在替你的游戏求救。
>
> *Unity's console keeps screaming.*
> *Like your game is calling for help.*

---

### Frame 2-E｜主角侧脸：揉眼睛（2s）

```
┌─────────────────────────────────┐
│   [侧脸特写]                    │
│   ╔═══════════════════════╗    │
│   ║   [揉眼睛的动作]      ║    │
│   ║   眼镜反光是屏幕红     ║    │
│   ║   嘴角下沉            ║    │
│   ╚═══════════════════════╝    │
│   [手边：空了一半的能量饮料]   │
└─────────────────────────────────┘
```

**AI Prompt**
```
Side profile close-up of exhausted developer rubbing his eyes with one hand,
glasses reflecting red light from monitors, headphones around neck, dark
circles under eyes, weary expression, half-empty energy drink can in
foreground bokeh, warm-cold lighting contrast (desk lamp vs monitor glow),
low poly stylized 3D, cyberpunk, cinematic emotional close-up
```

**台词**
> 能量饮料见底了。
> 你揉了揉眼睛，还是看不清下一行。
>
> *The energy drink is empty.*
> *You rub your eyes — still can't focus on the next line.*

---

### Frame 2-F｜趴键盘睡着（俯视）（2s）

```
┌─────────────────────────────────┐
│  [俯视角度]                     │
│                                 │
│         [机械键盘]              │
│   [主角头趴在键盘上]            │
│      [头发散开]                 │
│   [一只手垂在桌沿]              │
│  [屏幕光从下打上来照亮脸]       │
└─────────────────────────────────┘
```

**AI Prompt**
```
Top-down overhead shot of developer asleep with head resting on mechanical
keyboard, hair fanned across keys, one hand limp hanging off desk edge,
monitors still glowing in background with red errors and review page,
desk lamp warm pool of light, low poly stylized art, cyberpunk office,
eerie quiet moment, cinematic overhead composition
```

**台词**
> 你告诉自己——
> 只闭上眼，五分钟。
>
> *You tell yourself —*
> *Just close your eyes. Five minutes.*

---

### Frame 2-G｜屏幕：报错洪流 → 异常停滞（3s）

```
┌─────────────────────────────────┐
│  [3 屏同时特写，并排]           │
│  ─────────────────────────────  │
│  ❌ NullRef in Combat.cs:42    │
│  ❌ MissingRef Cell[3,5]       │
│  ❌ Stack overflow             │
│  ❌ ... (高速向上滚动)         │
│  ─────────────────────────────  │
│  突然 3 屏同时冻结              │
│  → 全黑闪烁 0.5s                │
└─────────────────────────────────┘
```

**动画效果**：红色错误堆栈高速向上滚动 → 突然全部冻结 → 三屏齐刷刷闪黑一瞬 → 中屏开始打出绿色字符

**AI Prompt**
```
Three monitors all showing rapidly scrolling red error stack traces in
terminal font, NullReferenceException and MissingComponentException
repeating endlessly, sense of cascading system failure, intensifying glitch
effect, moment captured just as all screens go black simultaneously, low
poly stylized, cyberpunk aesthetic, dramatic tension, sleeping developer's
silhouette in foreground
```

**台词**
> 错误还在滚动。
> 一行，又一行。然后，三块屏幕同时停住。
>
> *The errors keep scrolling.*
> *Line after line. Then — all three screens freeze.*

---

### Frame 2-H｜WARNING 全屏 → 黑屏（3s）

```
┌─────────────────────────────────┐
│                                 │
│  ┌──────────────────────────┐   │
│  │ > SYSTEM                 │   │
│  │ > WARNING:               │   │
│  │ > Developer has entered  │   │
│  │ > the runtime environment│   │
│  │ >                        │   │
│  │ > Press any key to debug_│   │
│  └──────────────────────────┘   │
│  绿色终端字 + 扫描线 + 抖动     │
│  → 黑屏淡出，接镜头 3（地牢）   │
└─────────────────────────────────┘
```

**动画效果**：逐字打字机，phosphor 绿，CRT 扫描线，轻微 chromatic aberration
**音效**：低沉警报声 + 键盘打字音 → 渐隐 → 静默
**过渡**：文字打完 3 秒 → 整体淡黑 → 1 秒静默 → 切到地牢场景（镜头 3）

**AI Prompt**
```
Single monitor displaying terminal WARNING message in phosphor green CRT
font on pure black background, text reads "WARNING: Developer has entered
the runtime environment", prominent CRT scan lines, chromatic aberration,
slight digital glitch, blinking underscore cursor, green monitor glow
illuminating sleeping developer's face from below, sinister calm before
the dive, low poly stylized 3D, cyberpunk, cinematic dramatic shot
```

**说明**
此帧无字幕——WARNING 文字已经画在屏幕上，不再叠加台词。

---
## 四、地牢入场镜头（镜头 3）

**所属段落**：游戏世界 · 开篇  
**时长**：约 16 秒  
**风格切换**：从办公室黑屏 → 暖橙低多边形地牢  
**任务**：用第一视角交代“从现实坠入游戏”，最后让主角意识到这里就是自己做的游戏。

---

### Frame 3-0｜黑屏文字过渡（2s）

```
┌─────────────────────────────────┐
│                                 │
│          BLACK SCREEN           │
│                                 │
│      你感到一阵眩晕。           │
│                                 │
│      远处传来火焰燃烧声。       │
│                                 │
└─────────────────────────────────┘
```

**画面**
纯黑背景，白字居中或偏下，保留一秒静默。上一幕 WARNING 的绿色残影可以轻微闪一下后消失。

**台词**
> 你感到一阵眩晕。
> 远处传来火焰燃烧声。
>
> *A wave of dizziness hits you.*
> *Somewhere, flames are crackling.*

---

### Frame 3-A｜半睁眼从地板醒来（第一视角）（3s）

```
┌─────────────────────────────────┐
│  [第一视角：半睁眼低角度]       │
│                                 │
│  [上下黑边像眼皮遮住画面]       │
│  [模糊的石板地面 / 裂纹 / 尘土] │
│  [两只护甲手撑在地上]           │
│                                 │
│  [远处：虚化的大厅与火光]       │
└─────────────────────────────────┘
```

**动作**
镜头从贴近石板地面的低角度开始，像刚醒来时半睁眼：上下有黑色眼皮遮挡，视野狭窄且模糊。主角两只护甲手撑在地上，暗示正在爬起。远处地牢大厅只是虚焦轮廓，中央不出现敌人或怪物；如果需要空间层次，可以在大厅侧边保留很模糊的小型轮廓，但不要抢画面。面前没有法杖。地面不显示红色 debug 网格线。

**AI Prompt**
```
First-person point of view from a very low angle on a warm orange stone
dungeon floor, as if the player has just opened their eyes halfway. Heavy
black eyelid shadows crop the top and bottom of the image, creating a narrow
semi-open-eye view. The player character's two armored hands are visible in
the foreground, bracing against the cracked stone floor, bronze gauntlets and
wrapped sleeves matching the main controllable character. Distant dungeon hall
and torchlight are softly blurred and indistinct. No monsters or enemies in
the center of the hall; optional tiny blurred silhouettes may appear near the
side background only. No staff or weapon in front of the player yet. Current
Unity low-poly fantasy dungeon style, Synty-like simple geometry, warm orange
and gold lighting, dark cavern shadows, cinematic cutscene still, no red debug
grid lines on the floor, no UI, no subtitles, no watermark
```

**台词**
> 冰冷的石板贴着你的脸。
>
> *Cold stone presses against your cheek.*

---

### Frame 3-B｜看手与地上的法杖（3s）

```
┌─────────────────────────────────┐
│                                 │
│  [第一视角：双手抬到眼前]       │
│  [视线朝向远处地牢大厅]         │
│                                 │
│  [护甲手套 / 绷带 / 游戏角色手] │
│  [暖橙火光照亮金属边缘]         │
│                                 │
│  [地毯上：一根法杖躺在旁边]     │
│  [背景虚化：大厅 / 石像 / 火把] │
└─────────────────────────────────┘
```

**动作**
主角抬起双手，看到自己的手变成了游戏角色的护甲手套。镜头方向不是低头看地面，而是朝向远处地牢大厅；大厅保持虚焦，只作为空间背景。画面下方或中下方的地毯上能看到尚未捡起的法杖，为下一帧“捡起法杖”做铺垫。

**AI Prompt**
```
First-person close-up of the player character raising both armored hands
in front of their face while looking toward the large dungeon hall. Angular
bronze gauntlets, wrapped cloth bands, dark undersuit sleeves, warm torchlight
catching the metal edges. The distant dungeon hall, statues, braziers and
orange stone architecture are visible but softly blurred in the background.
On the carpet in the lower part of the frame, a bronze staff with a red gem
lies on the floor, not picked up yet. Current Unity/Synty-like low-poly game
style, flat color blocks, orange-gold dungeon lighting, subtle magical glow,
no red debug grid lines, no UI, no subtitles, no watermark, no modern office
objects
```

**台词**
> 这不是你的手。
>
> *These aren't your hands.*

---

### Frame 3-C｜抬头看见地牢（4s）

```
┌─────────────────────────────────┐
│                                 │
│  [第一视角：地牢大厅展开]       │
│                                 │
│  [远处祭坛 / 石像 / 蓝色水晶]   │
│  [吊链 / 火盆 / 巨型机械结构]   │
│  [队友或敌影在远处很小]         │
│                                 │
└─────────────────────────────────┘
```

**动作**
镜头抬起并稳定，第一次完整看见远处地牢大厅：火盆、石像、蓝色水晶、吊链和大型机关结构。构图参考当前战斗场景截图，但去掉地面红色 debug 线。

**AI Prompt**
```
First-person view looking up into a large low-poly fantasy dungeon hall,
matching the current Unity game scene: warm orange stone platform, railings,
braziers, hanging chains, massive suspended mechanical structure overhead,
distant altar, stone statues, glowing blue crystals, lava-orange light and
dark cavern ceiling. The floor must be clean stone with no red debug grid
lines. Cinematic in-game cutscene still, Synty-like low-poly geometry, warm
orange and gold lighting, deep shadows, no UI, no subtitles, no watermark,
no office objects
```

**台词**
> 远处的地牢慢慢清晰起来。
>
> *The dungeon ahead slowly comes into focus.*

---

### Frame 3-D｜捡起旁边的法杖（3s）

```
┌─────────────────────────────────┐
│                                 │
│  [第一视角：地面近景]           │
│                                 │
│  [一根法杖躺在石板旁边]         │
│  [主角的手伸过去抓住它]         │
│                                 │
│  [红色宝石发出微弱光]           │
└─────────────────────────────────┘
```

**动作**
镜头低头看向身边，主角伸手捡起法杖。法杖红色宝石亮起一瞬，像是系统确认装备。

**AI Prompt**
```
First-person close-up in a low-poly fantasy dungeon, a bronze and wood staff
with a red glowing gem lies on cracked stone floor beside the player,
matching the main controllable character weapon design. Armored hand reaches
down and grips the staff, warm torchlight, orange-gold shadows, small red
gem glow reflecting on the gauntlet, current Unity/Synty-like low-poly game
style, no red debug grid lines on the floor, no UI, no subtitles, no watermark
```

**台词**
> 你下意识地捡起了旁边的法杖。
>
> *You instinctively pick up the staff beside you.*

---

### Frame 3-E｜意识到这里是自己的游戏（4s）

```
┌─────────────────────────────────┐
│                                 │
│  [第一视角：法杖举到画面右侧]   │
│                                 │
│  [远处地牢大厅完整展开]         │
│  [熟悉的祭坛 / 石像 / 机关]     │
│                                 │
│  [主角停住，意识到真相]         │
└─────────────────────────────────┘
```

**动作**
主角举起法杖，视线扫过远处大厅。画面停住半秒，让玩家把地牢布局和“自己设计过的游戏关卡”联系起来。

**AI Prompt**
```
First-person cinematic view inside the current low-poly Unity fantasy dungeon,
the player character holds a bronze staff with red gem at the right edge of
the frame, looking toward a familiar dungeon hall with altar, statues,
braziers, blue crystals, hanging chains, railings and warm orange stone
architecture. The scene feels recognizable, like a game level the character
designed. Synty-like low-poly geometry, flat color blocks, warm red-gold
lighting, dark cavern shadows, no red debug grid lines, no UI, no subtitles,
no watermark, no office objects
```

**台词**
> 这地方……有点熟悉。
> 不对。
> 这不就是你做的游戏吗？
>
> *This place... feels familiar.*
> *Wait.*
> *Isn't this the game you made?*

---

### Frame 3-F｜系统提示黑屏（自动跳转战斗场景前）

```
┌─────────────────────────────────┐
│                                 │
│          BLACK SCREEN           │
│                                 │
│        [ 系统提示 ]             │
│  你已进入 Runtime Environment。 │
│  找到出口，或在这里永远留下。   │
│                                 │
└─────────────────────────────────┘
```

**画面**
纯黑画布 + 白色字幕居中。这是地牢动画的最后一帧，玩家点击后激活已在后台预加载完成的战斗场景。

**台词**
> [ 系统提示 ]
> 你已进入 Runtime Environment。
> 找到出口，或在这里永远留下。
>
> *[ SYSTEM ]*
> *You have entered the Runtime Environment.*
> *Find the exit, or stay here forever.*

---

## 五、游戏内 UI 提示（非过场，仅参考）

进入战斗场景后，屏幕短暂显示：

```
[ 系统提示 ]
你已进入 Runtime Environment。
找到出口，或在这里永远留下。
```

---

## 六、胜利结局过场（待制作）

**所属段落**：游戏世界 → 现实世界 · 结尾  
**触发条件**：玩家通关地牢 / 击败最终 Bug Core / 到达出口  
**建议时长**：约 18~22 秒  
**任务**：闭环“办公室 → 游戏 → 办公室”，让玩家知道主角修好了游戏，但保留一点不确定的余味。

---

### Frame E-1｜Bug Core 崩溃（3s）

```
┌─────────────────────────────────┐
│                                 │
│       [地牢中央核心 / Boss]      │
│        红色错误裂纹崩解         │
│                                 │
│   [玩家小队站在远处，背影]      │
│                                 │
└─────────────────────────────────┘
```

**动作**
地牢中央的最终核心或 Boss 被击败。红色错误裂纹从地面、空中和敌人身体上剥落，逐渐变成金色/蓝色粒子。

**AI Prompt**
```
Low-poly Unity fantasy dungeon victory scene, matching the current game
screenshots. A central Bug Core or final boss construct collapses in the
dungeon hall, red glitch cracks and error-like fragments dissolving into
gold and blue particles. Player party silhouettes stand in the distance,
warm orange braziers, blue crystals, stone statues, hanging chains, dramatic
but readable in-engine cutscene still, no UI, no subtitles, no watermark,
no debug grid lines.
```

**台词**
> 随着 Boss 被击败，红色错误裂纹从地面、空中和敌人身体上剥落。
> 最后一行 Bug，终于停了。
>
> *As the Boss falls, red error cracks break away from the floor, the air, and its body.*
> *The final error line goes still.*

---

### Frame E-2｜法杖变成光标（3s）

```
┌─────────────────────────────────┐
│                                 │
│   [第一视角 / 法杖举到眼前]     │
│   [红宝石发光 → 终端光标形状]   │
│                                 │
│      [周围漂浮代码碎片]         │
│                                 │
└─────────────────────────────────┘
```

**动作**
主角举起法杖。红宝石光芒变成终端光标或 Debug 指针，周围浮现短暂的代码碎片，暗示“武器其实是调试工具”。

**AI Prompt**
```
First-person low-poly Unity dungeon cutscene. The player holds the bronze
staff with red gem close to camera; the gem transforms into a glowing terminal
cursor / debug pointer shape. Small floating code fragments and clean green
runtime particles orbit the staff. Warm orange dungeon lighting, blue crystal
accents, bronze gauntlet visible, in-engine stylized look, no UI, no subtitles,
no watermark, no debug grid lines.
```

**台词**
> 你举起法杖。
> 它不像武器，更像一个断点。
>
> *You raise the staff.*
> *Not a weapon. More like a breakpoint.*

---

### Frame E-3｜Runtime 修复（2s）

```
┌─────────────────────────────────┐
│                                 │
│  PATCH APPLIED                  │
│  Runtime stable.                │
│  Returning developer...         │
│                                 │
└─────────────────────────────────┘
```

**画面**
黑底终端风格。不同于开头的 WARNING，这次是稳定、干净的绿色系统日志。

**台词**
> 世界开始重新编译。
>
> *The world begins to recompile.*

---

### Frame E-4｜办公室醒来（4s）

```
┌─────────────────────────────────┐
│  [三屏工位，清晨微光]           │
│                                 │
│      [主角从键盘上猛地抬头]     │
│                                 │
│  [屏幕还亮着，但不再全红]       │
└─────────────────────────────────┘
```

**动作**
回到办公室。主角从键盘上抬起头，窗外已经有一点晨光。桌面仍然是同一个工位，三屏还亮着。

**AI Prompt**
```
Low-poly Unity/Synty-like office workstation at dawn, same triple-monitor
desk layout as the current project screenshots. The developer wakes up from
the keyboard, lifting his head suddenly, glasses askew, headphones around neck,
warm desk lamp fading against pale morning light through the window. Monitors
are still on but no longer flooded with red errors. Cinematic cutscene still,
no UI overlays, no subtitles, no watermark.
```

**台词**
> 你从键盘上抬起头。
> 指尖还记得法杖的重量。
>
> *You lift your head from the keyboard.*
> *Your fingertips still remember the staff's weight.*

---

### Frame E-5｜Build Success（3s）

```
┌─────────────────────────────────┐
│  [中屏：BUILD SUCCESS]          │
│  [右屏：Console clean]          │
│  [左屏：评论页停止刷新]         │
└─────────────────────────────────┘
```

**动作**
镜头贴近三块屏幕。中屏显示 `BUILD SUCCESS`，右屏 Unity Console 清空，左屏的差评页面不再刷新。

**AI Prompt**
```
Close-up of the same triple-monitor office workstation. Center monitor shows
large clean green text "BUILD SUCCESS"; right monitor shows a Unity-like
console with no red errors; left monitor shows the game store review page
paused, no new red review rows. Warm desk lamp, early morning blue light,
low-poly Unity/Synty-like in-engine style, no external UI overlays, no
watermark.
```

**台词**
> 没有红字。
> 没有崩溃。
> 只有一个绿色的成功提示。
>
> *No red text.*
> *No crashes.*
> *Just a single green success message.*

---

### Frame E-6｜上传修复版（3s）

```
┌─────────────────────────────────┐
│                                 │
│  [中屏：Hotfix v1.0.1 uploaded] │
│  [左屏：玩家开始反馈已修复]     │
│       08:57                     │
│                                 │
│  [主角坐在工位前，疲惫但放松]   │
└─────────────────────────────────┘
```

**动作**
主角坐回椅子，身体终于放松。中屏显示修复版上传完成，时间卡在早上 8:57。左屏不是好评暴涨，而是 v1.0.1 发布后的少量新反馈：玩家开始说大部分 bug 已经修复，游戏终于能正常玩。

**AI Prompt**
```
Low-poly Unity/Synty-like office workstation at early morning. The exhausted
developer sits back in the chair, shoulders relaxing for the first time.
Center monitor shows a hotfix upload complete screen, version 1.0.1, time
08:57. Left monitor shows a small forum/review feed after v1.0.1 with a few
mixed Chinese and English player comments saying most bugs are fixed and the
game is finally playable, but not a sudden flood of positive reviews. Desk
clutter, lamp, keyboard, energy drink bottle, soft morning light through
window, cinematic quiet ending, no Unity editor overlays, no watermark.
```

**台词**
> 早上 8:57。
> 修复版本上传完成。
>
> *8:57 AM.*
> *Hotfix uploaded.*

---

### Frame E-7｜最后的痕迹（4s）

```
┌─────────────────────────────────┐
│                                 │
│  [主角离开工位，画面停在桌面]   │
│                                 │
│  [键盘旁：微弱发光的红宝石碎片] │
│  [屏幕角落：connection idle_]   │
└─────────────────────────────────┘
```

**动作**
主角离开画面。镜头停在桌面：键盘旁边有一个微弱发光的红宝石碎片，或中屏角落闪过一行 `Runtime Environment: connection idle_`。

**AI Prompt**
```
Quiet low-poly Unity/Synty-like office desk close-up after the developer has
left the chair. Keyboard, mouse, desk lamp, empty cans and sticky notes remain.
Beside the keyboard lies a tiny glowing red gem shard, subtle and mysterious.
On the corner of a monitor, small terminal text reads "Runtime Environment:
connection idle_". Pale morning light, calm but unsettling ending, no
subtitles, no watermark, no extra characters.
```

**台词**
> 你以为自己回来了。
> 但游戏，似乎还没有完全结束。
>
> *You think you're back.*
> *But the game... isn't finished with you yet.*

---

## 七、后续结局分支（待开发）

| 镜头 | 触发条件 | 内容 |
|------|----------|------|
| 坏结局 | 玩家在地牢中全灭 | 回到办公室，屏幕：`fix: developer.exe — FAILED`，GAME OVER |
| 隐藏结局（待定） | 特定条件 | 主角修复了游戏，但选择留在游戏世界 |

---

## 八、技术实现说明

### 当前实现

过场通过 `CutsceneController.cs` 的 `PlaySlides()` API 播放幻灯片（图 + 字幕）。`OfficeBootstrap.cs` 上两个序列化数组接入对应资产：

```csharp
// OfficeBootstrap.cs
[Header("Opening cutscene (plays on scene load, before player control)")]
[SerializeField] private CutsceneSlide[] _openingSlides;  // 镜头 1 — frame1-A~E（已就位）

[Header("Sleep cutscene (plays when interacting with desk)")]
[SerializeField] private CutsceneSlide[] _sleepSlides;    // 镜头 2 — frame2-A~H（待制作）
```

**触发链**：
- `Start()` → `PlayOpeningCutscene()` → 播 `_openingSlides` → 启用玩家输入
- 玩家走到桌前互动 → `TriggerSleepSequence()` → 播 `_sleepSlides` → 加载战斗场景

每个 `CutsceneSlide` 包含 `Sprite image` 和 `string caption`，左键点击进入下一帧。

### 制作流程

1. **AI 生图**：用文档中各 Frame 的 AI Prompt（Midjourney / DALL-E / Stable Diffusion）生成静态图
2. **导入 Unity**：放进 `Assets/Cutscene/` 文件夹，TextureType 改为 `Sprite`
3. **接入 Inspector**：在 `OfficeBootstrap` 的对应数组里填图 + 台词
4. **可选升级**：若有视频，赋值给 `CutsceneController._videoClip` 字段

---

*文档维护：随剧情调整更新 | AI 提示词可直接复制用于 Midjourney / DALL-E / Stable Diffusion*
