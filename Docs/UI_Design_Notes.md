# UI Design Notes

## Session 12 - Dark Fantasy HUD Integration Attempt (2026-03-04)

### What We Tried
Attempted to integrate Synty's "Dark Fantasy HUD" asset pack into the existing uGUI-based combat UI system.

### Why It Failed
1. **Design Mismatch**: Dark Fantasy HUD is designed for ARPG/action games with fixed layouts, not tactical RPGs with dynamic UI
2. **9-Slice Issues**: Most decorative sprites (Frame_Arch series) lack proper 9-slice borders, causing severe distortion when stretched
3. **Color Problems**: Dark sprites with dark tint (`DOS2Theme.SyntyDarkBg`) resulted in nearly black, invisible UI elements
4. **Layout Incompatibility**: Asset pack expects horizontal bar layouts (see mockups), but our code generates compact vertical/square panels

### Technical Issues Encountered
- `Frame_Arch_Large_01_Background` has `spriteBorder: {x: 0, y: 0, z: 0, w: 0}` - no 9-slice support
- Using `Image.Type.Sliced` on non-9-slice sprites causes whole-sprite stretching and distortion
- `Frame_Box_Large_01_Background` has proper borders but still didn't match our layout needs

### Lessons Learned
1. **Asset Pack Evaluation**: Always check if UI assets are designed for your game genre
2. **9-Slice Requirements**: For programmatic UI generation, assets MUST have proper 9-slice borders
3. **Information > Decoration**: For tactical games, clarity and readability trump visual flair
4. **Test Before Integration**: Preview asset pack mockups against your actual UI layout before committing

### Current Status
**Reverted to original simple uGUI UI** (no sprite config) for the following reasons:
- Clean, readable, functional
- No visual glitches or distortion
- Proven to work with all UI components
- Maintains DOS2-inspired color scheme in code

---

## Future UI Design Plan

### Design Principles (DOS2/BG3 Inspired)
1. **Information Density First**: Players need to see HP, AP, abilities, turn order at a glance
2. **Minimal Decoration**: Simple borders, clean backgrounds, no excessive ornamentation
3. **High Contrast**: Dark backgrounds with bright text/icons for readability
4. **Consistent Layout**: Predictable positions for all UI elements

### Recommended Approach: Custom Minimal UI

#### Color Palette (Already in DOS2Theme.cs)
```csharp
// Backgrounds
PanelBg = #0A0A0F (deep black-purple)
PanelBg85 = 85% alpha version

// Accents
GoldAccent = #B8860B (dark goldenrod)
HPGreen = #4CAF50
APGreen = #00E676
EnemyRed = #8B0000

// Borders
SyntyFrameGray = #4A4A4A (dark gray)
```

#### UI Components Design

