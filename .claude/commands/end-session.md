# End Session

结束当前工作 session，更新文档，提交代码，生成下次 session 的上下文恢复提示。

## 执行步骤

1. **更新 `progress_report.md`**
   - 在文件顶部（`---` 分隔线之后）插入新的 session 记录
   - 格式参考已有的 session 记录：日期、session 编号、标题
   - 包含 Completed（完成项）、Files Modified/Created、Key Technical Details
   - 如有未完成工作，记录在 session 条目中

2. **Git 提交和推送**
   - `git add` 所有本 session 修改/新增的项目文件（不包括 Screenshots、.claude/settings.local.json）
   - 用清晰的 commit message 提交，包含 `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>`
   - `git push` 到远程

3. **生成下次 session 恢复提示**
   - 输出一段中文文字，供用户粘贴到新 session 开头
   - 格式如下：

```
你好，这是 Turn-Based Tactical RPG 项目的第 N+1 次开发 session。

请先按顺序读取以下文件了解项目状态：
1. `CLAUDE.md` — 项目规范和架构规则
2. `progress_report.md` — 完整开发历史和当前状态（重点看最新的 Session N 记录）
3. `GameOutline.md` — 游戏设计大纲

上次 session（Session N）完成了：
- [简要列出 2-3 项主要完成内容]

本次 session 的工作重点：
- [列出下一步任务，参考 progress_report.md 的 Next Steps]

未完成的遗留项：
- [如有，列出具体内容]

注意事项：
- [如有特殊注意事项]

读完文档后告诉我你已了解项目状态，然后我们开始工作。
```

4. **输出 PR 信息**（如果本 session 有需要创建的 PR）
   - 提供 branch 名、建议的 PR title 和 body
   - 如果 `gh auth` 可用则直接创建

## 注意
- 用户说的语言是中文，所有输出用中文
- 不要提交 Screenshots 目录下的截图文件
- 不要提交 .claude/settings.local.json
