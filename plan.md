# HUD Overhaul Plan — DOS2 Style Combat UI

## Goal
Replace the current placeholder UI (OnGUI + plain boxes) with a proper uGUI Canvas-based HUD inspired by Divinity: Original Sin 2's combat interface.

---

## DOS2 Reference Layout
```
┌──────────────────────────────────────────────────────────────┐
│ [Party Portraits]        [TURN ORDER BAR]         [Round N]  │
│  Upper-Left               Top-Center              Top-Right  │
│  ┌──────────┐  ┌──┬──┬──┬──┬──┬──┐                          │
│  │ Portrait │  │P1│P2│E1│P3│E2│..│  ← faction-colored frames│
│  │ HP ████░ │  └──┴──┴──┴──┴──┴──┘                          │
│  │ AP ●●●○  │                                                │
│  ├──────────┤                                                │
│  │ Portrait │                                                │
│  │ HP ████░ │                                                │
│  │ AP ●●○○  │           [3D GAME WORLD]                      │
│  └──────────┘                                                │
│                                                              │
│                    [AP Pips: ●●●○○○]                         │
│                    [HP Bar ████████░░]                        │
│   ┌──────────────────────────────────────────────────┐       │
│   │ [Ability1][Ability2][Ability3][Move] ... [EndTurn]│       │
│   │         BOTTOM ACTION BAR (Hotbar)               │       │
│   └──────────────────────────────────────────────────┘       │
│   "Click a target to attack..."  ← hint text                │
└──────────────────────────────────────────────────────────────┘
```

---

## DOS2 Color Palette (exact hex from Larian wiki)
| Role           | Hex       | Usage                                    |
|----------------|-----------|------------------------------------------|
| Gold Accent    | `#C7A758` | Borders, dividers, active highlights     |
| Panel BG       | `#0D0D14` | Dark near-black panel backgrounds        |
| Panel BG Alt   | `#1A1A24` | Slightly lighter panel fill              |
| HP Red         | `#D7001F` | Enemy frames, HP bars                    |
| Party Blue     | `#00A2FD` | Player faction, ally outlines            |
| Enemy Red      | `#D7001F` | Enemy faction frames                     |
| Ally Green     | `#11D77A` | Healing, ally indicators                 |
| AP Green       | `#00F27D` | Available AP pips                        |
| AP Used Red    | `#D7001F` | Previewed cost AP pips                   |
| Text White     | `#FFFFFF` | Primary text                             |
| Text Gray      | `#A8A8A8` | Secondary/hint text                      |
| Highlight Gold | `#FFD400` | Active unit border, selection highlight  |

---

## Affected Files

### New Files
1. **`Assets/_Project/Scripts/UI/ActionBar.cs`** — Bottom hotbar with ability slots, AP pips, HP bar
2. **`Assets/_Project/Scripts/UI/DOS2Theme.cs`** — Static color/style constants (the palette above)

### Modified Files
3. **`Assets/_Project/Scripts/UI/TurnOrderBar.cs`** — Restyle with DOS2 colors, faction frames, active glow
4. **`Assets/_Project/Scripts/UI/PartyPortraitPanel.cs`** — Restyle with DOS2 portrait layout + AP pips
5. **`Assets/_Project/Scripts/UI/CombatHudController.cs`** — Gut OnGUI, replace with thin wrapper or remove entirely (ActionBar takes over)
6. **`Assets/_Project/Scripts/Core/GameBootstrap.cs`** — Wire `ActionBar` initialization, remove old HUD init if replaced

### Untouched
- `CombatResultsScreen.cs` — Already decent (dark overlay + panel), minor color tweak only
- `CombatUIManager.cs` — World-space HP bars, keep as is
- `FloatingDamageText.cs` — Working well, no change
- `UnitWorldUI.cs` — Working well, no change

---

## Step-by-Step Implementation

### Step 1: Create `DOS2Theme.cs` (static palette)
A plain C# class with:
- All color constants from the palette table above
- Helper to create styled UI `Image` with border (9-slice or code-drawn)
- Reusable `CreatePanel()`, `CreateBorder()`, `CreateText()` factory methods

### Step 2: Create `ActionBar.cs` (replace CombatHudController)
The main bottom HUD, structured as:

