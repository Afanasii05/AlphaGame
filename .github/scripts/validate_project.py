#!/usr/bin/env python3
"""Fast, license-free sanity checks for the Unity project.

These checks run on every pull request before the (optional) Unity test job.
They catch the problems that most often break a Unity CI run: broken JSON/YAML,
missing .meta files, duplicate GUIDs, dangling project scripts, broken build
settings and input bindings that no longer match the gameplay code.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets"
PROJECT_SETTINGS = ROOT / "ProjectSettings"

GUID_RE = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)
SCRIPT_RE = re.compile(
    r"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-f]{32}),\s*type:\s*3\}"
    r"\s*\n\s*m_Name:[^\n]*"
    r"\s*\n\s*m_EditorClassIdentifier:\s*Assembly-CSharp::([^\n]+)"
)
BUILD_SCENE_RE = re.compile(
    r"-\s*enabled:\s*1\s*\n\s*path:\s*([^\n]+)\s*\n\s*guid:\s*([0-9a-f]{32})"
)
TEXT_SUFFIXES = {
    ".cs",
    ".asmdef",
    ".json",
    ".yml",
    ".yaml",
    ".md",
    ".asset",
    ".prefab",
    ".unity",
    ".inputactions",
    ".meta",
}

errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def check_required_files() -> None:
    required = [
        PROJECT_SETTINGS / "ProjectVersion.txt",
        PROJECT_SETTINGS / "EditorBuildSettings.asset",
        ROOT / "Packages" / "manifest.json",
        ROOT / "Packages" / "packages-lock.json",
        ASSETS / "PlayerInputActions" / "PlayerActions.inputactions",
        ASSETS / "PlayerInputActions" / "ActionsHandler.cs",
        ASSETS / "Scenes" / "SampleScene.unity",
        ASSETS / "Tests" / "Edit" / "ProjectHealthTests.cs",
        ASSETS / "Tests" / "Play" / "GameplayTests.cs",
    ]
    for path in required:
        if not path.is_file():
            fail(f"Required file is missing: {relative(path)}")

    version = PROJECT_SETTINGS / "ProjectVersion.txt"
    if version.is_file() and not re.search(
        r"^m_EditorVersion:\s*\S+", read_text(version), re.MULTILINE
    ):
        fail("ProjectSettings/ProjectVersion.txt has no m_EditorVersion.")


def check_json() -> None:
    json_files = [
        ROOT / "Packages" / "manifest.json",
        ROOT / "Packages" / "packages-lock.json",
        *ASSETS.rglob("*.asmdef"),
        *ASSETS.rglob("*.inputactions"),
    ]
    for path in json_files:
        try:
            json.loads(read_text(path))
        except json.JSONDecodeError as exc:
            fail(f"Invalid JSON in {relative(path)}: {exc}")


def check_meta_files() -> dict[str, str]:
    guid_to_asset: dict[str, str] = {}

    for path in ASSETS.rglob("*"):
        if not path.is_file() or path.name.startswith(".") or path.suffix == ".meta":
            continue

        meta = Path(str(path) + ".meta")
        if not meta.is_file():
            fail(f"Missing .meta file: {relative(path)}")
            continue

        match = GUID_RE.search(read_text(meta))
        if not match:
            fail(f"Missing or invalid guid in {relative(meta)}")
            continue

        guid = match.group(1)
        rel = relative(path)
        if guid in guid_to_asset:
            fail(f"Duplicate guid {guid}: {guid_to_asset[guid]} and {rel}")
            continue
        guid_to_asset[guid] = rel

    for meta in ASSETS.rglob("*.meta"):
        if meta.name.startswith("."):
            continue
        asset = Path(str(meta)[: -len(".meta")])
        if not asset.exists():
            fail(f"Orphan .meta file: {relative(meta)}")

    return guid_to_asset


def check_conflict_markers() -> None:
    for path in list(ASSETS.rglob("*")) + list((ROOT / ".github").rglob("*")):
        if not path.is_file() or path.suffix.lower() not in TEXT_SUFFIXES:
            continue
        for number, line in enumerate(read_text(path).splitlines(), start=1):
            if line.startswith("<<<<<<< ") or line.startswith(">>>>>>> "):
                fail(f"Merge conflict marker in {relative(path)}:{number}")
                break


def check_build_scenes(guid_to_asset: dict[str, str]) -> None:
    path = PROJECT_SETTINGS / "EditorBuildSettings.asset"
    if not path.is_file():
        return

    scenes = BUILD_SCENE_RE.findall(read_text(path))
    if not scenes:
        fail("EditorBuildSettings.asset has no enabled build scene.")
        return

    for scene_path, guid in scenes:
        scene_path = scene_path.strip().strip("'\"")
        if not (ROOT / scene_path).is_file():
            fail(f"Build scene does not exist: {scene_path}")
        mapped = guid_to_asset.get(guid)
        if mapped is not None and mapped != scene_path:
            fail(f"Build scene GUID {guid} points to {mapped}, not {scene_path}.")


def check_script_references(guid_to_asset: dict[str, str]) -> None:
    for path in ASSETS.rglob("*"):
        if not path.is_file() or path.suffix not in {".prefab", ".unity", ".asset"}:
            continue
        for guid, class_name in SCRIPT_RE.findall(read_text(path)):
            target = guid_to_asset.get(guid)
            if target is None:
                fail(
                    f"{relative(path)} references missing project script "
                    f"{class_name} (guid {guid})."
                )
            elif not target.endswith(".cs"):
                fail(
                    f"{relative(path)} references {class_name} through {target}, "
                    "which is not a C# script."
                )


def check_asmdefs() -> None:
    names: dict[str, str] = {}
    for path in ASSETS.rglob("*.asmdef"):
        data = json.loads(read_text(path))
        name = data.get("name")
        if not name:
            fail(f"{relative(path)} has no assembly name.")
            continue
        if name in names:
            fail(f"Duplicate assembly name {name}: {names[name]} and {relative(path)}")
            continue
        names[name] = relative(path)

    edit_asmdef = ASSETS / "Tests" / "Edit" / "Test.EditMode.asmdef"
    if edit_asmdef.is_file():
        data = json.loads(read_text(edit_asmdef))
        if "TestAssemblies" not in data.get("optionalUnityReferences", []):
            fail(f"{relative(edit_asmdef)} does not reference the test assemblies.")
        if "Unity.InputSystem" not in data.get("references", []):
            fail(
                f"{relative(edit_asmdef)} must reference Unity.InputSystem "
                "for the input binding tests."
            )

    play_asmdef = ASSETS / "Tests" / "Play" / "Test.PlayMode.asmdef"
    if play_asmdef.is_file():
        data = json.loads(read_text(play_asmdef))
        if "TestAssemblies" not in data.get("optionalUnityReferences", []):
            fail(f"{relative(play_asmdef)} does not reference the test assemblies.")


def check_input_bindings() -> None:
    path = ASSETS / "PlayerInputActions" / "PlayerActions.inputactions"
    if not path.is_file():
        return

    data = json.loads(read_text(path))
    maps = {entry["name"]: entry for entry in data.get("maps", [])}

    expected = {
        "Player": {
            "Block": "<Keyboard>/f",
            "Crouch": "<Keyboard>/leftShift",
            "Sprint": "<Keyboard>/leftCtrl",
            "Jump": "<Keyboard>/space",
            "TogglePerspective": "<Keyboard>/p",
        },
        "Weapon": {
            "Toggle": "<Keyboard>/e",
            "Click": "<Mouse>/leftButton",
            "RightClick": "<Mouse>/rightButton",
            "Special": "<Keyboard>/z",
            "Charge": "<Keyboard>/q",
            "Teleport": "<Keyboard>/t",
        },
    }

    for map_name, actions in expected.items():
        action_map = maps.get(map_name)
        if action_map is None:
            fail(f"Input action map is missing: {map_name}")
            continue

        names = {action["name"] for action in action_map.get("actions", [])}
        bindings = action_map.get("bindings", [])

        for action_name, binding_path in actions.items():
            if action_name not in names:
                fail(f"Input action is missing: {map_name}/{action_name}")
                continue
            paths = {
                binding.get("path")
                for binding in bindings
                if binding.get("action") == action_name
            }
            if binding_path not in paths:
                found = sorted(path for path in paths if path)
                fail(
                    f"Input action {map_name}/{action_name} is not bound to "
                    f"{binding_path} (found: {found})."
                )


def main() -> int:
    check_required_files()
    check_json()
    guid_to_asset = check_meta_files()
    check_conflict_markers()
    check_build_scenes(guid_to_asset)
    check_script_references(guid_to_asset)
    check_asmdefs()
    check_input_bindings()

    if errors:
        print(f"Project validation failed with {len(errors)} error(s):\n")
        for error in errors:
            print(f"  - {error}")
        return 1

    print(
        "Project validation passed "
        f"({len(guid_to_asset)} assets, {len(list(ASSETS.rglob('*.asmdef')))} asmdefs)."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
