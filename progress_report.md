# Progress Report

This file tracks implementation progress in small, append-only milestones.
Update it after each completed feature slice so the current state is visible
without diffing code.

> **New session? Read these files first:**
> 1. `CLAUDE.md` — Project rules, architecture, naming conventions
> 2. `GameOutline.md` — Full game design outline & system architecture
> 3. This file (`progress_report.md`) — Current state & next steps

---

## 2026-03-04 (Session 12) — UI Polish, Combat Log, Hotkeys, & Phase 1 Audit

### Completed (Dark Fantasy HUD — Attempted & Reverted)
- **HUD Sprite Integration** — Attempted to integrate Dark Fantasy HUD asset pack via Unity MCP tools. Successfully assigned `DarkFantasyHUDConfig` to `GameBootstrap._hudSpriteConfig` in scene.
- **Invisible UI fix** — Fixed dark sprites being tinted with dark colors (`DOS2Theme.SyntyDarkBg`) making UI invisible. Changed all sprite tints to `Color.white` in `PartyPortraitPanel`, `ActionBar`, `TurnOrderBar`.
- **9-Slice distortion** — Discovered most Dark Fantasy sprites lack proper 9-slice borders. Tested `Frame_Box_Large_01_Background` (180px borders) but result still poor for tactical RPG layouts.
- **Reverted** — User decided asset pack is ARPG-focused, not suitable for tactical dynamic UI. Cleared `_hudSpriteConfig` reference in scene. Documented failure analysis in `Docs/UI_Design_Notes.md`.

### Completed (Combat Log)
- **CombatLog.cs** — Created DOS2-style scrolling combat log panel (bottom-right corner). Uses `RectMask2D` for clean text clipping. Subscribes to: `CombatStartedEvent`, `RoundStartedEvent`, `TurnStartedEvent`, `UnitDamagedEvent`, `UnitHealedEvent`, `UnitDiedEvent`, `UnitMoveCompletedEvent`. Rich text color coding per event type. Panel: 380×200px, font size 13px.
- **GameBootstrap wiring** — Added `InitializeCombatLog()` method, passes `UnitRegistry` to `CombatLog.Initialize()` for unit name resolution.

### Completed (ActionBar Improvements)
- **Text layout fix** — Ability initials: `CreateOutlinedText`, 18px bold. Shortcut numbers: gold color (`DOS2Theme.GoldAccent`), bold, 10px. Hint text: `CreateOutlinedText`, italic, 11px.
- **Hotkey bindings** — Added `HandleHotkeys()` in `ActionBar.Update()` using `Keyboard.current` from Input System:
  - `1` = Move, `2/3/4/5/6` = Abilities (mapped to slots), `C` = Cancel queued action, `Space` = End Turn (existing).

### Completed (Hex Grid Visibility)
- **Configurable visibility** — Added `_showGridInGame` serialized field to `HexGridVisualizer`. Always visible in Editor, uses config value in builds. Set to `false` in scene.

### Completed (Archer Ranged Attack)
- **ThrowSpear.asset** — Created ranged ability: range 4, AP cost 2, physical damage, Finesse scaling 1.1. GUID: `645de0d0297459a48be04da623a087e9`.
- **Archer_01.asset** — Added ThrowSpear as first ability (before BasicAttack). Changed weapon to `SM_Wep_Spear_01` with Y rotation offset 90°.

### Completed (Oil Puddle Test Setup)
- **CreateTestOilPuddles** — Added to `GameBootstrap.InitializeCombat()`. Places oil surfaces on enemy cells (40,45) and (42,45) plus adjacent cells. Moved call to after `SurfaceSystem` initialization.
- **SurfaceSystem.GetDefinition()** — Added method to look up registered `SurfaceDefinition` by `SurfaceType`.

### Completed (Attack Animation Tuning)
- **Slower animations** — Added `_actionAnimationSpeed = 0.45f` to `UnitVisual`. Sets animator to 45% speed during attacks, resets after 2s. Post-action wait: 2.0s in `CombatSceneController`.

### Completed (Documentation)
- **Docs/UI_Design_Notes.md** — Created comprehensive UI design notes: Dark Fantasy HUD failure analysis, future UI design plan, color palette reference, asset pack evaluation criteria, known issues backlog (oil ignition).
- **CLAUDE.md** — Added "UI Design Guidelines" section with design philosophy, asset pack rules, color palette, and lessons learned.

### Phase 1 Completion Audit

| Feature | Status | Notes |
|---------|--------|-------|
| Abilities | 4/6-10 | BasicAttack, FireBolt, BasicHeal, ThrowSpear. Missing control/support skills |
| Status Effects | 4/4-6 | Burning, Poisoned, Blessed, Frozen ✅ |
| Items & Equipment | 0/5-10 | **Not implemented** — empty folders, no scripts |
| Unit Definitions | 5 (2+3) | Warrior, Mage (player) + SkeletonKnight, GoblinWarrior, Archer (enemy) |
| Surfaces | 4/2-3 | Oil, Fire, Poison, Ice + reaction table ✅ |
| Encounter System | Partial | Framework exists, no asset files |
| Target Preview | Missing | No hover damage prediction UI |
| Damage Popup | ✅ | FloatingDamageText working |
| Surface Tooltip | Missing | No hover tooltip |
| Cover System | ✅ | Half/Full directional cover |
| Height System | Partial | Framework only, combat bonuses not coded |
| Line of Sight | ✅ | Directional LoS with height awareness |
| Combat Log | ✅ | New this session |
| Combat VFX/Audio | ✅ | Particles + audio events |
| Victory/Defeat | ✅ | Results screen working |

### Files Modified
- `Assets/_Project/Scripts/UI/CombatLog.cs` (CREATED)
- `Assets/_Project/Scripts/UI/ActionBar.cs` (hotkeys, text layout)
- `Assets/_Project/Scripts/UI/PartyPortraitPanel.cs` (sprite tint fix)
- `Assets/_Project/Scripts/UI/TurnOrderBar.cs` (sprite tint fix)
- `Assets/_Project/Scripts/UI/CombatHudController.cs`
- `Assets/_Project/Scripts/Grid/HexGridVisualizer.cs` (visibility toggle)
- `Assets/_Project/Scripts/Grid/SurfaceSystem.cs` (GetDefinition method)
- `Assets/_Project/Scripts/Grid/OilPuddleVisual.cs` (CREATED)
- `Assets/_Project/Scripts/Units/UnitVisual.cs` (animation speed)
- `Assets/_Project/Scripts/Combat/CombatSceneController.cs` (post-action wait, oil puddles)
- `Assets/_Project/Scripts/Core/GameBootstrap.cs` (combat log init, oil puddles)
- `Assets/_Project/Data/Abilities/ThrowSpear.asset` (CREATED)
- `Assets/_Project/Data/Units/Archer_01.asset` (ranged weapon + ability)
- `Assets/_Project/Data/DarkFantasyHUDConfig.asset` (modified, then unused)
- `Assets/_Project/Scenes/Combat/Combat_RuinsPrototype_01.unity` (grid visibility, HUD config cleared)
- `Docs/UI_Design_Notes.md` (CREATED)
- `CLAUDE.md` (UI guidelines added)

### Next Steps (Session 13)
1. **Warrior Stun Ability** — "Shield Bash" with 50% stun chance + pity system (累加概率保底)
2. **Equipment System** — EquipmentDefinition SO, EquipmentLoadout class, starting equipment for units, stat bonuses flowing through UnitStats
3. **Exploration Mode** — Pre-combat exploration phase: party following (Mage leader), enemy patrol, encounter trigger on proximity → transition to combat

### Known Issues
- **Oil ignition** — Fire abilities don't convert oil surfaces to fire. Needs `SurfaceReactionTable` system (documented in `Docs/UI_Design_Notes.md`)
- **FireBolt ground targeting** — Currently `targetingType: SingleEnemy` only, cannot target ground tiles
- **3rd player unit missing** — Only Warrior + Mage as player units, need a 3rd

---

## 2026-03-04 (Session 11) — Combat Audio System & DOS2 HUD Research

