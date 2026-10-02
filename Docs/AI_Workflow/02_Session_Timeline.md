# 02 · Session Timeline

来源：`progress_report.md`、git 历史（`Co-Authored-By` 尾注）、本地 Claude Code / Codex 记录（可用 [`tools/session_digest.py`](tools/session_digest.py) 重新提取）。

图例：🟣 Claude Code　🟢 Codex　🔵 Antigravity　📄 只有 progress_report / git 可追溯（transcript 已过期）

---

## Phase 0 · 立项与规划（2026-02-23 ~ 02-28）

| 日期 | 工具 / 模型 | 内容 | 产出 |
|---|---|---|---|
| 02-23 | 🟢 GPT-5.3-codex | 根据 `GameInfo.md` 写仓库简介 | GitHub description |
| 02-27 ~ 28 | 🟢 GPT-5.3-codex + `unity-game-dev` skill | 讨论 Unity 目录规范（`_Project` / `ThirdParty` 混合结构）；以 DOS2 为主、BG3 为辅出两阶段开发计划和完整设计文档（要求"生成计划后二次判断是否完善"）；浏览 Synty 资源包选战斗场景；按文档搭脚本骨架；反复调试 Unity MCP 连接（stdio ↔ http、重启、升级插件） | `GameInfo.md`（中英对照）、`GameOutline.md`、脚本骨架、MCP 可用 |

## Phase 1 · 规则落地与核心系统（2026-03-01 ~ 03-09）

| Session | 日期 | 工具 | 主题 | 关键产出 |
|---|---|---|---|---|
| — | 03-01 | 🟣 Opus 4.6 | 写 agent 规则 | `CLAUDE.md`（PR #1，worktree `festive-germain`） |
| — | 03-01 | 🔵 Antigravity | 目录重构、导入 Forge 场景 | 按 `CLAUDE.md` 重组项目结构 |
| S1 | 03-01 | 🟣 📄 | Grid + Units + Movement | HexGrid、A*、UnitDefinition/Runtime、EventBus、GameBootstrap |
| S2 | 03-02 | 🟢 GPT-5.3-codex | 战斗 UI 与行动选目标 | 攻击/治疗技能、回合计数；提出"每做完一部分就记录"→ 形成 `progress_report.md` 习惯 |
| S3 | 03-02 | 🟢/🟣 📄 | Combat Polish & Equipment | 战场探索原型、战术相机（PR #2） |
| S4 | 03-03 | 🟣 📄 | 移动可视化修复 | DOS2 风格移动预览 |
| S5 | 03-03 | 🟣 📄 | 敌方 AI、血条、飘字、回合条 | `AIBrain` + `AIScorer` |
| S6 | 03-03 | 🟣 📄 | UI 点击穿透、移动确认 | `WeaponBindingGuide.md`；新增 **`/end-session` 命令** |
| S7 | 03-03 | 🟣 📄 | 数据驱动技能系统 | `AbilityDefinition` SO + `AbilityExecutor` |
| S8 | 03-03 | 🟣 📄 | 死亡动画、状态、地表、掩体 | Status/Surface/Cover 系统 |
| S9 | 03-03 | 🟣 📄 | 视线 + DOS2 AP 系统 | LoS、AP 预算 AI |
| S10 | 03-03 | 🟣 📄 | VFX、胜负界面、场景切换 | `SceneTransitionManager` |
| S11 | 03-04 | 🟣 📄 | 战斗音效、DOS2 HUD 调研 | `CombatAudioManager` |
| S12 | 03-04 | 🟣 📄 | UI 打磨、战斗日志、快捷键、Phase 1 审计 | Dark Fantasy HUD **尝试后回滚** → `UI_Design_Notes.md` + `CLAUDE.md` UI 规范 |
| S13 | 03-04 | 🟣 📄 | 探索模式 | 队伍跟随、巡逻、遭遇触发、小地图 |
| — | 03-04 | 🟢 GPT-5.3-codex | 课程中期报告改写 | 按反馈 + 6 周课件生成 report-2，再按评分标准自评生成 report-3（限定雅思 6 分词汇） |
| S14 | 03-09 | 🟣 📄 | 探索音效、报告与视频 | `ExplorationAudioManager`；报告/视频脚本（不入库） |

## Phase 2 · 流程固化（2026-05-03）

| 日期 | 工具 | 内容 | 产出 |
|---|---|---|---|
| 05-03 | 🟣 Opus 4.6 → Sonnet 4.6 | 用 `ccd_session_mgmt` 检索历史会话，总结工作流 | [`AGENT_WORKFLOW.md`](../../AGENT_WORKFLOW.md) |

## Phase 3 · 终版冲刺（2026-05-13 ~ 05-14）

| Session | 时间 | 工具 | 主题 | 关键产出 |
|---|---|---|---|---|
| S15 | 05-13 13:53–18:52 | 🟣 Sonnet 4.6 + PDF skill + Unity MCP | 解读终版作业要求 → gap analysis；ESC 菜单（做完后**回滚**，改成右上角 FPS + 画质按钮）；相机飞入修复；构思"办公室 → 地牢"背景故事；Office 场景可玩化（重力、动画、碰撞、坐椅子交互）；分镜设计 | `GameSettings`、`FPSCounter`、`Office_01`、`Storyboard_*.md`；3 次上下文压缩 |
| S16 | 05-13 18:55–23:06 | 🟣 Sonnet 4.6 → **Opus 4.7** | 三幕静态图过场（点击继续）；AI 生成图片后让 agent 识图配台词；后台 `LoadSceneAsync` 预加载战斗场景；修黑屏露底；音量、怪物飞天、小地图位置 | `CutsceneController`、`OfficeBootstrap` |
| S17 | 05-13 23:23–05-14 11:29 | 🟣 Opus 4.7 | 胜利结局过场 + 片尾字幕；台词全英文化（md 保留中英）；非战斗隐藏 hex 网格；**Build 专属卡死**排查（Player.log → null 粒子 shader）；README 同步 | v1.0 Release Build；两次 `/end-session` |

## Phase 4 · 交付与复盘（2026-05-14 ~ 05-15）

| 时间 | 工具 | 内容 |
|---|---|---|
| 05-14 | 🟣 Sonnet 4.6 + Explore subagent | 核对架构图（`SystemArchitecture.drawio`）是否与代码一致 |
| 05-14 ~ 15 | 🟣 Sonnet 4.6 | 视频解说稿措辞去"模板感"；GitHub 浏览视频要点；Synty 资源包清单；英文解说分段（便于配音拼轨） |
| 05-15 | 🟣 Sonnet 4.6 + Explore subagent | 对照评分标准给最终提交物客观打分；检查视频脚本完整性 |

## Phase 5 · 长期维护（2026-05-20 ~ 10-02）

| 日期 | 工具 | 内容 | 产出 |
|---|---|---|---|
| 05-20 | 🟢 GPT-5.5 | Codex 聊天记录丢失 → 从备份按项目导出 Markdown，编写恢复脚本，修复中文乱码 | `CodexChatExports_ByProject/`（本地，不入库） |
| 08-17 | 🟢 GPT-5.6 | 整理项目技术点与 agent 参与边界，写简历描述 | `Docs/Resume_Project_Description.md`（本地，未入库） |
| 09-14 | 🟢 GPT-5.6 + `archify` skill | 基于最新 main 生成可交互架构图 | `Docs/Architecture_Main_Archify.html`（本地，未入库） |
| 10-02 | 🟣 Opus 5.5 | 汇总所有会话、skill 与工作流 | 本目录 `Docs/AI_Workflow/` |
