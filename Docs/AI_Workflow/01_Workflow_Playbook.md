# 01 · Workflow Playbook

这是从 17 个开发 session 中提炼出来、可直接复用的 agent 协作流程。适用于"一个人 + 若干 AI agent"开发中小型 Unity（或其他）项目。

---

## 1. 核心原则

1. **文档即上下文（Docs as Context）**：agent 每次都是"失忆"的，项目的规则、设计和进度必须写在仓库里，而不是留在聊天记录里。
2. **人做决策，agent 做实现**：玩法、范围、取舍由开发者拍板；agent 负责方案草拟、编码、调试和文档维护。
3. **小步迭代，每步可验证**：一个 session 只做 1~2 个功能；每次改动都要能在 Unity 里跑起来看到结果。
4. **验证闭环由 agent 自己完成**：通过 Unity MCP 读 Console、查场景层级、跑代码，而不是让开发者来回截图报错。
5. **每个 session 必须"可交接"**：结束时更新进度日志、提交代码、生成下一次的开场 prompt。

---

## 2. 文档体系（Docs as Context）

| 文件 | 角色 | 更新频率 | 谁写 |
|---|---|---|---|
| `GameInfo.md` | 最初的项目蓝图（中英对照），阶段划分 | 立项时一次 | Codex 生成，人审 |
| `GameOutline.md` | **当前**设计文档：系统架构、属性/技能/装备/AI 等所有设计 | 设计变更时 | agent 起草，人确认 |
| `CLAUDE.md` / `AGENTS.md` | agent 规则：目录、三层架构、命名、禁止事项、**设计决策表** | 发现新规则/新坑时 | agent 维护 |
| `progress_report.md` | 追加式 session 日志：Completed / Files / Next Steps / Known Issues | **每个 session** | agent 在收尾时写 |
| `AGENT_WORKFLOW.md` | 新 agent 的开场说明书（流程 + 状态表 + 文件地图 + 陷阱） | 阶段性 | agent 汇总 |
| `AssetInventory.md` | 第三方资源清单 | 引入资源时 | agent 盘点 |
| `Docs/UI_Design_Notes.md`、`Docs/Storyboard_*.md` | 专题设计文档（UI 失败复盘、分镜+台词） | 专题进行时 | 共同维护 |

要点：

- **设计决策表**（`CLAUDE.md` 的 *Design Decisions Log*）记录"选了哪个方案、为什么"。写明 *Do not contradict these without explicit user approval*，防止新 agent 推翻已定方案。
- `CLAUDE.md` 和 `AGENTS.md` 内容保持一致，这样 Claude Code 和 Codex 读到的是同一份规则。
- `progress_report.md` 最新记录放在顶部，`Next Steps` 就是下一个 session 的任务清单。

---

## 3. 项目阶段与工具分工

| 阶段 | 时间 | 主力工具 | 产出 |
|---|---|---|---|
| 0. 立项与规划 | 02-23 ~ 02-28 | Codex（`unity-game-dev` skill） | 目录规范、`GameInfo.md`、`GameOutline.md`、脚本骨架、Unity MCP 打通 |
| 1. 规则落地 | 03-01 | Claude Code、Antigravity | `CLAUDE.md`、目录重构、首个战斗场景 |
| 2. 核心系统 | 03-01 ~ 03-09（S1–S14） | Claude Code（Opus 4.6）为主，Codex 辅助 | 网格/寻路/AP/技能/状态/地表/掩体/LoS/AI/UI/音效/探索 |
| 3. 课程报告 | 03-04 | Codex | 报告 v2/v3（按反馈与评分标准自评重写） |
| 4. 流程固化 | 05-03 | Claude Code | `AGENT_WORKFLOW.md` |
| 5. 终版冲刺 | 05-13 ~ 05-14（S15–S17） | Claude Code（Sonnet 4.6 → Opus 4.7） | 设置/FPS、办公室剧情、三幕过场、异步预加载、结局、Build 修复 |
| 6. 交付与复盘 | 05-14 ~ 05-15 | Claude Code | 架构图核对、视频解说稿、提交物自评 |
| 7. 长期维护 | 05-20 ~ 10-02 | Codex、Claude Code | 聊天记录恢复/导出、简历描述、Archify 架构图、本工作流文档 |

经验：**规划类任务**（文档、目录、报告）和**实现类任务**（编码 + Unity 操作）可以交给不同 agent；只要共享同一套仓库文档，切换工具的成本很低。

---