### Completed (Asset Management)
- **Large asset imports** — Imported Audio (6.6GB), Icon (2.9GB), HUD (2MB) asset packs locally. Added them to `.gitignore` to keep repo size manageable (~365MB). Assets available on local disk but not tracked by git.
- **.gitignore cleanup** — Added exclusions for `Assets/ThirdParty/Audio/`, `Icon/`, `HUD/`, `Materials/`, `Assets/Synty/`, `Assets/Screenshots/`, orphaned meta files, and Windows `NUL` artifact.
- **Build settings** — Registered `Combat_RuinsPrototype_01` scene in EditorBuildSettings.
- **InfinityPBR fix** — Deleted broken `SupportFilesCheckerAudio.cs` editor script from InfinityPBR audio pack (referenced missing `SupportFilesChecker` class, blocked all compilation).

### Completed (Combat Audio System)
- **CombatAudioConfig** — ScriptableObject (`Assets/_Project/Data/Audio/CombatAudioConfig.asset`) with all audio clip slots and tuning params. Categories: BGM (Intro/Loop/Victory/Defeat), Melee (Swing + Hit), Ranged, Elemental Magic (Fire/Ice/Lightning/Holy), Heal, Death, Footsteps, Buff, UI (Select/Confirm/TurnStart). Volume/pitch variation controls.
- **CombatAudioManager** — EventBus-driven audio manager attached to CombatRoot. Three-layer audio architecture:
  - **Music layer**: Two AudioSources for crossfade. Combat start plays Intro → auto-transitions to Loop. Victory/Defeat crossfades to ending music.
  - **SFX layer**: 8-channel AudioSource pool for polyphony. Random clip selection from arrays. ±8% pitch variation for natural feel.
  - **Movement**: Coroutine-based footstep loop (0.35s interval) during unit movement.
