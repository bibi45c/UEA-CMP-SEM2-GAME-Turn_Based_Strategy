# Asset Inventory

Last updated: 2026-03-02

This document catalogs all third-party and project assets available for development.

---

## Render Pipeline

- **Pipeline**: Built-in Render Pipeline (NOT URP)
- **ShaderGraph**: 17.3.0 (installed via package manager)
- **Environment shaders**: Standard shader (fileID: 46) - works natively
- **Character shaders**: Synty custom ShaderGraph shader (GUID: `db628544640279b41a4a7aa5d75c0322`) - **MISSING**, characters fallback to PolygonDungeonRealms materials

---

## ThirdParty Assets Summary

| Pack | Location | Models | Prefabs | Materials | Animations | Purpose |
|------|----------|--------|---------|-----------|------------|---------|
| AnimationBaseLocomotion | `Synty/AnimationBaseLocomotion/` | 696 FBX | - | - | 2 controllers | Idle, Walk, Run, Sprint, Crouch, Jump, Transitions |
| AnimationIdles | `Synty/AnimationIdles/` | - | - | - | 660 FBX | Extended idle variation animations |
| AnimationSwordCombat | `Synty/AnimationSwordCombat/` | 113 FBX | - | - | - | Sword attacks, blocks, dodges, deaths |
| SidekickCharacters | `Synty/SidekickCharacters/` | 158 FBX | 8 | 8 | - | Modular humanoid characters (4 Starter + 4 HumanSpecies) |
| SyntyPackageHelper | `Synty/SyntyPackageHelper/` | - | - | - | - | Editor utility for package configuration |
| SyntyPropBoneTool | `Synty/Tools/SyntyPropBoneTool/` | - | - | - | - | Weapon/prop bone attachment tool |
| PolygonDungeonRealms | `PolygonDungeonRealms/` | 1,163 | 1,112 | 30+ | 9 env FX | Dwarven forge environment (current combat arena) |
| PolygonDungeon | `PolygonDungeon/` | 782 | 802 | 42 | - | Classic dungeon environment pieces |
| PolygonDungeonMap | `PolygonDungeonMap/` | 62 | 57 | 1 | - | Minimap/overworld tile elements |
| PolygonFantasyRivals | `PolygonFantasyRivals/` | 45 | 76 | 27 | - | Fantasy rival characters + equipment |
| PolygonKnights | `PolygonKnights/` | 327 | 344 | 15 | - | Knight armor, weapons, variants |
| PolygonParticleFX | `PolygonParticleFX/` | 58 | 180 | 57 | - | Combat/environment particle effects |

**Totals**: ~3,400 models, ~2,600 prefabs, ~180 materials, ~1,400 animation files

All ThirdParty assets located under `Assets/ThirdParty/`.

---

## Animation Packs Detail

### AnimationBaseLocomotion
**Rig support**: Polygon (Feminine/Masculine) + Sidekick (Feminine/Masculine)

| Category | Subcategory | Key Files (Sidekick Masculine) |
|----------|-------------|-------------------------------|
| Idles | Standing, Crouching | `A_MOD_BL_Idle_Standing_Masc.fbx`, `A_MOD_BL_Idle_Crouching_Masc.fbx` |
| Locomotion/Walk | Forward, Strafe, Back | `A_MOD_BL_Walk_FwdStrafeF_Masc.fbx`, `A_MOD_BL_Walk_BckStrafeB_Masc.fbx` |
| Locomotion/Run | Forward, Strafe, Back | Similar naming with `Run` prefix |
| Locomotion/Sprint | Forward variants | Similar naming with `Sprint` prefix |
| InAir | Jump, Land (Soft/Medium/Hard) | `A_MOD_BL_Jump_Idle_Masc.fbx`, `A_MOD_BL_Land_IdleSoft_Masc.fbx` |
| Transitions | Idle-to-Walk, Walk-to-Idle, etc. | `A_MOD_BL_Idle_ToWalk_*.fbx`, `A_MOD_BL_Walk_ToIdleF_*.fbx` |

**Root Motion variants**: Files with `_RM_` suffix include root motion data.

**Pre-built Controllers**:
- `AC_Polygon_Feminine.controller`
- `AC_Polygon_Masculine.controller`

### AnimationIdles (Extended)
660 FBX files of idle variation animations for both Polygon and Sidekick rigs.
Useful for giving characters personality during idle states.

### AnimationSwordCombat
**Rig support**: Polygon (Masculine)

