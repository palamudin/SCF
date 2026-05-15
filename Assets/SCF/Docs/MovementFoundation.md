# SCF Movement Foundation

This project uses Frost Blade Studios' MxM community fork as the motion matching package:

`https://github.com/Frost-Blade-Studios/Motion-Matching-for-Unity.git`

The package is registered in `Packages/manifest.json` as:

`com.frost-blade-studios.motion-matching`

The local gameplay layer is intentionally separate from the animation package:

- `IsometricPlayerInput` reads the Unity Input System, with keyboard/mouse and gamepad fallback bindings.
- `IsometricCharacterMotor` converts movement into camera-relative planar velocity and aim-relative facing.
- `MovementAnimatorBridge` sends safe optional parameters to a traditional Animator.
- `SCFMxMInputDriver` feeds SCF movement and aim intent into MxM's `MxMTrajectoryGenerator` when an MxM animator database is configured.
- `MotionMatchingSignalHub` exposes desired velocity, planar velocity, facing, and sprint intent, and also implements `IMxMRootMotion` with root motion ignored by default.
- `IsometricCameraFollow` provides a fixed isometric shooter camera.

## Starter Assets Humanoid

The desired free Unity humanoid source is:

`Starter Assets - ThirdPerson | URP`

Asset Store URL:

`https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-urp-196526`

Version checked on 2026-05-13: `1.1.7`, released 2026-03-16.

The package is URP-oriented, while this project is HDRP. Use it for the humanoid rig, prefab, and animations; expect materials/rendering to need HDRP cleanup.

After importing it from Package Manager / My Assets, run:

`SCF > Setup > Create Isometric Prototype Player`

The setup tool looks for `PlayerArmature` from Starter Assets. If it is unavailable, it creates a playable capsule fallback with the same movement scripts.

## Traversal Animation Harvest

The old parkour package has been reduced to harvested clips under SCF-owned assets.

SCF owns input, camera, motor physics, and animation routing. The harvested traversal clips are event clips only; base locomotion comes from the clean Starter Assets run/walk/idle set until a proper MxM animation database is authored.

Useful clips live in:

`Assets/SCF/Animation`

Live controller candidates:

- Locomotion: Starter Assets `Stand--Idle`, `Locomotion--Walk_N`, `Locomotion--Run_N`
- Mobility events: SCF `Jump`, `Roll`
- Wall run: movement state plus visual lean while continuing to feed normal run animation

The SCF movement motor currently emits jump, land, combat-roll, and wall-run signals through `SCFMxMCombatDriver`; once an MxM database is authored, name the live MxM tags/events to match `Jumping`, `CombatRoll`, `WallRun`, `Jump`, `Land`, and `CombatRoll`.

## Traversal Prototype Controls

- `WASD` / left stick: camera-relative run, including forward and left/right strafing.
- `Left Shift` / left-stick press: sprint.
- `Space` / gamepad south: traversal button.
- Tap space to combat roll.
- Hold space past the tap window to jump.
- Hold space while running into wall contact to jump into the wall and enter/stay in wall run. Wall run keeps using the normal run animation, leaned against the wall, while space is held and a wall remains under the side probe.
- Release space during wall run to jump away with retained wall-run momentum. Tap space during the short wall-jump window to convert into a faster dodge roll.

The motor does not scale movement metrics from the player transform by default. Keep the player container at scale 1 where possible; Unity's scaled `CharacterController` bounds still define the physical capsule.

For quick collision testing, run `SCF > Setup > Add Mobility Test Buildings`. This creates `SCF_MobilityTestBuildings` around `SCF_Player` with wall-run faces and larger blockouts sampled onto the active terrain.
