# CLAUDE.md — Project Guide for AI Agents

This file is the single source of truth for AI coding agents working on this project.
Read this file before making any changes.

---

## Project Overview

- **Project**: Turn-Based Tactical RPG (hex grid, DOS2/BG3 inspired)
- **Engine**: Unity 6000.3.6f1 (Unity 6)
- **Render Pipeline**: Built-in Render Pipeline (with ShaderGraph 17.3.0)
- **Language**: C# (.NET Standard 2.1 / Unity subset)
- **Input**: Unity Input System 1.18.0
- **UI**: uGUI (com.unity.ugui 2.0.0)
- **Platform target**: PC (Windows primary)
- **MCP Tools**: Unity MCP server available for Unity Editor operations

---

## Unity MCP Integration

**IMPORTANT**: This project has Unity MCP tools available for direct Unity Editor operations.

### Available Unity MCP Operations

Use Unity MCP tools for tasks that normally require Unity Editor GUI:
- **Asset management**: Search, create, modify, delete assets
- **ScriptableObject editing**: Modify SO properties via serialized property paths
- **Prefab operations**: Create, modify prefab contents headlessly
- **Scene operations**: Query hierarchy, take screenshots
- **Material/Shader management**: Set properties, assign materials
- **Script operations**: Create, validate, apply structured edits

### When to Use Unity MCP

- **Creating/modifying ScriptableObject assets** (e.g., UnitDefinition, AbilityDefinition, HUDSpriteConfig)
- **Assigning object references** (sprites, prefabs, materials) to SO fields
- **Querying scene hierarchy** or GameObject components
- **Batch asset operations** (search, rename, duplicate)
- **Prefab modifications** without opening prefab mode

### Unity MCP Limitations

- Cannot directly edit `.unity` scene files (binary format)
- Sprite references require GUID + fileID format (use YAML editing for complex cases)
- Some operations may require Unity Editor refresh to take effect

### Example: Assigning Sprites to ScriptableObject

```bash
# Method 1: Direct YAML editing (most reliable for sprites)
1. Read the .asset file to see current YAML structure
2. Find sprite GUIDs from .meta files
3. Edit YAML with format: {fileID: 21300000, guid: <sprite-guid>, type: 3}

# Method 2: Unity MCP manage_scriptable_object (for simple properties)
Use manage_scriptable_object with "modify" action and patches array
```

**Best Practice**: For sprite/texture references, prefer direct YAML editing over MCP tools due to Unity's sub-asset reference complexity.

---

## Key Documents

- `GameOutline.md` — Active game design outline, system architecture, and design decisions
- `GameInfo.md` — Original project blueprint (bilingual EN/CN), phase planning, module map
- `AssetInventory.md` — Complete inventory of all third-party and project assets

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

## UI Design Guidelines

### Current UI System (Session 11-12)

The project uses **uGUI (Unity UI)** with a **sprite-optional architecture**:
- UI components work with or without sprite assets
- Fallback to clean geometric shapes using `DOS2Theme` color palette
- All UI code is in `Assets/_Project/Scripts/UI/`

**Key Files:**
- `DOS2Theme.cs` — Centralized color palette and UI helper methods
- `HUDSpriteConfig.cs` — Optional ScriptableObject for sprite assignments
- `ActionBar.cs` — Bottom ability bar (sprite-aware)
- `TurnOrderBar.cs` — Top turn order display (sprite-aware)
- `PartyPortraitPanel.cs` — Left party status panel (sprite-aware)

### Design Philosophy

**Information Density > Visual Decoration**

For tactical RPGs, players need to see HP, AP, abilities, and turn order at a glance. Prioritize:
1. **Clarity**: High contrast, readable fonts, clear icons
2. **Consistency**: Predictable layout, uniform spacing
3. **Minimalism**: No excessive ornamentation or animation
4. **Functionality**: Every UI element serves a purpose

Reference games: Divinity: Original Sin 2, Baldur's Gate 3, XCOM 2, Into the Breach

### Asset Pack Integration Rules

**CRITICAL**: Before integrating any UI asset pack, verify:

1. **Genre Match**: Is it designed for tactical/strategy games? (Not ARPG/MMO/action games)
2. **9-Slice Support**: Do panel/frame sprites have proper `spriteBorder` values in `.meta` files?
3. **Layout Compatibility**: Does it support dynamic/programmatic UI generation?
4. **Modularity**: Are components separate (borders, backgrounds, icons) or fixed mockups?

**Session 12 Lesson**: Dark Fantasy HUD failed because:
- Designed for ARPG fixed layouts, not tactical dynamic UI
- Most decorative sprites lacked 9-slice borders → severe distortion
- Asset pack mockups showed horizontal bars, our code generates compact panels

**When in doubt**: Keep the current sprite-less UI. It's clean, functional, and proven to work.

### Color Palette (DOS2Theme.cs)

Current colors (dark gothic theme):
```csharp
GoldAccent     = #B8860B  // Dark goldenrod (borders, highlights)
PanelBg        = #0A0A0F  // Deep black-purple (backgrounds)
EnemyRed       = #8B0000  // Blood red (enemy indicators)
HPGreen        = #4CAF50  // Health bars
APGreen        = #00E676  // Action point pips
SyntyDarkBg    = #1A0F1F  // Deep purple-black (panel tint)
SyntyFrameGray = #4A4A4A  // Dark gray (borders)
```

**Important**: When using sprites, set `Image.color = Color.white` to show sprite's original colors. Dark tints on dark sprites = invisible UI.

### Future UI Improvements (Backlog)

See `Docs/UI_Design_Notes.md` for detailed plans. Priority order:
1. Refine spacing and font sizes in current UI
2. Add tooltips for abilities and units
3. Add status effect icons above HP bars
4. Add combat log (scrolling text feed)
5. Custom minimal sprites (if time permits)

**Do not** attempt UI visual overhauls without explicit user approval. Functionality first.

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
