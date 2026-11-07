# Testing

The project has three CI layers:

- **Static validation** (`.github/scripts/validate_project.py`): runs without a Unity license. It checks ProjectSettings JSON, asset `.meta` files, duplicate GUIDs, missing script references, build scenes, assembly definitions, input bindings, and merge-conflict markers.
- **EditMode tests** (`Assets/Tests/Edit`): validate scenes, prefabs, input actions, combat action definitions, animator parameters, health lights, wall/golem wiring, and sword hit detection.
- **PlayMode tests** (`Assets/Tests/Play`): load the build scene and verify runtime combat definitions, faction registration, enemy health lights, damage/glow changes, F-block wall spawning, and the right-click heavy attack.

The tests intentionally use reflection for `Assembly-CSharp` types because test assemblies cannot reference the project's default assembly directly.

## Run manually

1. Open the project in Unity `6000.6.4f1`.
2. Open **Window > General > Test Runner**.
3. Run the **EditMode** tests, then the **PlayMode** tests.

Headless examples (run from the repository root):

```bash
python3 .github/scripts/validate_project.py

~/Unity/Hub/Editor/6000.6.4f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath "$PWD" \
  -runTests -testPlatform editmode \
  -testResults "$PWD/TestResults/editmode.xml" \
  -logFile "$PWD/TestResults/editmode.log"

~/Unity/Hub/Editor/6000.6.4f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath "$PWD" \
  -runTests -testPlatform playmode \
  -testResults "$PWD/TestResults/playmode.xml" \
  -logFile "$PWD/TestResults/playmode.log"
```

Do **not** add `-quit` to these commands. On Unity `6000.6.4f1`, `-quit` can exit after the asset import and before the test runner starts.

The main scene must be added to **File > Build Profiles** and its active camera tagged `MainCamera` for the PlayMode tests to pass.

## Continuous integration

`.github/workflows/unity-tests.yml` runs on pull requests targeting `main` or `testing_scripts`, and can also be started manually with **workflow_dispatch**. It does not run on pushes.

1. **Validate project** always runs the static validator.
2. **Unity editmode tests** and **Unity playmode tests** run only when all three Unity license secrets are present.

### Enable the Unity jobs

A free/personal Unity license is stored as a `.ulf` file. On Linux it is usually at:

```text
~/.local/share/unity3d/Unity/Unity_lic.ulf
```

Add these repository secrets (**Settings > Secrets and variables > Actions**):

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | Full contents of `Unity_lic.ulf` |
| `UNITY_EMAIL` | The Unity account email |
| `UNITY_PASSWORD` | The Unity account password |

With the GitHub CLI:

```bash
gh secret set UNITY_LICENSE < ~/.local/share/unity3d/Unity/Unity_lic.ulf
gh secret set UNITY_EMAIL
gh secret set UNITY_PASSWORD
```

Never commit `Unity_lic.ulf` or add it to the repository. If the secrets are absent, the workflow still passes the static job and prints a notice that the Unity jobs were skipped.

## Manual QA

Use [Docs/QA_CHECKLIST.md](../../Docs/QA_CHECKLIST.md) to verify block/golem behavior, enemy animations, health glow, hit detection, and hit reactions in the editor.
