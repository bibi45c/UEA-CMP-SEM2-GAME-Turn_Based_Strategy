# Progress Report

This file tracks implementation progress in small, append-only milestones.
Update it after each completed feature slice so the current state is visible
without diffing code.

> **New session? Read these files first:**
> 1. `CLAUDE.md` — Project rules, architecture, naming conventions
> 2. `GameOutline.md` — Full game design outline & system architecture
> 3. This file (`progress_report.md`) — Current state & next steps

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
- [ ] **Enemy AI** — Enemies auto-skip turns (no AI). `CombatSceneController.AutoEndEnemyTurn()` just calls `EndCurrentTurn()` after 0.1s.
- [ ] **Unit death edge cases** — Verify death during active turn doesn't cause NullRef.
- [ ] **GoblinWarrior weapon position** — Small Axe may need bone position adjustment.
- [ ] **Damage feedback missing** — No floating damage numbers or HP bars above units.
- [ ] **No turn order display** — No initiative bar showing upcoming turn order.

---

## Next Steps (Priority Order)

### Immediate (Phase 1 Core Combat Loop)
1. **Enemy AI** — `AI/AIBrain.cs`, `AIScorer.cs`. Simple: evaluate targets by distance+HP, move toward nearest, attack if in range. Wire into `CombatSceneController.OnTurnStarted` for enemy turns.
2. **Ability System** — `Abilities/AbilityDefinition.cs` (SO: name, range, damage, element, cost, targetType), `AbilityExecutor.cs`. Replace hardcoded `BasicAttackSystem`/`HealSkillSystem` with data-driven abilities.
3. **Better UI (uGUI)** — Replace OnGUI with proper Canvas-based UI. Unit HP bars, ability bar, turn order display, floating damage numbers.
4. **Status Effects** — Burning, Poisoned, Frozen, Blessed. Tied to surface system.
5. **Surface System** — Cells have optional surface (Fire, Ice, Poison, Oil). Abilities create/interact with surfaces.
6. **Cover System** — HalfCover -25% dmg, FullCover blocks ranged. Directional check.
7. **Line of Sight** — Ranged attack LoS validation using hex raycasting.

### Later (Phase 1 Polish)
- Damage numbers / floating combat text
- Turn order UI (initiative bar)
- Unit HP bars above character models
- Better attack/heal visual feedback (VFX, camera shake)
- Victory/Defeat screens
- Multiple encounters / scene transitions

---

## Architecture Quick Reference

- **Three-layer rule**: Thin MonoBehaviour → Plain C# domain logic → Presentation
- **Events**: `EventBus.Publish<T>()` / `EventBus.Subscribe<T>()` (static generic struct)
- **Action economy**: 1 move + 1 main + 0~1 bonus, explicit end turn, order-free
- **Stats**: All derived stats centralized in `UnitStats.cs`, equipment via `SetEquipmentBonuses()`
- **Grid**: `HexGridMap` is authority for cell data, pathfinding, occupancy
- **Input**: Unity New Input System 1.18.0, `TacticalInputHandler` uses `FindAction()` on existing actions
- **Keybindings**: Left-click=select/move, Right-click=move, [/]=cycle units, Space=end turn, WASD=camera pan, QE=camera rotate, Scroll=zoom

## File Map

```
Assets/_Project/
  Scripts/
    Core/       GameBootstrap.cs, EventBus.cs, GameSession.cs
    Grid/       HexGridMap, HexCell, HexCoord, HexPathfinder, HexGridVisualizer, HexGridScanner, HexGridConfig, MinHeap, GridEnums
    Combat/     CombatSceneController, TurnManager, ActionSystem, BasicAttackSystem, HealSkillSystem, DamageResolver, CombatEvents
    Units/      UnitDefinition, UnitRuntime, UnitStats, UnitRegistry, UnitSpawner, UnitBrain, UnitVisual, UnitSelectionManager, UnitMovementSystem, MovementRangeVisualizer, TacticalInputHandler, UnitEvents
    Camera/     TacticalCamera, TacticalCameraInputHandler, TacticalCameraConfig
    UI/         CombatHudController
    AI/         (empty — next to build)
    Abilities/  (empty — next to build)
  Data/
    Units/      Warrior_01, Archer_01, Mage_01, SkeletonKnight_01, GoblinWarrior_01
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
