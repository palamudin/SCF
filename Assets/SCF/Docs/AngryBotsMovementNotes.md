# Angry Bots Movement Notes

Imported as a reference only. Do not wire the Unity 4 scripts into SCF.

Useful movement ideas found:

- `Assets/Scripts/Movement/FreeMovementMotor.js` drives ground motion by pushing current velocity toward a target velocity with high snappiness instead of teleporting or root-motion pulling.
- `FreeMovementMotor.js` rotates from a signed angle around world up, using either facing direction or movement direction. This keeps the marine responsive without needing parkour-style traversal state.
- `Assets/Scripts/Movement/PlayerMoveController.js` maps input into camera-relative world vectors and aims from cursor-to-player world direction. This matches the isometric shooter control shape we want.
- `Assets/Scripts/Animation/PlayerAnimation.js` picks the best locomotion clip by measured local velocity angle and speed, then keeps lower body/upper body alignment separate. This is the part worth recreating in our MXM-ish layer.

SCF application:

- Keep SCF's wall-run code. Angry Bots has no useful wall movement.
- Frank remains `Parkour` profile.
- Non-Frank characters use `Standard` traversal: short wall run, lower wall latch, slower vault/climb, and load-sensitive climb limits.
- Weapon load should eventually drive `IsometricCharacterMotor.SetCarriedLoad(float)` so heavy weapons reduce climb height and slow traversal.
- `IsometricCharacterMotor` now exposes lower-body locomotion facing knobs, while `SCFAimBodyDifferentiator` handles the Angry Bots-style torso/legs split for humanoid rigs.
- Frank/Parkour characters can now branch from wallrun into a flat-approach wall climb-up, then hand off to the existing climb top-out action when the ledge is reached.
- Frank/Parkour sprint traversal now auto-vaults low obstacles and switches taller head-height obstacles to a slide-vault animation path; non-sprint traversal remains manual and slower.
- Frank/Parkour now uses generic rig bone-name fallback for cursor aim layering and turns the whole body toward aim once the head/torso yaw threshold is exceeded. Slide-vaults roll out into a combat roll instead of ending straight to feet.
