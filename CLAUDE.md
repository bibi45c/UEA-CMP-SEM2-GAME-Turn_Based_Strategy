# CLAUDE.md — Project Guide for AI Agents

This file is the single source of truth for AI coding agents working on this project.
Read this file before making any changes.

---

## Project Overview

- **Project**: Turn-Based Tactical RPG (hex grid, DOS2/BG3 inspired)
- **Engine**: Unity 6000.3.6f1 (Unity 6)
- **Render Pipeline**: URP (Universal Render Pipeline)
- **Language**: C# (.NET Standard 2.1 / Unity subset)
- **Input**: Unity Input System 1.18.0
- **UI**: uGUI (com.unity.ugui 2.0.0)
- **Platform target**: PC (Windows primary)

---

## Key Documents

- `GameOutline.md` — Active game design outline, system architecture, and design decisions
- `GameInfo.md` — Original project blueprint (bilingual EN/CN), phase planning, module map

Always read `GameOutline.md` first for the latest design decisions.

---

## Directory Structure

```
Assets/
  _Project/
    Scripts/           # All C# code, organized by module
      Core/            # Bootstrap, EventBus, Session
      Grid/            # HexGrid, Cell, Pathfinding, Surface
      Combat/          # TurnManager, ActionSystem, DamageResolver
      Units/           # UnitRuntime, UnitStats, UnitDefinition
      Abilities/       # AbilityDefinition, AbilityExecutor
      Items/           # Inventory, Equipment
      AI/              # AIBrain, AIScorer
      Camera/          # TacticalCamera
      UI/              # All UI scripts
    Data/              # All ScriptableObject assets
      Units/
      Abilities/
      Items/
      Encounters/
    Scenes/
    Prefabs/
    Art/               # Custom art assets (cinematics, UI art, etc.)
    Audio/
  ThirdParty/          # Synty asset packs (do not modify)
    PolygonDungeonRealms/
    PolygonDarkFortress/
    ...
```

**Rules:**
- All new code goes under `Assets/_Project/Scripts/{ModuleName}/`
- Never modify files under `ThirdParty/`
- ScriptableObject assets go in `Assets/_Project/Data/{Category}/`
- Synty packs go under `ThirdParty/`, keep original directory names

---

## Architecture Principles

### Three-Layer Rule
1. **Unity Binding Layer** — MonoBehaviours: hold serialized refs, receive Unity events, forward to services
2. **Domain Logic Layer** — Plain C# classes: game rules, state, calculations
3. **Presentation Layer** — Visual feedback, VFX, UI updates, camera

### Code Style
- MonoBehaviours must stay **thin** — no heavy logic, no god managers
- Prefer **composition over inheritance**
- Prefer **ScriptableObject** for definitions, plain C# for runtime state
- Avoid static global gameplay state unless absolutely necessary
- Do not place combat rules inside `Update()`
- Derived stat formulas must be centralized (in `UnitStats`), never scattered across UI/combat scripts
- Equipment bonuses flow through stat aggregation, never hard-coded in UI

### Naming Conventions
- C# files: `PascalCase.cs` (e.g., `HexGridMap.cs`, `UnitRuntime.cs`)
- Interfaces: `I` prefix (e.g., `IDamageRandomizer`)
- ScriptableObjects: suffix with `Definition` or `Config` (e.g., `UnitDefinition`, `AbilityDefinition`)
- Runtime instances: suffix with `Runtime` or `Instance` (e.g., `UnitRuntime`, `StatusInstance`)
- Private fields: `_camelCase` with underscore prefix
- Public properties: `PascalCase`
- Constants/statics: `PascalCase`
- Enums: `PascalCase` for type, `PascalCase` for values

### File Organization
- One primary class per file
- File name must match class name
- Keep files under 300 lines when possible; split if growing beyond 400
- Group `using` directives: System → Unity → Project namespaces

---

## Design Decisions Log

Decisions made during planning discussions. Do not contradict these without explicit user approval.

| Decision | Choice | Notes |
|----------|--------|-------|
| Grid system | Hybrid (Option C) | Hex grid for logic, free movement for visuals, surface effects on cells |
| Height system | Discrete levels (Option B) | `int heightLevel` 0/1/2, final solution, no upgrade to continuous needed |
| Dice/randomness | Fixed formula (Option A) | Damage = formula ± 10%, hit = 100%, reserve `IDamageRandomizer` interface for future dice/pity system |
| Action economy | 1 move + 1 main + 0~1 bonus | Explicit end turn, order-free (move before or after attack) |
| Vision/fog | Full visibility (Option A) | No fog in combat; add LoS check after core loop stable; fog only for Phase 2 exploration |
| Cover system | Two-tier cover (Option B) | HalfCover -25% dmg, FullCover blocks ranged; directional; Phase 2 adds destructible cover |
| Movement snap | Snap to cell center (Option A) | Logic snaps to center; visual adds ±0.1m random offset for natural look |
| Hex cell size | R=0.75m (compact) | Configurable; grid width 1.5m per cell; 10×12 arena ≈ 13×16m |
| Unit size | Multi-cell (Option B) | Size 1=1cell, Size 2=7cells, Size 3=19cells; Phase 1 all size=1, add size=2 boss later |

---

## Common Mistakes to Avoid

- **Do not** create manager MonoBehaviours that own all game logic — keep them thin
- **Do not** scatter stat calculations across files — centralize in `UnitStats`
- **Do not** hard-code ability effects — use data-driven `AbilityDefinition` SOs
- **Do not** mix grid logic with visual rendering — grid is authority, visuals are dressing
- **Do not** put new code in `Legacy/` or modify `ThirdParty/`
- **Do not** use `Find()` or `FindObjectOfType()` at runtime for critical references — wire via serialization or dependency injection
- **Do not** commit `.meta` files for assets that don't exist (orphaned metas)
- **Do not** use `git add -A` blindly — check for large binary files or secrets first

---

## Git & PR Conventions

### Branch Naming
- Feature: `feature/<short-description>`
- Fix: `fix/<short-description>`
- Refactor: `refactor/<short-description>`

### Commit Messages
- Short imperative summary (< 72 chars)
- Examples: `Add hex grid generation and cell data`, `Fix pathfinding cost for height difference`
- Always include `Co-Authored-By` when AI-assisted

### PR Template
```markdown
## Summary
- [1-3 bullet points describing what changed and why]

## Systems Affected
- [List modules touched: Grid, Combat, Units, etc.]

## Test Plan
- [ ] Scene opens without errors
- [ ] [Specific test steps...]

## Screenshots / GIFs
[If visual changes]
```

---

## Unity-Specific Reminders

- Unity version: **6000.3.6f1** — do not upgrade without explicit approval
- Always use **Input System** (not legacy Input), package version 1.18.0
- UI system: **uGUI** (Canvas-based), not UI Toolkit
- Scene hierarchy for combat must follow the root structure defined in `GameOutline.md` Section 11 / `GameInfo.md` Section 11
- When creating new scripts, always add proper namespace: `namespace TurnBasedTactics.{Module}`
- Serialized fields use `[SerializeField] private` not `public`

---

## Testing Checklist

Before declaring any system "done":
- [ ] Scene opens and plays without NullReferenceException
- [ ] No compiler errors or warnings from new code
- [ ] System works in isolation (can test without all other systems running)
- [ ] Edge cases handled (empty grid, dead unit acting, 0 HP, etc.)