- **Audio clip assignments** — 45 clips wired via direct YAML editing (Unity MCP can't set ObjectReference properties). Sources: Action_RPG_SFX (melee/ranged/death/footsteps/UI), Big Fantasy RPG Music Bundle (BGM), RPG Magic Elemental Pack (Fire/Ice/Lightning/Holy cast sounds, Heal).
- **GameBootstrap integration** — `InitializeCombatAudio()` added after VFX init. Reads `_audioConfig` serialized field. Auto-adds CombatAudioManager component if missing.

### Completed (DOS2 HUD Research)
- **`plan.md`** — Comprehensive DOS2 combat HUD analysis and implementation plan:
  - DOS2 color palette (exact hex from Larian wiki): Gold `#C7A758`, Panel BG `#0D0D14`, Party Blue `#00A2FD`, Enemy Red `#D7001F`, AP Green `#00F27D`, Highlight Gold `#FFD400`
  - Bottom Action Bar design: 64x64 ability slots with gold borders, AP pips (green circles), HP bar, hint text
  - Turn Order Bar restyle: faction-colored frames (blue=ally, red=enemy), active gold highlight
  - Party Portrait Panel restyle: larger slots, AP pips, death state overlay
  - Plan to replace OnGUI-based CombatHudController with proper uGUI Canvas ActionBar
  - Available art assets: Fantasy RPG Icons Pack (skill icons for all classes), Synty HUD Pack (shaders/scripts only, no sprites)

### Files Created This Session
- `Scripts/Combat/CombatAudioConfig.cs` — ScriptableObject for audio clip assignments and tuning
- `Scripts/Combat/CombatAudioManager.cs` — EventBus-driven audio with BGM crossfade and SFX pool
- `Data/Audio/CombatAudioConfig.asset` — SO instance with 45 audio clips wired
- `plan.md` — DOS2-style HUD overhaul research and implementation plan

### Files Modified This Session
- `Scripts/Core/GameBootstrap.cs` — Added `_audioConfig` field and `InitializeCombatAudio()` method
- `.gitignore` — Added large asset exclusions (Audio/Icon/HUD/Screenshots/Synty)
- `ProjectSettings/EditorBuildSettings.asset` — Registered combat scene

### Key Technical Details
- **Audio clip YAML format**: Unity AudioClip references use `{fileID: 8300000, guid: <guid>, type: 3}` in .asset YAML. MCP `manage_scriptable_object` tool can't set ObjectReference properties, so direct YAML editing was required.
- **BGM transition**: Intro clip plays once (non-looping), then coroutine schedules Loop clip after `Intro.length` seconds. Victory/Defeat uses two-source crossfade over 1.5s.
- **SFX pooling**: Round-robin index across 8 AudioSources. No cleanup needed — each source is reused when its index comes around again.
- **Element-based SFX**: `CombatAudioConfig.GetElementClips(ElementType)` returns element-specific cast clip arrays. Falls back to `MeleeHitClips` for `ElementType.None`.

### Next Steps
- [ ] **DOS2 HUD Overhaul** (highest priority) — Implement `plan.md`: ActionBar, restyle TurnOrderBar, restyle PartyPortraitPanel, replace OnGUI with uGUI
- [ ] Ability icon assignment on UnitDefinition SOs (integrate Fantasy RPG Icons Pack)
- [ ] Target preview panel (hit chance, damage estimate)
- [ ] Status effect icons on portraits
- [ ] Weapon offset tuning (low priority)

---

## 2026-03-03 (Session 10) — Phase 1 Polish: VFX, Victory/Defeat Screen & Scene Transitions

### Completed (Attack/Heal Visual Feedback)
- **CameraShake** — Additive Perlin noise camera shake with linear decay. Sits on the camera GO alongside TacticalCamera. ShakeOffset computed on-demand via property getter (no LateUpdate), read by TacticalCamera in `ApplyTransform()`. Stronger shakes override weaker in-progress ones. Three severity tiers: light (0.08, 0.15s), medium (0.15, 0.25s), heavy (0.25, 0.4s).
- **HitFlashEffect** — MaterialPropertyBlock-based emission flash on damaged units. Caches all Renderers on init (excluding SelectionRing), enables `_EMISSION` keyword on each material, flashes element-colored emission on hit, lerps back to black over configurable duration (default 0.12s). Avoids material instance cloning.
- **CombatVFXConfig** — Serializable config class holding all tunable VFX parameters (shake intensity/duration, particle counts, flash duration). Includes static `GetElementColor(ElementType)` mapping: Fire=orange, Ice=light blue, Lightning=yellow, Poison=green, Holy=gold, None=white.
- **CombatVFXManager** — Event-driven orchestrator subscribing to `UnitDamagedEvent` and `UnitHealedEvent`. Dispatches camera shake (severity by HP%), hit flash, and procedural particles. Impact particles: burst of element-colored particles using `Particles/Standard Unlit` shader. Heal particles: rising green-gold cone. Auto-cleanup via `ParticleSystemStopAction.Destroy`.
- **UnitDamagedEvent.Element** — Added `ElementType Element` field to `UnitDamagedEvent` struct. Updated all 5 publish sites (4 in CombatSceneController, 1 in AIBrain) to pass element data for VFX color selection.

### Completed (Victory/Defeat Results Screen)
- **CombatResultsScreen** — Full-screen uGUI overlay (ScreenSpaceOverlay, sortingOrder=100) shown when combat ends. Subscribes to `CombatEndedEvent`. Tracks kills via `UnitDiedEvent` + initial unit snapshots (because dead units are unregistered from UnitRegistry before the event fires). Shows: title (gold VICTORY / red DEFEAT), stats (rounds completed, enemies defeated, allies survived), survivor list with HP bars, Restart/Continue buttons. 1.5s delay for death animation, 0.6s background fade-in.

### Completed (Scene Transition Infrastructure)
- **SceneTransitionManager** — DontDestroyOnLoad singleton with lazy initialization. Creates its own Canvas (sortingOrder=999) with full-screen black Image for fade overlay. Public API: `TransitionToScene(int)`, `TransitionToScene(string)`, `RestartCurrentScene()`. `IsTransitioning` flag blocks double-clicks. Fade uses `Time.unscaledDeltaTime`, `raycastTarget=true` during transition blocks all input. Fade durations: 0.5s out + 0.5s in.
- **EncounterList + EncounterTracker** — `EncounterList` ScriptableObject defines ordered scene name list. `EncounterTracker` static helper tracks `CurrentEncounterIndex` + `ActiveList`, provides `HasNextEncounter()`, `GetNextSceneName()`, `AdvanceEncounter()`, `Reset()`.
- **BuildSettingsHelper** — Editor utility that auto-adds project scenes to build settings on first domain reload (when build settings are empty). Menu item: `TurnBasedTactics → Setup Build Settings`. Successfully auto-registered `Combat_RuinsPrototype_01` at build index 0.
- **CombatResultsScreen button integration** — Restart uses `SceneTransitionManager.Instance.RestartCurrentScene()`, Continue uses `EncounterTracker` to advance to next encounter or falls back to restart.

### Deferred
- **Sound Effects System** — No audio assets exist in the project. Deferred to future session as highest priority TODO. Will need: CombatAudioManager + EventBus subscriptions + audio clips.

### Files Created This Session
- `Scripts/Camera/CameraShake.cs` — Perlin noise camera shake with on-demand offset property
- `Scripts/Units/HitFlashEffect.cs` — MaterialPropertyBlock emission flash on hit
- `Scripts/Combat/CombatVFXConfig.cs` — VFX tuning parameters + element-to-color mapping
- `Scripts/Combat/CombatVFXManager.cs` — Event-driven VFX orchestrator
- `Scripts/UI/CombatResultsScreen.cs` — Victory/Defeat overlay with kill tracking
- `Scripts/Core/SceneTransitionManager.cs` — DontDestroyOnLoad singleton with fade canvas
- `Scripts/Core/EncounterList.cs` — EncounterList SO + EncounterTracker static helper
- `Scripts/Editor/BuildSettingsHelper.cs` — Auto-adds scenes to build settings

### Files Modified This Session
- `Scripts/Units/UnitEvents.cs` — Added `ElementType Element` to `UnitDamagedEvent`
- `Scripts/Combat/CombatSceneController.cs` — Added `Element` to 4 damage event publish sites
- `Scripts/AI/AIBrain.cs` — Added `Element` to AI damage event publish
- `Scripts/Camera/TacticalCamera.cs` — Reads `CameraShake.ShakeOffset` in `ApplyTransform()`
- `Scripts/Units/UnitSpawner.cs` — Added `HitFlashEffect` component creation during spawn
- `Scripts/Core/GameBootstrap.cs` — Added `InitializeCombatVFX()` and `InitializeResultsScreen()` in combat init pipeline

### Key Technical Details
- **CameraShake timing**: Uses `Time.time` for elapsed calculation in property getter — avoids LateUpdate execution order issues with TacticalCamera
- **Kill tracking**: Dead units are unregistered from UnitRegistry in `HandleUnitDeath()` BEFORE `UnitDiedEvent` is published. CombatResultsScreen snapshots all unit data at init time, then looks up team/name from snapshot when death event fires.
- **VFX particle lifetime**: Impact particles use `ParticleSystemStopAction.Destroy` for auto-cleanup. No manual tracking needed.
- **Scene transition fade**: Uses `Time.unscaledDeltaTime` so fade works even if `TimeScale=0`. Canvas at sortingOrder=999 sits above all game UI.
- **Build settings auto-setup**: `[InitializeOnLoadMethod]` triggers on domain reload, adds missing scenes only when build settings are completely empty.

### Next Steps
- [ ] **Sound Effects System** (highest priority) — needs audio asset procurement first
- [ ] OnGUI HUD migration to uGUI Canvas (low priority)
- [ ] Weapon offset tuning for Warrior, Archer, SkeletonKnight, GoblinWarrior (low priority)
- [ ] Create EncounterList SO asset and multi-scene encounter flow (when more scenes exist)

---

## 2026-03-03 (Session 9) — Line of Sight & DOS2 AP System

### Completed (Line of Sight)
- **HasLineOfSight()** — Static method in `CoverResolver` that validates ranged ability line of sight. Walks hex line from attacker to target via `HexCoord.LineTo()`, checking intervening cells for FullCover obstacles. Melee (range ≤ 1) always has LoS. Height bypass: attacker above blocking cell can see over FullCover.
- **AbilityExecutor LoS validation** — After range check, before target collection, LoS is verified. Returns `AbilityResult.Fail("No line of sight to target.")` if blocked.
- **AI LoS filter** — `AIBrain.FindAttackableTarget()` skips enemies behind FullCover obstacles when evaluating attack targets.

### Completed (DOS2 AP System)
- **AP pool replaces boolean actions** — `UnitRuntime` now tracks `MaxAP`/`CurrentAP` (default 6) instead of `HasMovedThisTurn`/`HasActedThisTurn`. Movement costs 1 AP per hex traversed. Abilities cost variable AP (BasicAttack=2, BasicHeal=2, FireBolt=3). Units can move+attack+move in same turn if AP permits.
- **ActionSystem rewrite** — New AP-aware methods: `CanUseAbility(unit, ability)`, `SpendMoveAP(unit, hexCount)`, `SpendAbilityAP(unit, ability)`, `IsTurnComplete(unit)`.
- **Movement range shrinks with AP** — `MovementRangeVisualizer` uses `Mathf.Min(MovementPoints, CurrentAP)` as effective movement range. As AP is spent, reachable area dynamically shrinks.
- **AI AP budgeting** — `AIBrain` reserves AP for ability before spending on movement: `apAvailableForMove = CurrentAP - abilityCost`. No AP wasted when already in range or staying still.
- **HUD AP display** — Title shows "Active Unit: {name} | AP: X/Y". Move button shows "Move (1)". Ability buttons show cost: "Attack (2)", "Fire Bolt (3)". Per-ability `CanUseAbility()` check grays out buttons individually when AP insufficient.
- **ScriptableObject data** — All 5 unit assets set to `_baseActionPoints = 6`. Ability costs: BasicAttack=2, BasicHeal=2, FireBolt=3.

### Files Modified This Session
- `Scripts/Grid/CoverResolver.cs` — Added `HasLineOfSight()` static method
- `Scripts/Abilities/AbilityExecutor.cs` — Added LoS validation before target collection
- `Scripts/AI/AIBrain.cs` — LoS filter in `FindAttackableTarget()` + complete AP-aware rewrite of `ExecuteTurnCoroutine()`, `ExecuteMove()`, `ExecuteAttack()`
- `Scripts/Units/UnitEvents.cs` — Added `HexesMoved` field to `UnitMoveCompletedEvent`
- `Scripts/Units/UnitDefinition.cs` — Default `_baseActionPoints = 6`, clamp range 1-20
- `Scripts/Units/UnitRuntime.cs` — Replaced boolean action tracking with AP pool (`MaxAP`, `CurrentAP`, `HasEnoughAP()`, `SpendAP()`, `ResetAP()`)
- `Scripts/Combat/ActionSystem.cs` — Complete rewrite: AP-aware `CanUseAbility()`, `SpendMoveAP()`, `SpendAbilityAP()`
- `Scripts/Combat/CombatSceneController.cs` — `QueueAbility()` validates AP, `ExecuteAbility()` uses `SpendAbilityAP()`, `OnUnitMoveCompleted()` uses `SpendMoveAP(unit, evt.HexesMoved)`
- `Scripts/Units/UnitMovementSystem.cs` — Tracks `_lastHexesMoved = path.Count - 1`, includes in move completed event
- `Scripts/Units/MovementRangeVisualizer.cs` — Effective range = `Mathf.Min(MovementPoints, CurrentAP)`
- `Scripts/UI/CombatHudController.cs` — AP display in title, per-ability cost labels, per-ability `CanUseAbility()` check
- `Scripts/Core/GameBootstrap.cs` — Added `FaceUnitsTowardEnemies()` (units face opposing team at spawn)
- `Data/Units/*.asset` — All 5 units: `_baseActionPoints = 6`
- `Data/Abilities/BasicAttack.asset` — `_apCost = 2`
- `Data/Abilities/BasicHeal.asset` — `_apCost = 2`
- `Data/Abilities/FireBolt.asset` — `_apCost = 3`

### Key Technical Details
- **AP formula**: 6 AP/turn default. Move = 1 AP/hex. BasicAttack = 2 AP. BasicHeal = 2 AP. FireBolt = 3 AP. Example turn: Move 3 hexes (3 AP) → Attack (2 AP) → 1 AP left.
- **AI budget**: `apReservedForAbility = ability.ApCost`, `apAvailableForMove = CurrentAP - apReservedForAbility`, `moveRange = Min(MovementPoints, apAvailableForMove)`.
- **LoS algorithm**: `attacker.LineTo(target)` returns hex line. Skip endpoints, check intervening cells. FullCover blocks LoS unless attacker's `HeightLevel > cell.HeightLevel`.
- **Backwards compatibility**: `ResetTurnActions()` wrapper in `UnitRuntime` calls `ResetAP()`, so `TurnManager` unchanged.

---

## 2026-03-03 (Session 8) — Death Animation, Status Effects, Surface System & Cover System

### Completed (Cover System)
- **Cover System** — Directional cover that reduces or blocks ranged damage. `CoverResolver` (static utility) traces hex line from attacker to target via `HexCoord.LineTo()`, checks intervening cells for cover, returns the highest `CoverType` found. Height bypass: attacker above target downgrades cover by one tier.
- **Cover damage integration** — `DamageResolver.ResolveAbilityDamage()` now accepts `CoverType` and `isRanged` parameters. HalfCover reduces damage by 25%, FullCover blocks ranged attacks entirely (returns 0 damage with `WasBlockedByCover` flag). Melee attacks (range ≤ 1) ignore cover completely. Backwards-compatible overload preserved.
- **AbilityExecutor cover wiring** — Before each damage effect, computes `CoverResolver.GetCoverBetween()` and passes cover data to `DamageResolver`. `AbilityResult` now carries `WasBlockedByCover` flag for UI feedback.
- **CoverSetup** — MonoBehaviour for manual cover placement via serialized `CoverEntry[]` array (Q, R, CoverType). Applied to grid during `GameBootstrap.InitializeGrid()` after `HexGridMap.Initialize()`.
- **CoverVisualizer** — Renders colored diamond markers on cover cells. Yellow for HalfCover, blue for FullCover. Uses vertex-colored mesh with `Sprites/Default` shader, same pattern as SurfaceVisualizer.
- **Test cover entries** — 3 cover cells placed between player spawn (40-42, 40) and enemy spawn (40-42, 45): 2 HalfCover + 1 FullCover.

### Files Created (Cover System)
- `Scripts/Grid/CoverResolver.cs` — Static utility: `GetCoverBetween()`, `IsRangedAttack()`, `GetDamageMultiplier()`
- `Scripts/Grid/CoverSetup.cs` — Manual cover cell assignment + `CoverEntry` struct
- `Scripts/Grid/CoverVisualizer.cs` — Diamond marker rendering for cover cells

### Files Modified (Cover System)
- `Scripts/Combat/DamageResolver.cs` — Added cover parameters to `ResolveAbilityDamage()`, `DamageResult` gains `WasBlockedByCover` field
- `Scripts/Abilities/AbilityExecutor.cs` — Pre-computes cover per target, passes to DamageResolver, tracks `anyBlocked`
- `Scripts/Abilities/AbilityResult.cs` — Added `WasBlockedByCover` field
- `Scripts/Core/GameBootstrap.cs` — Added `CoverSetup.Initialize()` and `CoverVisualizer.Initialize()` in grid init

---

## 2026-03-03 (Session 8) — Death Animation, Status Effects & Surface System

### Completed
- **Death Animation** — Units no longer vanish instantly on kill. `UnitVisual.PlayDeathAnimation()` plays a death sequence: triggers "Death" animator parameter (if present), fades out all renderers by switching materials to transparent mode and lerping alpha → 0, sinks the unit 0.3m into the ground over 1.2s. `UnitSpawner.DespawnWithDeathAnimation()` orchestrates the visual, then destroys the GameObject. Grid occupancy and registry removal still happen immediately (gameplay unblocked), only the visual persists for the animation duration.
- **Status Effects System** — Full implementation with `StatusDefinition` (SO), `StatusInstance` (runtime), and `StatusManager` (service). Supports per-turn tick damage/healing, stat modifiers (STR/FIN/INT/CON/WIT/MOV), movement/action prevention flags, stackable vs refresh-on-duplicate behavior, and automatic buff recalculation through `UnitStats.SetBuffBonuses()`.
- **StatusManager integration** — Created and owned by `CombatSceneController`. Injected into `AbilityExecutor` via `SetStatusManager()`. Status ticks processed at the start of every unit's turn in `OnTurnStarted()`, publishing damage/heal events. Statuses cleared on death. Status tick kills trigger `HandleUnitDeath()`.
- **AbilityExecutor ApplyStatus** — The placeholder `ApplyStatus` case now calls `_statusManager.ApplyStatus()` with the referenced `StatusDefinition` SO from `EffectPayload.StatusToApply`.
- **FireBolt → Burning** — FireBolt ability now has a second effect: `ApplyStatus → Burning`. Hitting an enemy with Fire Bolt deals magic damage AND sets them ablaze (3 fire damage/turn for 3 turns, ignores armor).
- **2 status assets** — Burning (3 fire dmg/turn, 3 turns, ignores armor), Frozen (2 ice dmg/turn, 2 turns, prevents movement+actions, -3 FIN).
- **Surface System** — Persistent environmental effects on hex cells. `SurfaceDefinition` (SO) defines surface properties (type, duration, tick damage, element, on-enter status, movement cost modifier). `SurfaceInstance` tracks runtime state per cell. `SurfaceSystem` (plain C# service) manages all active surfaces with creation, removal, round-end ticking, and on-enter effects (damage + status application).
- **Surface Reactions** — Static lookup table `Dictionary<(SurfaceType, SurfaceType), SurfaceReaction>`. Implemented reactions: Fire+Oil→Fire(spreads), Fire+Ice→Water, Fire+Water→None(steam), Poison+Fire→Fire(spreads), Water+Electricity→Electricity(spreads), Ice+Fire→Water. Chain reactions spread to neighbors via deferred `_pendingCreations` list (bounded to one level to prevent infinite loops).
- **SurfaceVisualizer** — MonoBehaviour attached to GridSystem GO. Renders colored hex fill overlays for cells with active surfaces using vertex colors and `Sprites/Default` shader. Rebuilds mesh when `SurfaceSystem.IsDirty` flag is set. Edge alpha fade for visual polish.
- **CreateSurface ability effect** — Added `CreateSurface` to `AbilityEffectType` enum and `SurfaceToCreate` field to `EffectPayload`. `AbilityExecutor` handles the new case by calling `_surfaceSystem.CreateSurface()`.
- **FireBolt → Fire Surface** — FireBolt now has a third effect: `CreateSurface → FireSurface`. Hitting an enemy with Fire Bolt deals magic damage + applies Burning status + creates a fire surface on the target's cell.
- **4 surface assets** — FireSurface (2 fire dmg/turn, 3 rounds, applies Burning on enter), OilSurface (5 rounds, +0.5 move cost, ignites with fire), PoisonSurface (2 poison dmg/turn, 4 rounds), IceSurface (3 rounds, -0.3 move cost, applies Frozen on enter).

### Files Created This Session
- `Scripts/Abilities/StatusDefinition.cs` — SO for status effect data (identity, duration, tick damage, stat mods, flags)
- `Scripts/Abilities/StatusInstance.cs` — Runtime state (remaining turns, expiry check)
- `Scripts/Abilities/StatusManager.cs` — Service managing all active statuses + StatusTickResult struct
- `Scripts/Grid/SurfaceDefinition.cs` — SO for surface effect data (type, duration, tick damage, on-enter status, movement cost)
- `Scripts/Grid/SurfaceInstance.cs` — Runtime state (remaining rounds, expiry check)
- `Scripts/Grid/SurfaceSystem.cs` — Core surface manager + reaction table + SurfaceReaction/SurfaceTickResult structs
- `Scripts/Grid/SurfaceVisualizer.cs` — Hex fill overlay rendering for active surfaces
- `Data/Statuses/Burning.asset` — 3 fire dmg/turn, 3 turns, ignores armor, orange tint
- `Data/Statuses/Frozen.asset` — 2 ice dmg/turn, 2 turns, prevents move+act, -3 FIN, blue tint
- `Data/Surfaces/FireSurface.asset` — 2 fire dmg/turn, 3 rounds, applies Burning, orange-red tint
- `Data/Surfaces/OilSurface.asset` — 5 rounds, +0.5 move cost, dark brown tint
- `Data/Surfaces/PoisonSurface.asset` — 2 poison dmg/turn, 4 rounds, green tint
- `Data/Surfaces/IceSurface.asset` — 3 rounds, -0.3 move cost, applies Frozen, light blue tint

### Files Modified This Session
- `Scripts/Units/UnitVisual.cs` — Added `PlayDeathAnimation()`, `DeathSequenceCoroutine()`, `FadeOutCoroutine()`, `SetMaterialTransparent()`, death header fields, `IsDying` property. Refactored selection ring material setup to reuse `SetMaterialTransparent()`.
- `Scripts/Units/UnitSpawner.cs` — Added `DespawnWithDeathAnimation()` method
- `Scripts/Combat/CombatSceneController.cs` — Added `_statusManager`, `_surfaceSystem` fields + properties + serialized `_surfaceDefinitions[]`. Created both in `Initialize()`, registers surface definitions, injects into AbilityExecutor. Status tick processing in `OnTurnStarted()`, surface on-enter effects in `OnTurnStarted()` and `OnUnitMoveCompleted()`, surface round-end ticking in `OnRoundEnded()`. Status cleanup + death animation in `HandleUnitDeath()`.
- `Scripts/AI/AIBrain.cs` — `HandleUnitDeath()` now uses `DespawnWithDeathAnimation()` instead of `DespawnUnit()`
- `Scripts/Abilities/AbilityExecutor.cs` — Added `SetStatusManager()` and `SetSurfaceSystem()` injection, `ApplyStatus` and `CreateSurface` cases functional
- `Scripts/Abilities/AbilityResult.cs` — Added `StatusesApplied` field
- `Scripts/Abilities/EffectPayload.cs` — Added `StatusToApply` (StatusDefinition), `SurfaceToCreate` (SurfaceDefinition)
- `Scripts/Abilities/AbilityEnums.cs` — Added `CreateSurface` to AbilityEffectType enum
- `Scripts/Units/UnitEvents.cs` — Added `StatusAppliedEvent` and `StatusExpiredEvent` structs
- `Scripts/Core/GameBootstrap.cs` — Added `InitializeSurfaceVisualizer()` in combat init pipeline
- `Data/Abilities/FireBolt.asset` — Added ApplyStatus→Burning effect + CreateSurface→FireSurface effect
- `Data/Abilities/BasicAttack.asset`, `BasicHeal.asset` — Added `StatusToApply` and `SurfaceToCreate` fields for struct compatibility

### Key Technical Details
- **Death animation pipeline**: `HandleUnitDeath()` → clear grid/registry immediately → publish `UnitDiedEvent` → `DespawnWithDeathAnimation()` → `UnitVisual.PlayDeathAnimation()` → fade out + sink → `Destroy(GO)`. Gameplay logic isn't blocked by the animation.
- **Status tick flow**: `OnTurnStarted()` → `StatusManager.ProcessTurnStart(unit)` → iterate statuses, apply tick damage/healing, decrement duration, remove expired → return `StatusTickResult` → publish events → check tick-kill.
- **Buff aggregation**: `StatusManager.RecalculateBuffs()` sums all active status stat modifiers and calls `UnitStats.SetBuffBonuses()`. Recalculated on apply, remove, and tick.
- **Non-stackable refresh**: If a non-stackable status is applied again, the old instance is replaced (duration reset) rather than adding a duplicate.
- **Surface creation flow**: `AbilityExecutor` → `SurfaceSystem.CreateSurface()` → check reaction with existing surface → place instance + set `HexCell.Surface` → check neighbor chain reactions → deferred `_pendingCreations` processing.
- **Surface on-enter**: Applied in `OnTurnStarted()` (unit starts turn on surface) and `OnUnitMoveCompleted()` (unit moves onto surface). Deals tick damage (reduced by armor/magic resist unless `TickIgnoresArmor`) and applies on-enter status.
- **Surface round-end**: `ProcessRoundEnd()` ticks all surface durations, removes expired surfaces. Subscribed to `RoundEndedEvent`.
- **SurfaceVisualizer**: Polled via `SurfaceSystem.IsDirty` flag in `Update()`. Rebuilds mesh with 7-vertex hex fills (center + 6 corners at 0.9x radius). Edge vertices use 50% alpha fade. Uses `Sprites/Default` shader with vertex colors + alpha blending.

---

## 2026-03-03 (Session 7) — Data-Driven Ability System

### Completed
- **Ability System** — Replaced hardcoded `BasicAttackSystem` and `HealSkillSystem` with a fully data-driven ability framework. Abilities are authored as `AbilityDefinition` ScriptableObjects in the Inspector — no code changes needed to add new abilities.
- **AbilityDefinition SO** — Fields: name, description, icon, range, apCost, cooldown, targetingType, element, effects[], animationTrigger. Each ability has a list of `EffectPayload` structs (type, baseValue, scalingStat, scalingFactor, statusId).
- **AbilityExecutor** — Stateless execution pipeline: validate → collect targets (via TargetingHelper) → apply effects → return unified `AbilityResult`. Reuses `DamageResolver` for damage math.
- **TargetingHelper** — Static utility for target validation and collection. Supports 4 targeting types: SingleEnemy, SingleAlly, Self, CircleAOE.
- **DamageResolver upgrade** — New `ResolveAbilityDamage()` method handles data-driven damage: reads scaling stat from EffectPayload, applies armor (physical) or magic resistance (magic), crit from Wits. Physical damage preserves Finesse secondary contribution.
- **CombatSceneController refactor** — `QueuedActionType` simplified to None/Move/Ability. Unified `ExecuteAbility()` replaces separate `ExecuteAttack()`/`ExecuteHeal()`. New `QueueAbility(AbilityDefinition)` public API.
- **AIBrain refactor** — Uses `AbilityExecutor` + reads abilities from `unit.Definition.Abilities`. `GetBestOffensiveAbility()` picks first damaging ability. Range checks use ability.Range instead of hardcoded constant.
- **Dynamic HUD buttons** — `CombatHudController` generates ability buttons from active unit's ability list. Mage shows [Move][Attack][Heal][Fire Bolt][Cancel][End Turn]; Warrior shows [Move][Attack][Cancel][End Turn]. Queued ability highlighted with blue tint.
- **3 ability assets** — BasicAttack (melee range=1, STR scaling, physical), BasicHeal (range=2, INT*0.5+4, Holy), FireBolt (range=3, INT*0.8+3, Fire element, magic damage).
- **Unit ability assignments** — Warrior/Archer/SkeletonKnight/GoblinWarrior: [Attack]. Mage: [Attack, Heal, Fire Bolt].

### Files Created This Session
- `Scripts/Abilities/AbilityEnums.cs` — TargetingType, AbilityEffectType, ScalingStat, ElementType enums
- `Scripts/Abilities/EffectPayload.cs` — Serializable effect data struct
- `Scripts/Abilities/AbilityDefinition.cs` — ScriptableObject ability template
- `Scripts/Abilities/AbilityResult.cs` — Unified execution result struct
- `Scripts/Abilities/AbilityExecutor.cs` — Stateless ability execution service
- `Scripts/Abilities/TargetingHelper.cs` — Static target validation/collection utility
- `Data/Abilities/BasicAttack.asset` — Melee attack ability
- `Data/Abilities/BasicHeal.asset` — Heal ally ability
- `Data/Abilities/FireBolt.asset` — Ranged fire magic ability

### Files Modified This Session
- `Scripts/Combat/CombatSceneController.cs` — Replaced BasicAttackSystem/HealSkillSystem with AbilityExecutor, unified ExecuteAbility(), simplified QueuedActionType enum
- `Scripts/Combat/DamageResolver.cs` — Added ResolveAbilityDamage() overload, removed old ResolveBasicAttack()
- `Scripts/AI/AIBrain.cs` — Replaced BasicAttackSystem with AbilityExecutor, added GetBestOffensiveAbility()
- `Scripts/UI/CombatHudController.cs` — Dynamic ability button rendering from unit's ability list
- `Scripts/Units/UnitDefinition.cs` — Added `_abilities` (AbilityDefinition[]) field
- `Data/Units/*.asset` — All 5 unit assets updated with ability references

### Files Deleted This Session
- `Scripts/Combat/BasicAttackSystem.cs` — Replaced by AbilityExecutor
- `Scripts/Combat/HealSkillSystem.cs` — Replaced by AbilityExecutor

### Key Technical Details
- **Ability data flow**: AbilityDefinition (SO) → AbilityExecutor.Execute() → EffectPayload[] iteration → DamageResolver.ResolveAbilityDamage() or Heal calculation → UnitRuntime.TakeDamage()/Heal() → AbilityResult returned → caller publishes EventBus events.
- **Damage formula**: `BaseValue + ScalingStat * ScalingFactor` + Finesse*0.35 (physical only) - Armor/MagicResist. Crit from Wits unchanged.
- **Heal formula**: `BaseValue + ScalingStat * ScalingFactor`, minimum 1, capped at MaxHP.
- **AI ability selection**: `GetBestOffensiveAbility()` returns first `IsDamaging` ability from unit's definition. Phase 1 sufficient; later can add scoring.

### Upgrade Paths (documented for future sessions)
- **DOS2 AP system**: `AbilityDefinition._apCost` field already exists (default=1). Upgrade: change `ActionSystem` from boolean HasMoved/HasActed to int AP pool, change `UnitRuntime` to track `CurrentAP`, update UI to show AP bar. AbilityDefinition unchanged.
- **More targeting types**: Add enum values to `TargetingType` (Cone, Line, GroundTarget) + corresponding logic in `TargetingHelper.CollectTargets()`. AbilityDefinition/AbilityExecutor unchanged.
- **Status effects**: `EffectPayload.ApplyStatus` + `StatusId` field already exist. Implement `StatusDefinition` SO + `BuffManager`, wire into AbilityExecutor's ApplyStatus case.
- **Surface creation**: Add `AbilityEffectType.CreateSurface` + `SurfaceType` field to EffectPayload. Wire into AbilityExecutor + SurfaceSystem.
- **Cooldowns**: `AbilityDefinition._cooldown` field exists. Implement per-unit cooldown tracking in a `CooldownTracker` service, check in AbilityExecutor validation.

---

## 2026-03-03 (Session 6) — UI Click-Through Fix, Move Confirmation, Weapon Binding Research

### Completed
- **UI click-through fix** — Clicking OnGUI HUD buttons (End Turn, Move, etc.) no longer passes through to the game world. Dual-layer detection: `EventSystem.IsPointerOverGameObject()` for uGUI canvases + `CombatHudController.IsMouseOverHud` static property for OnGUI rects. Added `EnsureEventSystem()` in GameBootstrap creating EventSystem + InputSystemUIInputModule (scene had none).
- **Two-click move confirmation** — Combat move now requires clicking the same hex twice to confirm. First click shows green cylinder marker + hint text "Click again to confirm move to (Q, R)". Click different hex updates pending target. Cancel/EndTurn/turn change clears pending. Non-combat retains immediate movement.
- **Input.mousePosition fix** — `CombatHudController.Update()` migrated from legacy `Input.mousePosition` to `Mouse.current.position.ReadValue()` (New Input System API). Eliminates `InvalidOperationException` at runtime.
- **Weapon binding research** — Investigated Synty PropBone system. Confirmed it's designed for SwordCombat pack weapons only; PolygonDungeon/PolygonDungeonRealms weapons need manual offset tuning. Created `WeaponBindingGuide.md` reference document. Mage staff manually tuned: Pos(0.15, 0.05, 0) Rot(75, 22, 180).

### Files Modified This Session
- `Scripts/Units/TacticalInputHandler.cs` — Added `IsPointerOverUI()`, pending move state (`_pendingMoveTarget`, `_pendingMoveMarker`, `PendingActionHint`), `TryMoveWithConfirmation()`, `SetPendingMove()`, `ClearPendingMove()`
- `Scripts/UI/CombatHudController.cs` — Added `IsMouseOverHud` static property, `Update()` with `Mouse.current.position`, hint label shows `PendingActionHint`
- `Scripts/Core/GameBootstrap.cs` — Added `EnsureEventSystem()` creating EventSystem + InputSystemUIInputModule
- `Scripts/Combat/CombatSceneController.cs` — Minor adjustments for animation blocking
- `Data/Units/Mage_01.asset` — Weapon offset tuned: Pos(0.15, 0.05, 0) Rot(75, 22, 180)

### Files Created This Session
- `WeaponBindingGuide.md` — Synty Prop Bone system reference, weapon prefab paths, animation clip inventory

### Weapon Offset Status
| Unit | Weapon | Tuned? |
|------|--------|--------|
| Warrior | Straightsword | No — needs Play Mode tuning |
| Archer | Spear | No — needs Play Mode tuning |
| Mage | Staff | Yes — Pos(0.15, 0.05, 0) Rot(75, 22, 180) |
| Skeleton Knight | Large Sword | No — needs Play Mode tuning |
| Goblin Warrior | Small Axe | No — needs Play Mode tuning |

---

## 2026-03-03 (Session 5) — Enemy AI, HP Bars, Floating Damage Text, Turn Order UI

### Completed
- **Enemy AI system** — `AI/AIBrain.cs` (MonoBehaviour with coroutine-driven turn execution) + `AI/AIScorer.cs` (stateless scoring: target priority by HP/distance/kill potential, movement positioning). AI evaluates targets, moves toward best target, attacks if in melee range. Paced with configurable delays (think=0.5s, move=0.3s, attack=0.8s) for visual readability. Replaces `AutoEndEnemyTurn()` placeholder.
- **Floating damage text** — `UI/FloatingDamageText.cs`. WorldSpace Canvas text that rises, drifts randomly, and fades out. Red for damage, gold for crits (larger + bold), green for heals. Black outline for readability. Self-destructs after 1.2s lifetime.
- **Unit HP bars** — `UI/UnitWorldUI.cs`. WorldSpace Canvas HP bar attached to each unit's head. Billboard (always faces camera). Green bar for players (color shifts green→yellow→red as HP drops), red bar for enemies. Smooth fill lerp animation. Shows unit name above bar.
- **Combat UI Manager** — `UI/CombatUIManager.cs`. Subscribes to `UnitDamagedEvent`/`UnitHealedEvent`/`UnitDiedEvent`/`UnitSpawnedEvent` via EventBus. Creates HP bars on spawn, updates on damage/heal, removes on death. Spawns floating text at unit positions.
- **Turn order bar** — `UI/TurnOrderBar.cs`. uGUI ScreenSpace-Overlay Canvas. Horizontal bar anchored top-center. Shows all alive units in initiative order with name + HP. Active unit highlighted with gold border. Blue slots for player, red for enemy. Auto-refreshes on turn/round changes and unit death. Slot width auto-scales (80→52px min) when many units are present to prevent overflow (~60% max screen width).
- **Party portrait panel** — `UI/PartyPortraitPanel.cs`. Left-side vertical panel showing player team units. Each portrait (140×60px) has unit name, HP bar (color shifts green→red below 35%), and clickable button to select that unit. Gold border = active turn unit, blue = selected, dark = normal. Subscribes to TurnStartedEvent, UnitSelectedEvent, UnitDeselectedEvent, damage/heal/death events.
- **UI layout overhaul** — Round banner moved to top-left (12,12). Turn order bar → top-center. Party portraits → left side below Round banner. Right-top reserved for minimap (future).
- **CombatSceneController AI integration** — Replaced `AutoEndEnemyTurn()` with `AIBrain.ExecuteTurn()`. AI manages its own action spending (move+attack), `OnUnitMoveCompleted` now skips action spending for enemy units. Added `IsEnemyActing` property.
- **GameBootstrap wiring** — Added `InitializeCombatWorldUI()`, `InitializeTurnOrderBar()`, and `InitializePartyPortraits()` to initialization chain.

### Files Created This Session
- `Scripts/AI/AIBrain.cs` — AI turn execution (coroutine: pick target → move → attack → end turn)
- `Scripts/AI/AIScorer.cs` — Target scoring and movement positioning logic
- `Scripts/UI/FloatingDamageText.cs` — Animated floating combat text
- `Scripts/UI/UnitWorldUI.cs` — WorldSpace HP bar per unit
- `Scripts/UI/CombatUIManager.cs` — EventBus-driven manager for HP bars and floating text
- `Scripts/UI/TurnOrderBar.cs` — Initiative order display bar
- `Scripts/UI/PartyPortraitPanel.cs` — Left-side clickable party portraits with HP bars

### Files Modified This Session
- `Scripts/Combat/CombatSceneController.cs` — Added AIBrain integration, replaced AutoEndEnemyTurn, added IsEnemyActing, enemy move event filtering
- `Scripts/Core/GameBootstrap.cs` — Added InitializeCombatWorldUI(), InitializeTurnOrderBar(), InitializePartyPortraits()
- `Scripts/UI/CombatHudController.cs` — Moved Round banner to top-left (12,12)

### Key Technical Details
- **AI decision loop**: Pick best target (ScoreTarget: kill potential +80, low HP +0~50, distance -5/hex) → GetReachable for movement → FindBestMoveToward (minimize distance to target) → BasicAttackSystem.Execute if in range 1.
- **AI manages own actions**: AIBrain calls `_actionSystem.SpendMoveAction()` and `_actionSystem.SpendMainAction()` directly, then calls `_combatController.EndCurrentTurn()` when done. `OnUnitMoveCompleted` in CombatSceneController skips non-player units to avoid double-spending.
- **HP bar billboard**: `UnitWorldUI.Update()` sets `transform.rotation = Camera.main.transform.rotation` every frame. Position maintained via `LateUpdate()` at localPosition `(0, 2.0, 0)` above parent unit.
- **Turn order auto-scale**: `ComputeSlotWidth()` calculates `maxBarWidth = referenceResolution.x * 0.6 = 1152px`, divides by unit count with spacing. Clamps between 52~80px per slot. Handles 10+ units gracefully.

---

## 2026-03-03 (Session 4) — Movement Visualizer Fix & Combat Bootstrap Expansion

### Completed
- **Fix: MovementRangeVisualizer cells on buildings** — Root cause: ground plane at Y≈-3.25, buildings at Y≈0. Raycast from Y=+20 hit building rooftops instead of ground. Fixed by changing `_raycastHeight=20f` to `_localRayHeight=2f` — ray now starts from cellY+2m, so buildings above the ray origin are never hit.
- **Fix: Perimeter-only border** — Changed GL.Lines border from drawing on every reachable cell edge to only the outermost perimeter. Uses `EdgeToDirection = { 0, 5, 4, 3, 2, 1 }` mapping for flat-top hex edge-to-neighbor direction. Each edge checks if the neighbor across it is also reachable; only draws the edge if the neighbor is NOT reachable.
- **DOS2-style overlay** — Reachable cells show blue fill with vertex-color gradient (center bright, edges fade). Unreachable walkable cells within extended range show gray dimming overlay. Bright perimeter border highlights the movement boundary.
- **Fix: E key binding corruption** — `TacticalInputHandler` was dynamically creating InputActions which corrupted the InputActionAsset at runtime. Simplified to use `FindAction()` on existing actions. E key no longer triggers EndTurn.
- **Archer model swap** — Changed from SidekickCharacters model to PolygonDungeonRealms character to fix shader issues.
- **CombatHudController scaling** — Scaled up UI: TopScale=2f for round banner, BottomScale=3f for action panel. All font sizes and element dimensions multiplied accordingly.
- **GameBootstrap expansion** — Full combat initialization pipeline: Grid → Units → Combat → HUD. Auto-spawns test units from serialized `SpawnData[]`, focuses camera on first player unit, initializes `CombatSceneController` and starts combat.
- **Combat system wiring** — TurnManager, ActionSystem, CombatSceneController all initialized and functional. Turn order by initiative, player gets Move/Attack/Heal/Cancel/EndTurn buttons.

### Files Modified This Session
- `Scripts/Units/MovementRangeVisualizer.cs` — Complete rewrite: terrain projection via local raycast (cellY+2m), perimeter-only GL.Lines border, vertex-colored hex mesh with mid-ring gradient, `Sprites/Default` shader for Built-in RP
- `Scripts/Units/TacticalInputHandler.cs` — Simplified to `FindAction()` instead of dynamic `AddAction()`, fixed E key conflict
- `Scripts/UI/CombatHudController.cs` — Added TopScale/BottomScale constants, scaled all UI elements 2-3x
- `Scripts/Core/GameBootstrap.cs` — Added full InitializeUnits(), InitializeCombat(), InitializeCombatHud(), SpawnTestUnits() with camera focus
- `Data/Animation/ExplorerAnimator.controller` — Updated with combat animations
- `Scenes/Combat/Combat_RuinsPrototype_01.unity` — Updated scene with all system objects and spawn data
- `InputSystem_Actions.inputactions` — Updated action bindings
- `ProjectSettings/TagManager.asset` — Added Units layer

### Key Technical Details
- **Terrain raycast**: `_localRayHeight = 2f`, `_surfaceOffset = 0.05f`. Ray starts at worldPos.y + 2m, casts down 4m. Excludes Units layer via `_terrainLayerMask`.
- **Edge-to-Direction mapping**: For flat-top hex, Edge i (corner[i]→corner[i+1]) maps to HexDirection `{ E, SE, SW, W, NW, NE }` = `{ 0, 5, 4, 3, 2, 1 }`.
- **Shader choices**: `Sprites/Default` for vertex-colored fill mesh, `Hidden/Internal-Colored` for GL.Lines border. Both work in Built-in RP.
- **Colors**: Reachable `(0.3, 0.6, 1.0, 0.55)`, Unreachable `(0.4, 0.4, 0.4, 0.3)`, Border `(0.3, 0.75, 1.0, 0.85)`.

---

## 2026-03-02 (Session 3) — Combat Polish & Equipment

### Completed
- **Fix: Move action flow** — `QueueMoveAction()` now auto-selects the active unit and calls `RefreshSelection()` to immediately show movement range (blue reachable + gray unreachable hex overlay) without needing to re-click the character.
- **Fix: UI button highlight** — Active queued action button (Move/Attack/Heal) highlighted with blue tint via `GUI.backgroundColor`. All buttons grayed out and disabled during action animations with "Action in progress..." hint.
- **Fix: Animation timing** — Attack/heal now uses `WaitThenProcessPostAction()` coroutine (1s delay) before auto-ending turn or re-selecting unit. Added `_actionAnimating` flag that blocks all input and turn-end during animation.
- **Fix: E key conflict** — Changed EndTurn keybinding from `<Keyboard>/e` to `<Keyboard>/space`. E was conflicting with camera Rotate action (Q/E) in `TacticalCameraInputHandler`.
- **Weapon attachment system** — Added `_weaponPrefab` and `_weaponBoneName` fields to `UnitDefinition`. `UnitSpawner` now auto-attaches weapons to character hand bones using recursive bone search at spawn time.
- **Character model swap** — Mage (ID:2) model changed from SidekickCharacters `HumanSpecies_03` (had shader issues) to PolygonDungeonRealms `Chr_Nomad_Male_01` (proper materials).
- **Weapons assigned** — Warrior: Straightsword, Skeleton Knight: Large Sword, Goblin Warrior: Small Axe.

### Files Modified This Session
- `Scripts/Combat/CombatSceneController.cs` — Added `IEnumerator WaitThenProcessPostAction()`, `_actionAnimating` flag, `IsActionAnimating` property, modified `QueueMoveAction()` to auto-select+refresh, blocked `EndCurrentTurn()` and `TryExecuteQueuedActionOnTarget()` during animation
- `Scripts/UI/CombatHudController.cs` — Added button highlight colors, animation state rendering, `GUI.backgroundColor` per queued action
- `Scripts/Units/TacticalInputHandler.cs` — Changed EndTurn binding to Space, added `_combatController.IsActionAnimating` check to block input during animations
- `Scripts/Units/UnitDefinition.cs` — Added `_weaponPrefab`, `_weaponBoneName` fields and properties
- `Scripts/Units/UnitSpawner.cs` — Added `AttachWeapon()`, `FindBoneRecursive()` methods
- `Data/Units/*.asset` — All 5 unit assets updated with `_weaponPrefab` and `_weaponBoneName` fields

---

## 2026-03-02 (Session 2) — Combat UI And Action-Targeting

### Completed
- Added a basic combat HUD with an on-screen round banner and action buttons.
- Added a visible `Move` action in the HUD so movement remains discoverable in combat.
- Added player-triggered `Attack` and `Heal` action queueing instead of using right-click to attack.
- Added `[` and `]` keyboard shortcuts to cycle player-unit selection and camera focus.
- Added basic combat services for melee damage and ally healing.
- Added combat events for damage and healing.
- Added unit action animation triggering for attack and heal execution.
- Added unit despawn support for death cleanup.
- Rebound prototype unit visuals to one knight hero and two monster models.
- Rewired combat bootstrap and verified the updated combat startup path enters play mode successfully.
- Restored move-action spending after movement completion so turn economy stays consistent.

---

## 2026-03-01 (Session 1) — Grid + Units + Movement Foundation

### Completed
- Hex grid system (HexGridMap, HexCell, HexCoord, HexPathfinder, HexGridVisualizer, HexGridScanner)
- Unit system (UnitDefinition SO, UnitRuntime, UnitStats, UnitRegistry, UnitSpawner, UnitBrain, UnitVisual)
- Selection and movement (UnitSelectionManager, UnitMovementSystem, MovementRangeVisualizer)
- Input handling (TacticalInputHandler with New Input System)
- Camera system (TacticalCamera with WASD pan, QE rotate, scroll zoom)
- Combat foundation (TurnManager, ActionSystem, CombatSceneController)
- Event system (EventBus, CombatEvents, UnitEvents)
- Bootstrap and scene wiring (GameBootstrap, GameSession)
- Fixed HexGridVisualizer NullRef from Awake() ordering
- Fixed left-click-to-move on reachable cells

---

## Known Issues

- [x] ~~**Archer model** — Still uses SidekickCharacters model, may have pink shader issues.~~ Fixed Session 4: swapped to PolygonDungeonRealms character.
- [x] ~~**Enemy AI** — Enemies auto-skip turns (no AI).~~ Fixed Session 5: AIBrain + AIScorer implemented.
- [x] ~~**Damage feedback missing** — No floating damage numbers or HP bars above units.~~ Fixed Session 5: FloatingDamageText + UnitWorldUI.
- [x] ~~**No turn order display** — No initiative bar showing upcoming turn order.~~ Fixed Session 5: TurnOrderBar.
- [ ] **Unit death edge cases** — Verify death during active turn doesn't cause NullRef.
- [ ] **GoblinWarrior weapon position** — Small Axe may need bone position adjustment.
- [ ] **OnGUI HUD migration** — CombatHudController still uses OnGUI, should migrate to uGUI Canvas.

---

## Next Steps (Priority Order)

### Immediate (Phase 1 Core Combat Loop)
1. ~~**Ability System**~~ — Done (Session 7).
2. ~~**Death animation**~~ — Done (Session 8). Fade out + sink before despawn.
3. ~~**Status Effects**~~ — Done (Session 8). StatusDefinition SO + StatusManager + wired into AbilityExecutor.
4. ~~**Surface System**~~ — Done (Session 8). SurfaceDefinition SO + SurfaceSystem manager + reaction table + SurfaceVisualizer + 4 surface assets.
5. ~~**Cover System**~~ — Done (Session 8). CoverResolver + DamageResolver integration + CoverSetup + CoverVisualizer.
6. ~~**Line of Sight**~~ — Done (Session 9). CoverResolver.HasLineOfSight() + AbilityExecutor + AIBrain integration.
7. ~~**DOS2 AP system**~~ — Done (Session 9). UnitRuntime AP pool, ActionSystem rewrite, per-ability AP costs, AI budgeting, HUD display.

### Later (Phase 1 Polish)
- Better attack/heal visual feedback (VFX, camera shake)
- Victory/Defeat screens
- Sound effects (attack, move, select, turn switch)
- Multiple encounters / scene transitions

---

## Architecture Quick Reference

- **Three-layer rule**: Thin MonoBehaviour → Plain C# domain logic → Presentation
- **Events**: `EventBus.Publish<T>()` / `EventBus.Subscribe<T>()` (static generic struct)
- **Action economy**: DOS2-style AP pool (default 6 AP/turn). Movement = 1 AP/hex, abilities cost variable AP. Explicit end turn, order-free (move/attack/move if AP permits).
- **Abilities**: Data-driven via `AbilityDefinition` SO → `AbilityExecutor.Execute()`. Each unit has `AbilityDefinition[]` in its `UnitDefinition`. HUD buttons generated dynamically.
- **Stats**: All derived stats centralized in `UnitStats.cs`, equipment via `SetEquipmentBonuses()`
- **Grid**: `HexGridMap` is authority for cell data, pathfinding, occupancy
- **Input**: Unity New Input System 1.18.0, `TacticalInputHandler` uses `FindAction()` on existing actions
- **Keybindings**: Left-click=select/move, Right-click=move, [/]=cycle units, Space=end turn, WASD=camera pan, QE=camera rotate, Scroll=zoom

## File Map

```
Assets/_Project/
  Scripts/
    Core/       GameBootstrap.cs, EventBus.cs, GameSession.cs
    Grid/       HexGridMap, HexCell, HexCoord, HexPathfinder, HexGridVisualizer, HexGridScanner, HexGridConfig, MinHeap, GridEnums, SurfaceDefinition, SurfaceInstance, SurfaceSystem, SurfaceVisualizer, CoverResolver, CoverSetup, CoverVisualizer
    Combat/     CombatSceneController, TurnManager, ActionSystem, DamageResolver, CombatEvents
    Units/      UnitDefinition, UnitRuntime, UnitStats, UnitRegistry, UnitSpawner, UnitBrain, UnitVisual, UnitSelectionManager, UnitMovementSystem, MovementRangeVisualizer, TacticalInputHandler, UnitEvents
    Camera/     TacticalCamera, TacticalCameraInputHandler, TacticalCameraConfig
    UI/         CombatHudController, CombatUIManager, FloatingDamageText, UnitWorldUI, TurnOrderBar, PartyPortraitPanel
    AI/         AIBrain, AIScorer
    Abilities/  AbilityEnums, EffectPayload, AbilityDefinition, AbilityResult, AbilityExecutor, TargetingHelper, StatusDefinition, StatusInstance, StatusManager
  Data/
    Units/      Warrior_01, Archer_01, Mage_01, SkeletonKnight_01, GoblinWarrior_01
    Abilities/  BasicAttack, BasicHeal, FireBolt
    Statuses/   Burning, Frozen
    Surfaces/   FireSurface, OilSurface, PoisonSurface, IceSurface
    Grid/       ForgeGridConfig.asset
    Animation/  CombatUnitAnimator.controller, ExplorerAnimator.controller
  Scenes/Combat/ Combat_RuinsPrototype_01.unity
```

## Unit Configuration

| Unit | ID | Team | Model Pack | Weapon | MP | Stats Focus |
|------|----|------|-----------|--------|-----|-------------|
| Warrior | 0 | Player | PolygonDungeon | Straightsword | 4 | STR 14, CON 14 |
| Archer | 1 | Player | SidekickCharacters | None | 5 | FIN 14, WIT 12 |
| Mage | 2 | Player | PolygonDungeonRealms | None | 3 | INT 14, WIT 14 |
| Skeleton Knight | 3 | Enemy | PolygonDungeonRealms | Large Sword | 3 | STR 14, CON 12 |
| Goblin Warrior | 4 | Enemy | PolygonDungeon | Small Axe | 5 | FIN 12, WIT 10 |
