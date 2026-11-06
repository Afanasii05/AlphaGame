# Unchosen

> **A third-person action game focused on melee combat, movement, and enemy encounters, built with Unity.**

---

## Current Features

- Third-person player movement and camera
- Basic player input system 
- Melee combat with hitboxes and hit detection
- Player and enemy health/damage systems
- Light and heavy attacks
- Dodge and block mechanics
- Jumping, sprinting, and crouching
- Weapon pickup, equip, throw, and recall interactions
- Combat and movement animations
- Enemy combat foundations and basic enemy AI
- Some enemy types
- Heavy attacks effects
- Sword-pulling cutscene 
- Basic environment and greybox level setup
- Basic manual Unity CI

---

## Gameplay Loop

1. Explore the level 
2. Approach and engage enemies.
3. Use light attacks, heavy attacks, specials, to survive and or advance
4. Test different combat interactions and enemy behaviors
5. Continue toward a more complete playable level and encounter structure.

---

## Combat System

The main systems include:

- Attack definitions and combat states
- Player and enemy combat controllers
- Hitbox-based attack detection
- Health and damage attributes
- Special moves such as:
  - Shockwave
  - Heavy impact
  - Weapon teleportation
  - Throwing and recalling weapons

The combat system is still under active development. Attack timing, transitions, hit feedback, and edge cases are being refined through dedicated testing tasks.

---

## Project Structure

```text
Assets/
├── Character/              Player models and animations
├── Cutscenes/              Timeline cutscenes and transitions
├── Enemies/                Enemy models, controllers, and effects
├── Environment/            Level and environment assets
├── Materials/              Shared materials
├── ParticleAssets/         Particle and visual effects
├── PlayerInputActions/     Unity Input System assets and handlers
├── Prefabs/                Reusable game objects
├── Scenes/                 Unity scenes
├── Scripts/
│   ├── CharacterInterfaces/
│   ├── Combat/
│   ├── Enemy/
│   ├── GlobalScripts/
│   └── Player/
├── Tests/                  Edit mode and play mode tests
├── VFX/                    Visual effects
└── Weapons/                Weapon-specific assets and scripts
