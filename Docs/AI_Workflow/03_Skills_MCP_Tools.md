# 03 · Skills, Commands, MCP & Tools

统计来自可追溯的会话（2026-02 Codex、2026-05 以后的 Claude Code / Codex）。2026-03 的 Claude Code transcript 已过期，那段时期的用法根据 `progress_report.md` 和 worktree memory 补充。

---

## 1. Agent 与模型

| Agent | 模型 | 主要用途 |
|---|---|---|
| Claude Code（CLI / 桌面端） | Opus 4.6 | Session 1–14 核心系统开发（git 尾注 `Co-Authored-By: Claude Opus 4.6`） |
| | Sonnet 4.6 | S15 日常开发、交付材料、只读核对 |
| | Opus 4.7 | S16–S17 过场系统、异步加载、Build bug |
| | Opus 5.5 | 本工作流文档 |
| Codex（VS Code 插件 / 桌面端） | GPT-5.3-codex | 立项规划、设计文档、S2 开发、课程报告 |
| | GPT-5.5 / GPT-5.6 | 聊天记录恢复、简历描述、Archify 架构图 |
| Antigravity | — | 03-01 目录重构、导入 Forge 演示场景 |

片尾署名 *"In collaboration with Opus 4.7 & GPT 5.5"* 即来源于此。

---

## 2. Skills

| Skill | 平台 | 用在哪里 | 作用 |
|---|---|---|---|
| `unity-game-dev` | Codex | Phase 0 立项（02-27） | Unity 结构化开发流程。8 条硬性规则（不假设场景对象存在、避免 God class、`Update()` 不放重逻辑、组合优于继承、MonoBehaviour 只做胶水、增量验证……）和固定输出结构（Goal → 约束 → 拆解 → 计划 → MCP 行动计划 → 代码 → 测试清单 → 常见坑）。`CLAUDE.md` 的 Architecture Principles 基本继承自这里 |
| `unity-mcp-orchestrator` | Codex（已安装） | 可选 | Unity MCP 工具/资源用法与工作流模板 |
| `anthropic-skills:pdf` | Claude Code | S15（05-13） | 读取终版作业要求 PDF，做 gap analysis |
| `archify` | Codex | 09-14 | 从仓库代码生成可交互 HTML 架构图（含明暗主题、截图校验） |
| `frontend-slides` | Claude Code / Codex（已安装） | 可选 | HTML 演示文稿；可用于答辩/展示 |

> 经验：在 Codex 的 02-27 会话里，曾把 `SKILL.md` 误放进项目目录 —— skill 应该放在 `~/.codex/skills/` 或 `~/.claude/skills/`，不要进仓库。

---

## 3. Slash Commands

| 命令 | 类型 | 说明 |
|---|---|---|
| `/end-session` | **项目自定义**（[`.claude/commands/end-session.md`](../../.claude/commands/end-session.md)） | 更新 progress_report → commit & push → 生成下一次中文交接 prompt → 输出 PR 信息。S6 引入，之后每个 session 收尾使用 |
| `/model` | 内置 | 在会话中切换 Sonnet ↔ Opus（S16 切到 Opus 4.7 处理复杂过场逻辑） |
| `/compact`（自动） | 内置 | S15 一个会话里触发 3 次上下文压缩；压缩后的摘要会作为新消息注入 |

---

## 4. MCP Servers

### 4.1 Unity MCP（[CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)）

通过 `Packages/manifest.json` 中的 `com.coplaydev.unity-mcp` 安装，让 agent 直接操作 Unity Editor。

5 月会话中的调用统计（S15 + S16/S17，去除 worktree 分叉副本）：

| 工具 | 次数 | 典型用法 |
|---|---|---|
| `execute_code` | 94 | 在 Editor 内跑 C#：给 SerializedObject 赋引用、批量改 Transform/材质、断言场景状态 |
| `refresh_unity` | 28 | 改脚本后触发资源导入与编译 |
| `read_console` | 19 | 读编译错误/运行时异常，形成自检闭环 |
| `find_gameobjects` | 11 | 按名称/组件查层级 |
| `manage_scene` | 10 | 打开/保存/查询场景 |
| `manage_camera` | 7 | 调整机位（分镜构图） |
| `manage_components` / `manage_gameobject` / `manage_asset` | 6 / 4 / 4 | 增删组件、建对象、资源操作 |
| `manage_build` | 2 | Build 设置 |
| `set_active_instance` | 3 | 多个 Unity 实例时指定目标（主仓库 vs worktree） |

### 4.2 其他 MCP

| Server | 用途 |
|---|---|
| `ccd_session_mgmt`（Claude 桌面端） | 05-03 列出并搜索历史会话 transcript，用于总结工作流 |
| Codex `list_mcp_resources` | 02-28 排查 MCP 连接问题 |

---

## 5. 内置工具使用画像

以 S16–S17（最长的一次 Claude Code 会话，约 16.5 小时、56 条用户消息）为例：

| 工具 | 次数 | 说明 |
|---|---|---|
| Edit | 106 | 小步修改，几乎不整文件重写 |
| Read | 102 | 改前必读 |
| Grep | 72 | 定位调用点、事件订阅 |
| Bash | 53 | git、日志、文件检查 |
| Unity MCP 合计 | 91 | 见上表 |
| TodoWrite | 13 | 多步任务（如用户一次提出 6 点需求）时拆清单跟踪 |

**Subagent**：只读核对类任务（架构图 vs 代码、提交物评分）使用 `Explore` subagent，避免大量文件内容挤占主会话上下文。

**Worktree**：桌面端为每个会话建立 `claude/<name>` 分支的 worktree；本项目共产生 8 个 worktree 会话。
