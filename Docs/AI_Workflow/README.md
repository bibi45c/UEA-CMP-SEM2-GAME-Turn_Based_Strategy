# AI-Assisted Development Workflow — Engineering Reference

# AI 协作开发工作流 — 工程实践参考

本目录整理了 **Hex Tactics**（Unity 6 回合制战术 RPG）从立项到交付全过程中，与 AI Coding Agent（Claude Code、Codex 等）协作的会话记录、使用到的 Skill / MCP / 命令，以及沉淀下来的工作流和踩坑经验。目标是让后续项目（或新的 agent）可以直接复用这套流程。

> 项目本身的设计与架构请看根目录的 [`README.md`](../../README.md)、[`GameOutline.md`](../../GameOutline.md)、[`progress_report.md`](../../progress_report.md)。本目录只关注"怎么和 agent 一起把它做出来"。

---

## 一图看懂 / At a Glance

```text
┌────────────── 文档即上下文 (Docs as Context) ──────────────┐
│ CLAUDE.md / AGENTS.md   规则、架构、命名、设计决策表          │
│ GameOutline.md          完整设计文档（agent 的"需求说明书"）  │
│ progress_report.md      追加式 session 日志 + Next Steps       │
└───────────────────────────────┬────────────────────────────┘
                                │ 每个 session 开头必读
                                ▼
  Handoff Prompt ─► 确认目标 ─► 探索代码 ─► 实现 ─► Unity MCP 验证
        ▲                                             │
        │                                             ▼
  /end-session ◄── 更新 progress_report ◄── 用户试玩反馈迭代
  (commit + push + 生成下一次的 Handoff Prompt)
```

## 数据概览 / Numbers

| 指标 | 数值 |
|---|---|
| 开发周期 | 2026-02-23 → 2026-05-15（交付），之后文档/简历/架构图维护至 2026-10 |
| 开发 Session（progress_report 编号） | 17 |
| 可追溯的 agent 会话 | 16（Claude Code 9 · Codex 7），见 [06_Prompt_Log.md](06_Prompt_Log.md) |
| 使用的 Agent / 模型 | Claude Opus 4.6 / 4.7、Sonnet 4.6、Opus 5.5；GPT-5.3-codex、GPT-5.5、GPT-5.6；Antigravity（早期目录重构） |
| C# 脚本 | 88 个，11 个模块目录（含 Editor） |
| Git 提交 / PR | 26 个非合并提交，13 个 PR 合入 main |
| Unity MCP 调用（仅可追溯的 5 月会话，去重后） | `execute_code` 94 次、`refresh_unity` 28、`read_console` 19 |

> ⚠️ **记录缺口**：Session 3–14（2026-03）主要在 Claude Code 中完成，但本地 transcript 已被 Claude Code 的默认清理策略删除，这一段只能通过 `progress_report.md`、git 提交和 worktree memory 重建。这本身就是一条经验，见 [04_Lessons_Learned.md](04_Lessons_Learned.md#记录与可追溯性)。

---

## 文档目录 / Contents

| 文件 | 内容 |
|---|---|
| [01_Workflow_Playbook.md](01_Workflow_Playbook.md) | **可复用的工作流**：文档体系、Session 生命周期、分支/worktree、验证、交接 |
| [02_Session_Timeline.md](02_Session_Timeline.md) | 全部 Session 时间线：阶段、使用的工具/模型、目标与产出 |
| [03_Skills_MCP_Tools.md](03_Skills_MCP_Tools.md) | 用到的 Skill、Slash Command、MCP Server、Subagent 清单与使用统计 |
| [04_Lessons_Learned.md](04_Lessons_Learned.md) | 踩坑记录与最佳实践（Unity MCP、上下文管理、构建差异、协作习惯） |
| [05_Prompt_Library.md](05_Prompt_Library.md) | 可直接复用的 Prompt 模板（立项、交接、收尾、报告、复盘） |
| [06_Prompt_Log.md](06_Prompt_Log.md) | 每个会话的用户提示词摘录（脚本生成、已脱敏） |
| [tools/session_digest.py](tools/session_digest.py) | 从本地 Claude Code / Codex 记录重新生成 06 的脚本 |

相关的现有文件：

- [`CLAUDE.md`](../../CLAUDE.md) / [`AGENTS.md`](../../AGENTS.md) — 给 agent 的项目规则（两份内容相同，分别给 Claude Code 和 Codex 读取）
- [`AGENT_WORKFLOW.md`](../../AGENT_WORKFLOW.md) — Session 14 后整理的开发流程 prompt
- [`.claude/commands/end-session.md`](../../.claude/commands/end-session.md) — 自定义 `/end-session` 收尾命令

## 重新生成提示词日志 / Regenerate the Log

```bash
python Docs/AI_Workflow/tools/session_digest.py --match 6056B --out Docs/AI_Workflow/06_Prompt_Log.md
```

脚本读取 `~/.claude/projects/*<match>*/*.jsonl` 和 `~/.codex/sessions/**/rollout-*.jsonl`，按 cwd 过滤，输出截断后的提示词并去除图片、IP 和本机用户路径。
