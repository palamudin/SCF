# Weapon Pose Reference

Reference source: `Assets/SCF/MovementAni/NVoperatorsoldier.glb`

This pack is reference-only. Do not wire these imported clips directly into runtime locomotion or combat graphs.

## Useful Findings

- It contains 11 animation entries.
- Shotgun set: `SHOTGUNpose`, `SHOTGUNpose2`, `SHOTGUNpose3`, `SHOTGUNpose3-ArmatureSG`, `SHOTGUNwalk`.
- SMG set: `SMGpose`, `SMGpose2`, `SMGpose3`, `SMGpose3-ArmatureSG`, `SMGwalk`.
- `SHOTGUNpose3` is a one-frame pose at `0.041667s`; use it as calibration data, not as a runtime cycle.
- The railgun should follow the shotgun family: stock/chest-forward support, right hand on pistol grip, left hand underbarrel.
- The GLB skeleton exposes clear upper-body/hand naming: `Spineupper`, `Spinelower`, `ArmUpper.L/R`, `ArmLower.L/R`, `HandWrist.L/R`, `Hand.L.001`, `Hand.R.001`.
- `SHOTGUNpose3` useful root-frame relationships:
  - Right wrist from `SHOTGUNbone`: `(-0.0332, -0.2147, -0.8692)`
  - Left wrist from `SHOTGUNbone`: `(0.1325, -0.2297, 0.3901)`
  - Runtime mapping flips the reference X axis into Unity weapon local X and scales the relationship down through `shotgunPose3Scale`.

## SCF Replication Direction

- Runtime weapon holding should be SCF-authored through chest sockets, grip targets, procedural arm solving, and later SCF-authored additive/override clips.
- Imported reference clips can be inspected for proportions, timing, and pose family names, but should not drive player animation directly.
- Current implementation uses `SHOTGUNpose3` only to reproduce the pistol-grip/underbarrel spacing in `SCFWeaponVisualSlot`; the player hands chase SCF-created socket targets on top of the MXM locomotion layer, while the railgun mesh is the default visual.
- Live railgun tuning is enabled on `SCFWeaponVisualSlot`:
  - Move/rotate/scale `SCF_Selected_Railgun` to tune weapon placement.
  - Move/rotate `SCF_Pose3RightGripTarget` and `SCF_Pose3LeftGripTarget` to tune hand contact and wrist twist.
  - Current baked weapon transform:
    - `SCF_Selected_Railgun`: final local position `(-0.06, -0.02, -0.04)`, rotation `(-4.95, -90.25, 25.36)`, scale `(0.8, 0.8, 0.8)`.
  - Current baked grip targets:
    - Right: position `(0, -0.08, -0.14)`, rotation `(-106.2, -23.89999, -75.70001)`.
    - Left: position `(-0.15, 0.07, 0.2)`, rotation `(-134.6, 146.4, -11.4)`.
  - Use `SCF/Capture Current Railgun Tuning` on the player `SCFWeaponVisualSlot` context menu, or press `F8` when the legacy input backend is enabled, to copy the current profile values to the clipboard/log.
- For railgun, author toward shotgun-style pose values first, then tune in play mode:
  - Right hand: pistol grip.
  - Left hand: underbarrel grip.
  - Weapon parent: chest/upper-chest socket.
  - Aim raise: stock moves into frontal chest line.
