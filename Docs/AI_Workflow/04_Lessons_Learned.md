# 04 · Lessons Learned

按主题整理 17 个 session 中真实踩过的坑。每条都写成"现象 → 原因 → 做法"，可以直接搬进下一个项目的 `CLAUDE.md`。

---

## Unity MCP

| 现象 | 原因 | 做法 |
|---|---|---|
| MCP 连不上，反复重启 Unity / IDE | 传输方式（stdio / http）、端口和客户端配置不一致；同一时间被另一个客户端占用 | 先查官方 README 和日志；一次只让一个 agent 连 Unity；把可用配置写进文档 |
| 用 Write 工具新建的 C# 文件 Unity 不编译 | 文件编码问题，Unity 静默失败 | 新建脚本优先用 `create_script`，或确认 UTF-8 编码后 `refresh_unity` + `read_console` |
| 给 SO 赋 Sprite 引用失败 | MCP 对 sub-asset ObjectReference 支持有限 | 直接编辑 `.asset` YAML：`{fileID: 21300000, guid: <guid>, type: 3}`（已写入 `CLAUDE.md`） |
| MCP 修改没生效 / 改到了别处 | 会话在 worktree，Unity 打开的是主仓库 | 交接 prompt 中写明"MCP 改场景走主项目路径"；多实例时 `set_active_instance` |
| 用户说"你直接调 mcp 做" | agent 把可自动化的步骤交给了人 | 默认用 `execute_code` / `manage_*` 完成场景接线，只有视觉判断才找人 |
| `manage_gameobject` 的 parent、动画状态参数名不对 | API 细节与直觉不同 | 把踩到的 API 细节记进 memory（如 parent 用名称字符串、`fromState`/`toState`） |

## Unity 引擎层

| 现象 | 原因 | 做法 |
|---|---|---|
| 依赖顺序导致 NullReference | 在 `Awake()` 里依赖其他系统 | 统一由 `GameBootstrap` 按顺序初始化 |
| 命名空间 `TurnBasedTactics.Camera` 与 `UnityEngine.Camera` 冲突 | 模块名撞类名 | 使用 `global::UnityEngine.Camera` 或 using 别名 |
| 人物悬浮 / 走路一跳一跳 / 一直往上飞 | CharacterController center 错位；每帧调用两次 `Move()`；Synty Animator 开了 Root Motion | 逐一排查：碰撞体中心 → 每帧单次 Move → 关 Root Motion |
| 角色粉色 | Synty 角色 shader 缺失 | 换用可用材质，记录 GUID |
| 深色 sprite + 深色 tint → UI 看不见 | tint 叠加 | 用 sprite 时 `Image.color = Color.white` |
| 素材包 UI 拉伸变形 | sprite 没有 9-slice border | 引入前检查 `.meta` 的 `spriteBorder`；不合适就回滚（S12） |
| 动态创建 InputAction 损坏资源 | 运行时修改 InputActionAsset | 只用 `FindAction()` 查已有 action |
| **Editor 正常、Build 卡死**（敌人攻击后卡住、血量归零不死亡） | Build 剥离了 `Particles/Standard Unlit`，`Shader.Find` 返回 null → VFX 订阅者抛异常 → 被 AI 的 catch 吞掉，跳过了死亡处理和回合切换 | Development Build + 实时看 `Player.log`；shader 做 null 检查并回退；**每个事件发布单独 try/catch**，保证一个订阅者出错不会打断核心流程 |
| 切场景卡顿、BGM 提前响 | 同步加载 | 过场期间 `LoadSceneAsync(allowSceneActivation=false)` 预加载，播完再激活 |
| 过场之间露出底层场景 | CanvasGroup alpha 残留、`??` 对 Unity 对象的伪 null 判断 | 显式 null 判断；`keepBlackAfter` 保持黑幕 |

## 上下文与会话管理

- **长会话会被压缩**：S15 一个会话压缩 3 次，细节会丢。→ 关键结论随时写进文档（分镜、台词、设计决策），而不是只留在聊天里。
- **压缩后可能出现异常输出**（如突然换成其他语言）。→ 发现后直接纠正，必要时开新会话并用交接 prompt 恢复。
- **复杂任务切 Opus**：多文件、跨场景、时序相关的问题（过场 + 异步加载）用更强模型更省时间。
- **一次给多个需求时**（S16 一条消息 6 点），让 agent 用 TodoWrite 拆清单逐项完成并汇报。
- **只读核对交给 subagent**，主会话保持干净。

## 协作习惯

- **先说思路再动手**；方案给选项，人来选。
- **敢回滚**：ESC 菜单做完不满意 → "回滚到上一版"，换成更小的 FPS + 画质按钮；Dark Fantasy HUD 不适合 → 回滚并写复盘文档。回滚本身是正常流程。
- **AI 生成的美术素材交给 agent 识图配文**：用户生成分镜图放进目录，agent 识别画面后写台词并接入过场系统；台词以 `Storyboard_Complete.md` 为唯一来源，代码与 md 双向同步。
- **文案去模板化**：报告/解说稿要求"像写给人看的终稿"，避免出现评分项标题、自我解释性句子；可指定词汇难度（如雅思 6 分）。
- **提交规范写进 memory**：commit/PR 用英文，聊天可用中文。
- **按评分标准倒推功能**：先让 agent 读要求做 gap analysis，再决定做什么（如 FPS 对应 "game statistics"）。

## 记录与可追溯性

- **Claude Code 本地 transcript 会被定期清理**（默认约 30 天），Session 3–14 的原始对话因此丢失。→ 重要项目应定期导出（本目录 `tools/session_digest.py`），或调大 `cleanupPeriodDays`。
- **Codex 记录也可能"消失"**（05-20 事件），需要从备份恢复并注意中文编码。
- 因为有 `progress_report.md` + 规范的 git 提交，即使聊天记录丢失，开发历史仍可完整重建 —— 这是"文档即上下文"最直接的回报。
- 不要把作业 PDF、构建产物、录屏、聊天导出等提交进仓库：用 `.gitignore`（如 `Docs/Coursework/`）隔离，提交前检查 `git status`。