| Category | Variants |
|----------|----------|
| Light Attacks | ComboA/B/C, Flourish, Stab |
| Heavy Attacks | ComboA/B/C, Flourish, Stab |
| Block | Defensive stance |
| Dodge | Evasion |
| Hit | Damage reactions |
| Death | Death animations |

Each attack has: Standard + RootMotion + ReturnToIdle variants.

---

## Character Assets Detail

### SidekickCharacters
8 pre-configured character prefabs:

| Character | Prefab Path |
|-----------|-------------|
| HumanSpecies_01 | `Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_01/HumanSpecies_01.prefab` |
| HumanSpecies_02 | `Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_02/HumanSpecies_02.prefab` |
| HumanSpecies_03 | `Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_03/HumanSpecies_03.prefab` |
| HumanSpecies_04 | `Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_04/HumanSpecies_04.prefab` |
| Starter_01-04 | Similar paths under `Characters/Starter/` |

**Modular system**: 158 FBX outfit parts in `Resources/Meshes/Outfits/` for customizing characters.

**Material issue**: Custom Synty ShaderGraph shader is missing. Current workaround: assign `PolygonDungeonRealms_Mat_01_A.mat` to character renderers.

---

## Environment Assets Detail

### PolygonDungeonRealms (Primary - Combat Arena)
- 1,163 models: walls, floors, pillars, stairs, doors, props, crystals, lava
- 1,112 prefabs: ready-to-place dungeon pieces
- Materials: 4 main color sets (A variants) + B/C alternates + specialty FX materials
- Current scene: **Forge** demo environment (1,356 child objects)
- Textures: Albedo, Normal, Metallic, Emission maps

### PolygonDungeon
- 782 models + 802 prefabs: classic dungeon tileset
- 42 materials with color variants

### PolygonDungeonMap
- 62 models + 57 prefabs: overworld/minimap tiles
- Minimal material set (1 material)

### PolygonKnights
- 327 models + 344 prefabs: knight armor/weapons
- Useful for equippable items and NPC variants

### PolygonFantasyRivals
- 45 models + 76 prefabs: rival/enemy character assets
- Includes demo scene

---

## VFX Assets Detail

### PolygonParticleFX
180 particle effect prefabs organized by category:

| Category | Examples |
|----------|----------|
| Combat | `FX_ArtilleryShell_01`, `FX_ArtilleryStrike_01`, `FX_BloodSplat_01` |
| Elemental | `FX_Blizzard_01`, `FX_Bubbles_Float_01` |
| Environment | `FX_Leaves_01`, `FX_Smoke_*`, `FX_Dust_*` |
| Cartoony | `FX_Cartoony_Footstep_01`, `FX_Cartoony_Jump_01`, `FX_Cartoony_Sprint_01` |
| Magic/UI | `FX_Direction_Arrows_01-03`, `FX_Confusion_01` |
| Weather | `FX_Rain_01`, `FX_Snow_01` |

All at `Assets/ThirdParty/PolygonParticleFX/Prefabs/`.

---

## Tools

### SyntyPropBoneTool
Runtime weapon/prop attachment system:
- **PropBone.cs** / **PropBoneBinder.cs**: Attach props to character bones at runtime
- Pre-configured binding configs for AnimationBaseLocomotion and AnimationSwordCombat
- Supports dynamic weapon swapping

---

## Project-Created Assets

| Asset | Path | Purpose |
|-------|------|---------|
| ExplorerAnimator.controller | `_Project/Data/Animation/` | Idle/Walk states with IsMoving bool (Synty Sidekick clips) |
| ExplorerIdle.anim | `_Project/Data/Animation/` | (DEPRECATED - placeholder, replaced by Synty clips) |
| ExplorerWalk.anim | `_Project/Data/Animation/` | (DEPRECATED - placeholder, replaced by Synty clips) |
| ExplorerTemp.mat | `_Project/Data/Materials/` | (DEPRECATED - used URP/Lit shader, project is Built-in RP) |

---

## Known Issues

1. **Missing Synty Character Shader**: GUID `db628544640279b41a4a7aa5d75c0322` not found in project. All SidekickCharacters materials show pink. Workaround: assign Forge environment material.
2. **BoxCollider warnings**: 11 "negative scale or size" warnings from Forge environment on play. Pre-existing, harmless.
3. **No URP**: CLAUDE.md previously stated URP, but project uses Built-in RP. Corrected.
