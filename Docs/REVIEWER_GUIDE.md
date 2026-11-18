# Reviewer Guide

> **How to open, run, and test the project. If you follow this from top to bottom you will be able to play the game and review the code behind it.**

---

## Requirements

- Unity 6000.6.4f1 
- Git

---

## Getting Started

You can use the release build or do the following if it doesnt work: (sorry, i don't have a windows machine)
1. Clone the repo.
2. Open the project in Unity Hub using the version listed above.
3. Open `Assets/Scenes/Death Valley` and press Play to start with the cinematic, or open `Assets/Scenes/World` to skip straight to the game.
4. The Death Valley cinematic loads the World scene automatically when it ends.

---

## Building a Release

The release build starts with the Death Valley cinematic and then loads the World scene:

1. Open **File > Build Profiles** (or **Build Settings**).
2. Use this **Scene List** order:
   - `Assets/Scenes/Death Valley.unity` (enabled)
   - `Assets/Scenes/World.unity` (enabled)
   - `Assets/Scenes/SampleScene.unity` (disabled)
3. `Battlefield_Cutscene` has a `CutsceneSceneLoader` component. It loads `World` after the timeline ends.
4. Build for Windows or Linux.
5. Zip the **entire build folder** before uploading it to GitHub Releases.

You can re-apply the release scene flow and build order from:
`Tools > Alpha > World > Setup Release Scene Flow`

---

## Controls

| Input | Action |
| --- | --- |
| WASD | Move (camera relative) |
| Mouse | Look |
| Space | Jump |
| Left Shift | Sprint |
| Left Ctrl | Crouch |
| F (hold) | Block, raises a block wall in front of the player |
| P | Toggle camera perspective |
| LMB | Light attack. |
| RMB | Heavy attack. |
| E | Weapon toggle. Throws the equipped sword, or recalls it when thrown |
| Q | Charge attack, plays the shockwave charge |
| T | Teleport to the thrown sword, only works while the weapon is out |

Notes:

- WASD moves the character relative to the camera
- RMB has two variants: heavy grounded impact and heavy fall while in the air
- E is context sensitive, throwing and recalling are the same key.
- Sprint, crouch, and block are hold actions, jump is a press.

---

## Where to Look in Code


```text
Assets/Scripts/GlobalScripts/InputHandler.cs     Reads input, fires events
Assets/PlayerInputActions/PlayerActions.inputactions   Actual key bindings
Assets/Scripts/Player/PlayerController.cs        Movement, sprint, gravity
Assets/Scripts/Player/CameraController.cs        Camera and perspective toggle
Assets/Scripts/Combat/CombatMovement/TriggerAttack.cs   Routes inputs to combat actions
Assets/Scripts/Combat/CombatMovement/PlayerCombatState.cs   Combo and combat state logic
Assets/Scripts/Combat/AtackDetection/            Hitboxes and hit detection
Assets/Scripts/Enemy/EnemyAI.cs                  Basic enemy AI
```

---

## Testing

- `Assets/Tests/Edit/ProjectHealthTests.cs` for edit mode checks.
- `Assets/Tests/Play/GameplayTests.cs` for play mode checks.
- Run them from `Window > General > Test Runner`.

---