```
[ActionBarCanvas] ScreenSpaceOverlay, sortOrder=15
└── BarRoot (anchor: bottom-center, ~70% width)
    ├── APPipRow (HorizontalLayout, centered above bar)
    │   └── 6x APPip images (green filled / dark empty)
    ├── HPBarContainer (centered above AP pips)
    │   ├── HPBarBg (dark)
    │   └── HPBarFill (red gradient)
    ├── HotbarFrame (dark panel with gold border)
    │   ├── AbilitySlotContainer (HorizontalLayout)
    │   │   ├── Slot_Move (icon + label + AP cost overlay)
    │   │   ├── Slot_Ability1
    │   │   ├── Slot_Ability2
    │   │   ├── ...
    │   │   ├── Slot_Cancel
    │   │   └── Slot_EndTurn
    │   └── HintText (below slots)
    └── UnitNameLabel (above HP bar)
```

**Each ability slot:**
- 64x64 px square with dark bg + gold 2px border
- Icon image (from Fantasy RPG Icons Pack, fallback to colored square)
- AP cost badge (small circle in bottom-right corner, gold text on dark bg)
- Hover: brighten border to `#FFD400`
- Queued/selected: bright gold border + slight scale up
- Disabled (can't afford): dim to 40% alpha
- Keyboard shortcut number shown top-left

**AP Pips:**
- 6 circles in a row (max AP)
- Filled green `#00F27D` = available
- Empty dark `#1A1A24` = spent
- Red preview `#D7001F` = cost of hovered ability

**HP Bar:**
- Thin bar (~6px height) showing active unit's HP
- Fill color: green→yellow→red gradient based on HP%

**Hint text:**
- Below the hotbar frame
- Gray `#A8A8A8` text showing current action hint

### Step 3: Restyle `TurnOrderBar.cs`
Changes from current:
- **BG color**: `#0D0D14` with 85% alpha (darker)
- **Player slots**: Border `#00A2FD` (blue), inner `#1A1A28`
- **Enemy slots**: Border `#D7001F` (red), inner `#1A1A28`
- **Active unit**: Border `#FFD400` (bright gold), 3px thick
- **Inactive**: Border `#454545` (dark gray), 1px
- **Slot layout**: Name on top, HP text smaller below
- **Font**: White for names, gray for HP

### Step 4: Restyle `PartyPortraitPanel.cs`
Changes from current:
- **BG color**: Dark panel `#0D0D14`
- **Border**: Gold `#C7A758` for active turn, Blue `#00A2FD` for selected, Gray for inactive
- **HP bar**: Red fill on dark bg, with numeric text overlay
- **Add AP pips**: Small row of dots below HP bar (green filled / dark empty)
- **Slot size**: Slightly larger (160×80) for readability
- **Death state**: Grayed out portrait, red X overlay

### Step 5: Restyle `CombatHudController.cs`
- **Option A** (Recommended): Remove OnGUI entirely. Transfer all logic to `ActionBar.cs`. Keep the `IsMouseOverHud` static property but feed it from ActionBar's GraphicRaycaster.
- The `Round N` display moves to top-right corner as a small badge (part of ActionBar or standalone).

### Step 6: Wire in `GameBootstrap.cs`
- Replace `InitializeCombatHud()` call with `InitializeActionBar()`
- Pass necessary references: `CombatSceneController`, `UnitRegistry`

---

## What This Does NOT Include (Future Work)
- ❌ Ability icon assignment on UnitDefinition SOs (can add `Sprite AbilityIcon` field later)
- ❌ Target preview panel (hit chance, damage estimate)
- ❌ Status effect icons on portraits
- ❌ Minimap
- ❌ Combat log panel
- ❌ Tooltip system for ability hover descriptions

---

## Risk / Complexity Notes
- **OnGUI → uGUI migration**: CombatHudController's `IsMouseOverHud` property is used by `TacticalInputHandler` to block clicks. The new ActionBar needs to provide equivalent blocking via `GraphicRaycaster` or `EventSystem.IsPointerOverGameObject()`.
- **Icon assignment**: Abilities currently have no icon sprites. Phase 1 uses colored squares with ability initials; icon sprites can be wired later from the Fantasy RPG Icons Pack.
- All UI is procedurally generated (no prefabs), matching existing project patterns.