## 4. Session 生命周期

### 4.1 开场（Handoff Prompt）

每个 session 用上一次 `/end-session` 生成的交接 prompt 开头（格式由 [`end-session.md`](../../.claude/commands/end-session.md) 定义）。它包含：

1. 按顺序读取 `CLAUDE.md` → `progress_report.md`（最新一条）→ `GameOutline.md` →（专题文档）
2. 上次完成了什么（2~4 条）
3. 本次重点（来自 Next Steps）
4. 遗留问题 + 注意事项（例如"Unity 打开的是主仓库路径，不是 worktree"）
5. "读完告诉我你已了解项目状态，然后我们开始工作" —— 先对齐，再动手

### 4.2 对齐目标

- 让 agent 先**说思路**再写代码（例："先做个 esc 菜单，说说你的思路……目前代码是不是还不太支持？"）。
- 有多个方案时让 agent 给选项（A/B/C），人来选（例：背景故事选 B、结局选 "1+3"）。
- 范围控制：一次 1~2 个功能；超出就写进 Next Steps。

### 4.3 探索 → 实现

- 先 `Glob/Grep/Read` 相关模块和上游依赖，尤其是 `GameBootstrap.cs`（所有系统在这里按顺序初始化）。
- 遵守三层架构：薄 MonoBehaviour / 纯 C# 领域逻辑 / 表现层订阅 `EventBus`。
- 数据用 ScriptableObject（`*Definition` / `*Config`），运行时状态用 `*Runtime`。

### 4.4 验证闭环（Unity MCP）

```text
改代码 → refresh_unity（触发编译）→ read_console（查编译错误）
      → find_gameobjects / manage_scene（查层级与引用）
      → execute_code（在 Editor 内跑 C# 片段做断言或批量修改）
      → 用户进 Play Mode 试玩 → 截图/描述反馈 → 下一轮
```

- 编译错误、场景引用、SO 字段这类问题由 agent 通过 MCP 自查；用户只负责"玩起来对不对"。
- 当用户说"你直接调 mcp 做"时，说明 agent 把本可自动化的步骤推给了人 —— 应默认优先用 MCP。

### 4.5 收尾（`/end-session`）

自定义命令 [`.claude/commands/end-session.md`](../../.claude/commands/end-session.md) 做三件事：

1. 在 `progress_report.md` 顶部插入本 session 记录（Completed / Files Created / Files Modified / Key Technical Details / Next Steps / Known Issues）
2. `git add` 本次相关文件（排除截图和 `settings.local.json`）→ 英文 commit（带 `Co-Authored-By`）→ push
3. 输出下一次 session 的中文交接 prompt；如有需要，给出 PR 标题和描述

---

## 5. 分支、Worktree 与 PR

- 长期开发分支：`feature/combat-visualizer-and-bootstrap`，每个阶段通过 PR 合入 `main`（共 13 个 PR）。
- Claude Code 桌面端为每个会话自动创建 worktree（`.claude/worktrees/<name>`，分支 `claude/<name>`），适合并行试验。
- **但 Unity 只打开主仓库**：通过 MCP 修改场景/资产时必须作用在主仓库路径；worktree 里只改代码，最后合回。这一点要写进交接 prompt。
- Commit / PR 一律英文（用户偏好，已写入 agent memory），聊天可以用中文。

---

## 6. 模型选择

| 场景 | 选择 | 理由 |
|---|---|---|
| 日常小改、文案、查资料 | Sonnet | 快、便宜 |
| 跨文件重构、难 bug、长链路功能（过场系统、异步加载） | Opus | 推理更稳，S16/S17 中途 `/model` 切到 Opus 4.7 |
| 立项规划、文档生成、报告改写 | Codex / GPT | 与 Claude 交叉使用，互相复核 |
| 只读核对（架构图 vs 代码、提交物自评） | Explore subagent | 不污染主上下文 |

---

## 7. 交付阶段的 agent 用法

- **需求解读**：用 PDF skill 读课程要求，做 gap analysis（"可交付物还缺什么"）。
- **按评分标准补功能**：例如 FPS 显示直接对应评分项 "game statistics (e.g. frame rate)"。
- **Build 专属 bug**：Editor 正常但 exe 卡死 → 让 agent 指导打开 Development Build 和 Player.log，定位到 build 中被剥离的粒子 shader 为 null。
- **材料准备**：视频解说稿（中英分段，便于配音拼轨）、GitHub 浏览要点、第三方资源清单、提交物客观自评。