**PartyPortraitPanel (Left Side)**
- Dark semi-transparent background (#0A0A0F @ 85%)
- Thin gold border (2px, #B8860B)
- Each portrait slot:
  - Unit name (white, bold, 13px)
  - HP bar (green fill, dark gray background)
  - AP pips (green gems, 6 max)
  - Highlight on active turn (gold glow)

**TurnOrderBar (Top Center)**
- Horizontal dark panel
- Unit slots showing:
  - Small portrait icon
  - Unit name below
  - Team color indicator (green/red border)
- Current turn highlighted with gold frame

**ActionBar (Bottom Center)**
- Wide horizontal panel
- Top section: Unit name + HP bar + AP pips
- Bottom section: 6 ability slots in a row
  - Slot frame (dark gray box)
  - Ability icon (placeholder or actual icon)
  - Hotkey label (1-6)
  - Cooldown overlay if on cooldown
- End Turn button (right side, gold accent)

#### Asset Requirements (If Using External Assets)
When searching for UI asset packs, look for:
- **9-slice border support** on all panel/frame sprites
- **Modular components** (separate borders, backgrounds, buttons)
- **Tactical/Strategy game focus** (not ARPG/MMO)
- **Neutral color palette** (can be tinted in code)

Recommended search terms:
- "RPG UI 9-slice"
- "Tactical Game UI Kit"
- "Strategy HUD Pack"
- "Minimal Fantasy UI"

#### Implementation Strategy
1. **Phase 1**: Keep current code-drawn UI, refine colors and spacing
2. **Phase 2**: Add simple geometric decorations (corner brackets, separator lines)
3. **Phase 3**: If needed, create custom sprites in Photoshop/Figma:
   - Simple bordered panels (9-slice ready)
   - Icon frames
   - Button states (normal/hover/pressed)
4. **Phase 4**: Polish with subtle animations (fade in/out, pulse on hover)

---

## Code Architecture Notes

### Current UI System (Session 11-12)
- **HUDSpriteConfig.cs**: ScriptableObject for optional sprite assignments
- **DOS2Theme.cs**: Centralized color palette and UI helper methods
- **ActionBar.cs**: Bottom ability bar (658 lines, sprite-aware)
- **TurnOrderBar.cs**: Top turn order display (262 lines, sprite-aware)
- **PartyPortraitPanel.cs**: Left party status (382 lines, sprite-aware)

All UI components check `DOS2Theme.HasSprites` and fall back to code-drawn rectangles if no sprites assigned.

### Sprite Integration Points
When sprites are assigned via HUDSpriteConfig:
- `Image.Type.Sliced` for panels/frames (requires 9-slice borders)
- `Image.Type.Simple` for icons/decorations (preserves aspect ratio)
- `Color.white` tint to show sprite's original colors
- Fallback to `DOS2Theme` colors when sprites are null

### Key Design Decision
**Sprite support is optional, not required.** The UI must always work without sprites, using clean geometric shapes and the DOS2Theme color palette.

---

## References

### Successful Tactical Game UIs
- **Divinity: Original Sin 2**: Minimal panels, high information density, gold accents
- **Baldur's Gate 3**: Clean portraits, clear ability icons, subtle borders
- **XCOM 2**: Functional over decorative, high contrast, clear status indicators
- **Into the Breach**: Extremely minimal, grid-focused, color-coded information

### Asset Packs Evaluated
- ❌ **Dark Fantasy HUD** (Synty): ARPG-focused, poor 9-slice support, layout mismatch
- ⏳ **Fantasy Warrior HUD** (Synty): Not tested, likely similar issues
- ⏳ Future evaluation needed for tactical-focused UI packs

---

## Known Issues / Backlog

### Oil Surface Ignition (Session 12)
**Status**: Deferred — needs design work.

**Problem**: Fire Bolt can hit enemies standing on oil puddles, but the oil-to-fire surface conversion isn't triggering properly. The surface interaction system (`SurfaceSystem`) currently handles surface-on-enter effects (e.g., poison damage when walking on poison) but doesn't fully implement **element-to-surface reactions** (e.g., fire ability hitting oil → creates fire surface).

**Root Cause**: The `AbilityExecutor` creates surfaces from ability effects (via `CreateSurface` effect type), but there's no reaction system that checks: "did a fire-element ability land on an oil cell? If so, convert oil → fire." The current `SurfaceSystem.CreateSurface` simply places a new surface, it doesn't check for element interactions with existing surfaces.

**Fix Plan**:
1. Add a `SurfaceReactionTable` (ScriptableObject or static dictionary) mapping element + existing surface → new surface
2. In `AbilityExecutor`, after resolving ability effects on a target cell, check if the ability's element interacts with any surface on that cell
3. Call `SurfaceSystem.ReactSurface(coord, element)` to perform the conversion
4. Add visual feedback (VFX) for surface conversion

**Priority**: Medium — nice-to-have for demo, not blocking core gameplay.

---

## Next Steps (Future Sessions)

1. **Refine Current UI**: Adjust spacing, font sizes, color contrast
2. **Add Tooltips**: Hover over abilities/units to show detailed stats
3. **Status Effect Icons**: Small icons above HP bars for buffs/debuffs
4. **Combat Log**: Scrolling text feed of combat events (bottom-left)
5. **Custom Sprites** (if time permits): Simple bordered panels in Photoshop

**Priority**: Functionality and clarity over visual polish. The game must be playable and readable first.
