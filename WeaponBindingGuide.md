# Weapon Binding Guide (Sword Combat Pack)

Quick reference for binding weapons (swords) to character models.
Read this document + check the reference prefabs listed below.

---

## Our Current System (Simple Parenting)

**How it works**: `UnitSpawner.AttachWeapon()` finds the bone by name, parents weapon prefab directly.

**Flow**:
1. `UnitDefinition` SO defines: `WeaponPrefab`, `WeaponBoneName` (default `"Hand_R"`), `WeaponPositionOffset`, `WeaponRotationOffset`
2. `UnitSpawner.SpawnUnit()` calls `AttachWeapon(model, definition)`
3. `FindBoneRecursive()` searches skeleton for bone containing `"Hand_R"`
4. `Instantiate(weaponPrefab, bone)` → set localPosition/localRotation from definition offsets

**Key files**:
- `Assets/_Project/Scripts/Units/UnitSpawner.cs` — `AttachWeapon()` method (line ~146)
- `Assets/_Project/Scripts/Units/UnitDefinition.cs` — weapon fields

**Tuning offsets**: Adjust `WeaponPositionOffset` and `WeaponRotationOffset` in the UnitDefinition SO inspector.

---

## Synty Prop Bone System (Advanced)

Use this when animations require weapon transforms (e.g., sword rotates in-hand during attack animations).

**Architecture**:
- `PropBoneConfig` (ScriptableObject) — stores bone definitions, rotation/scale offsets between rigs
- `PropBoneBinder` (MonoBehaviour) — runtime component, creates prop bones, updates transforms in LateUpdate

**Bone hierarchy created**:
```
Hand_R
  └── Prop_R           ← created by tool
        └── Prop_R_Socket  ← weapon attaches here
```

**Setup steps**:
1. Open Unity menu: `Synty > Tools > Animation > Setup Prop Bones`
2. Select a `PropBoneConfig` asset (or create one)
3. The tool creates `Prop_R` / `Prop_R_Socket` bones under `Hand_R`
4. Add `PropBoneBinder` component to the character root
5. Assign the `PropBoneConfig` to the binder
6. Parent weapon prefab under `Prop_R_Socket`

**How offsets work**:
- `PropBoneConfig.CalculateOffsetValues()` computes rotation offset between source/target rigs
- Rotation: `Quaternion.Inverse(targetParent.rotation) * sourceParent.rotation`
- Scale: `targetBoneDistance / sourceBoneDistance`
- `PropBoneBinder.UpdateBone()` applies offsets every LateUpdate via Matrix4x4

**Key files**:
- `Assets/ThirdParty/Synty/Tools/SyntyPropBoneTool/Runtime/PropBoneBinder.cs`
- `Assets/ThirdParty/Synty/Tools/SyntyPropBoneTool/Runtime/PropBoneConfig.cs`

---

## Reference Prefabs & Assets

| Asset | Path |
|-------|------|
| Sword prefab | `Assets/ThirdParty/Synty/AnimationSwordCombat/Samples/Prefabs/Wep_Sword_01.prefab` |
| Sword mesh | `Assets/ThirdParty/Synty/AnimationSwordCombat/Samples/Meshes/SM_Wep_Sword_01.fbx` |
| Demo scene | `Assets/ThirdParty/Synty/AnimationSwordCombat/Samples/Scenes/SwordCombat_Gallery_01.unity` |
| Default PropBoneConfig | `Assets/ThirdParty/Synty/Tools/SyntyPropBoneTool/Configs/AnimationSwordCombat_PropBoneBindingConfig_Default.asset` |
| Polygon rig config | `Assets/ThirdParty/Synty/Tools/SyntyPropBoneTool/Configs/POLYGONRig_01_PropBoneBindingConfig.asset` |
| Kid rig config | `Assets/ThirdParty/Synty/Tools/SyntyPropBoneTool/Configs/KidRig_01_PropBoneBindingConfig.asset` |
| Big rig config | `Assets/ThirdParty/Synty/Tools/SyntyPropBoneTool/Configs/BigRig_01_PropBoneBindingConfig.asset` |

---

## Animation Clips (Sword Combat Pack)

Location: `Assets/ThirdParty/Synty/AnimationSwordCombat/Animations/Polygon/`

| Category | Examples |
|----------|----------|
| Attack | `A_Attack_HeavyCombo01A_Sword`, `A_Attack_LightCombo01A_Sword`, etc. |
| Block | `A_Block_Idle_Sword`, `A_Block_React_Sword` |
| Death | `A_Death_A_Sword`, `A_Death_B_Sword` |
| Dodge | `A_Dodge_B_Sword`, `A_Dodge_F_Sword`, `A_Dodge_L_Sword`, `A_Dodge_R_Sword` |
| Hit | `A_Hit_A_Sword`, `A_Hit_B_Sword` |
| Idle | `A_Idle_Sword` |

All clips are FBX files. Import settings may need "Bake Into Pose" adjustments for root motion.

---

## Current Unit Weapon Configs

All 5 units use `WeaponBoneName = "Hand_R"`. Check individual `.asset` files in `Assets/_Project/Data/Units/` for per-unit offsets.

---

## Quick Checklist: Adding a New Sword Unit

1. Create `UnitDefinition` SO in `Assets/_Project/Data/Units/`
2. Set `ModelPrefab` to character model
3. Set `WeaponPrefab` to sword prefab (e.g., `Wep_Sword_01`)
4. Set `WeaponBoneName` to `"Hand_R"`
5. Adjust `WeaponPositionOffset` and `WeaponRotationOffset` in inspector until sword looks correct
6. If using Sword Combat animations with prop bone offsets, add `PropBoneBinder` + config instead
7. Test in demo scene or combat scene
