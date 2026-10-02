"""Build a sanitized Markdown digest of AI-agent sessions for this project.

Reads Claude Code transcripts (~/.claude/projects/<project>*/**.jsonl) and
Codex rollouts (~/.codex/sessions/**/rollout-*.jsonl) whose cwd matches the
project, then writes one entry per session: time range, models, tool / skill /
MCP / slash-command counts and the user's prompts (truncated, images and
local paths stripped).

Usage:
    python session_digest.py --match 6056B --out prompt_log.md
"""
import argparse
import collections
import glob
import json
import os
import re

HOME = os.path.expanduser("~")


def sanitize(text, limit):
    text = re.sub(r"data:image/[^\s\"')]+", "<image>", text)
    text = re.sub(r"\b\d{1,3}(?:\.\d{1,3}){3}\b", "<ip>", text)
    text = re.sub(r"[A-Za-z]:[\\/]+Users[\\/]+[^\\/\s]+", "~", text, flags=re.I)
    text = re.sub(r"\s+", " ", text).strip()
    return text[:limit] + ("…" if len(text) > limit else "")


def new_session(source, path):
    return dict(source=source, path=path, ts=[], models=collections.Counter(),
                tools=collections.Counter(), skills=collections.Counter(),
                mcp=collections.Counter(), cmds=collections.Counter(),
                compactions=0, prompts=[], title=None)


def read_claude(match, limit):
    sessions = []
    for path in glob.glob(os.path.join(HOME, ".claude", "projects", f"*{match}*", "*.jsonl")):
        s = new_session("Claude Code", path)
        for line in open(path, encoding="utf-8"):
            try:
                e = json.loads(line)
            except ValueError:
                continue
            if e.get("timestamp"):
                s["ts"].append(e["timestamp"])
            if e.get("type") in ("summary", "ai-title", "custom-title"):
                s["title"] = e.get("summary") or e.get("aiTitle") or e.get("customTitle")
            msg = e.get("message") or {}
            if e.get("type") == "assistant":
                if msg.get("model") and not msg["model"].startswith("<"):
                    s["models"][msg["model"]] += 1
                for c in msg.get("content") or []:
                    if not isinstance(c, dict) or c.get("type") != "tool_use":
                        continue
                    s["tools"][c["name"]] += 1
                    if c["name"] == "Skill":
                        s["skills"][(c.get("input") or {}).get("skill")] += 1
                    if c["name"].startswith("mcp__"):
                        s["mcp"][c["name"]] += 1
            elif e.get("type") == "user" and not e.get("isMeta") and not e.get("isSidechain"):
                content = msg.get("content")
                texts = [content] if isinstance(content, str) else [
                    c.get("text", "") for c in content or [] if isinstance(c, dict) and c.get("type") == "text"]
                for t in texts:
                    cmd = re.search(r"<command-name>/?([^<]+)</command-name>", t)
                    if cmd:
                        s["cmds"][cmd.group(1)] += 1
                        s["prompts"].append((e.get("timestamp", ""), f"`/{cmd.group(1)}`"))
                    elif t.startswith("This session is being continued"):
                        s["compactions"] += 1
                        s["prompts"].append((e.get("timestamp", ""), "*(context compacted — summary injected)*"))
                    elif t and not t.startswith("<") and not t.startswith("[Request interrupted"):
                        s["prompts"].append((e.get("timestamp", ""), sanitize(t, limit)))
        sessions.append(s)
    return sessions


def read_codex(match, limit):
    sessions = []
    for path in glob.glob(os.path.join(HOME, ".codex", "sessions", "**", "rollout-*.jsonl"), recursive=True):
        lines = open(path, encoding="utf-8").read().splitlines()
        try:
            meta = json.loads(lines[0]).get("payload", {})
        except (ValueError, IndexError):
            continue
        if match not in meta.get("cwd", ""):
            continue
        s = new_session("Codex", path)
        for line in lines:
            try:
                e = json.loads(line)
            except ValueError:
                continue
            p = e.get("payload", {})
            if e.get("timestamp"):
                s["ts"].append(e["timestamp"])
            if e.get("type") == "turn_context" and p.get("model"):
                s["models"][p["model"]] += 1
            if p.get("type") in ("function_call", "custom_tool_call"):
                s["tools"][p.get("name")] += 1
                args = p.get("arguments") or p.get("input") or ""
                for skill in re.findall(r"skills[\\/]+([\w\-]+)[\\/]+SKILL\.md", str(args)):
                    s["skills"][skill] += 1
            if p.get("type") == "message" and p.get("role") == "user":
                t = " ".join(c.get("text", "") for c in p.get("content", []) if isinstance(c, dict))
                req = re.search(r"## My request for Codex:\s*(.*)", t, re.S)
                t = req.group(1) if req else t
                if t.strip() and not t.lstrip().startswith(("<", "#")):
                    s["prompts"].append((e.get("timestamp", ""), sanitize(t, limit)))
        # Skip the auto-review side threads Codex spawns for permission checks
        if s["prompts"] and not s["prompts"][0][1].startswith("The following is the Codex agent history"):
            sessions.append(s)
    return sessions


def write(sessions, out):
    sessions.sort(key=lambda s: min(s["ts"]) if s["ts"] else "")
    with open(out, "w", encoding="utf-8") as o:
        o.write("# Prompt Log (auto-generated)\n\n> Generated by `tools/session_digest.py`. "
                "Prompts are truncated; images, IPs and local user paths are stripped.\n")
        for s in sessions:
            start, end = (min(s["ts"])[:16], max(s["ts"])[:16]) if s["ts"] else ("?", "?")
            o.write(f"\n## {start} → {end} · {s['source']}\n\n")
            if s["title"]:
                o.write(f"- **Title**: {s['title']}\n")
            o.write(f"- **Models**: {', '.join(s['models']) or '—'}\n")
            o.write(f"- **Top tools**: {', '.join(f'{k}×{v}' for k, v in s['tools'].most_common(8)) or '—'}\n")
            if s["skills"]:
                o.write(f"- **Skills**: {', '.join(s['skills'])}\n")
            if s["cmds"]:
                o.write(f"- **Slash commands**: {', '.join(f'/{k}×{v}' for k, v in s['cmds'].items())}\n")
            if s["compactions"]:
                o.write(f"- **Context compactions**: {s['compactions']}\n")
            o.write("\n")
            for ts, p in s["prompts"]:
                o.write(f"1. `{ts[5:16].replace('T', ' ')}` {p}\n")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--match", default="6056B", help="substring of the project path / cwd")
    ap.add_argument("--out", default="prompt_log.md")
    ap.add_argument("--limit", type=int, default=280, help="max characters per prompt")
    args = ap.parse_args()
    write(read_claude(args.match, args.limit) + read_codex(args.match, args.limit), args.out)
