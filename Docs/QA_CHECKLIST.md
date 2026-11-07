# Manual QA Checklist

Use this checklist before merging gameplay changes to `main`.

## Setup

- [ ] Open `Assets/Scenes/SampleScene` in Unity `6000.6.4f1`.
- [ ] Clear the Console, enter Play mode, and confirm there are no red errors.
- [ ] Confirm the player starts with a weapon equipped.
- [ ] Record the build/commit under test: `________________`.

## Block state on F

- [ ] Hold **F** while standing on the ground. The player enters the block state and the `Block` animation plays.
- [ ] A block wall starts forming on the ground in front of the player.
- [ ] Keep **F** held. The wall continues building and the player stays in the block state.
- [ ] Release **F**. The block state ends, the `Intrerupt` animation triggers, and the wall stops building and falls/settles.
- [ ] Hold **F** briefly and release before the wall finishes. The wall does not remain floating or duplicate.
- [ ] Press **F** again. A new wall is created; no wall from the previous attempt remains active.

## Golem movement

- [ ] Build a wall past the golem threshold (more than 31 stones), release **F**, and wait for the wall to compose into a golem.
- [ ] The golem emerges as an ally and is active after the awakening delay.
- [ ] The golem moves over the NavMesh toward the nearest enemy without sliding, sinking, or standing still.
- [ ] The golem turns toward its target and its movement animation matches its speed.
- [ ] When no enemy is reachable, the golem does not jitter or run in place.
- [ ] Damage the golem. It reacts, recovers, and resumes moving/attacking.

## Wall-to-ally-golem transition

- [ ] While blocking, verify stones appear from the ground upward and the wall becomes solid.
- [ ] Release **F** after the golem threshold is reached. The wall settles and the golem composition starts.
- [ ] Stones leave the wall and attach to the golem slots in the expected order.
- [ ] The golem awakens only after composition finishes; it is not active while still being assembled.
- [ ] The original wall object is destroyed after the golem is complete.
- [ ] The golem is treated as an ally: enemies target it, and player attacks do not damage it.

## Golem death after another wall is instantiated

- [ ] Build and awaken golem A.
- [ ] Build a second wall and bring it past the golem threshold to create golem B.
- [ ] When golem B is registered, golem A plays its death reaction and is removed after the death delay.
- [ ] Only golem B remains active and usable.
- [ ] Golem A's death does not leave a missing-reference exception in the Console.
- [ ] Repeat once more: the newest golem replaces the previous one consistently.

## Player right-click special move with weapon

- [ ] With a weapon equipped, right-click while grounded. The `Heavy` attack starts and the heavy ground impact is performed.
- [ ] Right-click while airborne. The `HeavyFall` attack starts and the airborne heavy impact is performed.
- [ ] Right-click with no weapon equipped. No heavy attack starts and no error is logged.
- [ ] During the heavy attack, check that the weapon hitbox/impact detection is active only during the intended window.
- [ ] Right-click repeatedly. The attack cannot be restarted while the current action is still busy.
- [ ] Confirm the weapon remains attached/usable after the attack completes.

## Enemies and animations

- [ ] Test each enemy type present in the scene: Stumphorn, Shacklewraith, and StoneSentinel.
- [ ] Each enemy plays idle/chase movement animations with no T-pose or frozen animator.
- [ ] Each enemy attacks when the target is in range and plays its attack animation.
- [ ] Stumphorn and StoneSentinel play their stomp/lateral reaction when the target is beside them.
- [ ] Shacklewraith uses its expected attack and does not reuse another enemy's animation.
- [ ] Enemy hit and death animations play instead of snapping back to idle.
- [ ] Enemies target allies (player/golem) and allies target enemies; no faction targets itself.
- [ ] After an enemy dies, it stops moving/attacking and is removed or disabled as intended.

## Health shown via enemy/ally glow color

- [ ] At full health, each enemy's and ally's health light has its normal full-health color.
- [ ] Hit an enemy once. The health light briefly pulses brighter.
- [ ] Repeatedly damage the enemy. The glow shifts from the full-health color toward red as health drops.
- [ ] Verify the glow change happens for allies too, including the golem.
- [ ] Kill an enemy. The death smoke effect appears after the configured delay.
- [ ] Confirm health glow does not stay red after a fresh scene load.

## Hit detection and special hit reactions

- [ ] Light attack an enemy at close range. Damage is applied, the health glow changes, and the `GetHit` reaction animation plays.
- [ ] Hit an enemy from outside its hurtbox range. No damage or reaction animation occurs.
- [ ] Confirm knockback happens on an accepted hit, then the enemy recovers and can act again.
- [ ] Hit the same enemy repeatedly during the knockback cooldown. Damage can still apply, but knockback does not stack uncontrollably.
- [ ] Use the heavy/special impact. The special impact VFX/hit reaction plays at the impact point, and the target receives the special reaction.
- [ ] Kill an enemy with a normal hit and with a special hit. Both reach the death animation/state correctly.
- [ ] Let an enemy hit the player. The player takes damage, plays the player hit reaction, and the block state still ends cleanly afterward.
- [ ] Watch the Console for missing-script, missing-animator-parameter, or missing-reference errors during all hit tests.
