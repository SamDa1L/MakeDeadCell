# Repository Guidelines

## Project Structure & Module Organization
This repository targets Unity 2022.3.14f1. Gameplay code is split by module under `Assets/Scripts` (e.g., `DeadCells.Core`, `DeadCells.Player`, `DeadCells.Combat`, `DeadCells.Rooms`, `DeadCells.Data`, `DeadCells.Game`), each gated by its own `.asmdef` for clean dependencies. Scenes for quick smoke checks live in `Assets/Scenes/TestScene.unity` and `SampleScene.unity`. Data-driven content and LDtk sources are stored in `Assets/Data/LDtkLevel`, while art pipelines live in `Assets/Art`, `PlayerCharacter`, and `blender_scripts`. Keep `Library`, `Logs`, and `Temp` untracked; they are regenerated locally.

## Build, Test, and Development Commands
Open the project through Unity Hub or run `"<UnityEditor>/Unity.exe" -batchmode -projectPath "%CD%" -quit -logFile Logs/editor.log` for headless imports. Run automated tests locally with `"<UnityEditor>/Unity.exe" -batchmode -projectPath "%CD%" -runTests -testPlatform EditMode -testResults Logs/EditModeResults.xml`. CI mirrors these commands via `game-ci/unity-test-runner` and `unity-builder`; align Unity version and ensure `Library/LDtkTilesetExporter` scripts exist before pushing.

## Coding Style & Naming Conventions
Stick to four-space indentation, `namespace DeadCells.<Module>`, PascalCase for classes/methods/properties, and lowerCamelCase (without underscores) for serialized fields to match existing components like `PlayerController`. Keep XML documentation on public APIs and targeted `[Header]` attributes for inspector groups. Place editor tooling in `Editor` subfolders so Unity filters compilation correctly.

## Testing Guidelines
Add EditMode tests beside the module they cover (e.g., `Assets/Scripts/DeadCells.Core/Tests/DeadCells.Core.Tests.asmdef`) and PlayMode scenarios under `Assets/Scenes` when behaviours depend on physics. Name test classes `<Feature>Tests` and assert both state changes and Unity events. The GitHub Actions workflow expects green Unity Test Runner output on Windows, macOS, and Linux; reproduce failures locally before opening a PR.

## Commit & Pull Request Guidelines
Recent history favours short, single-line commit messages (often in Chinese, e.g., `error fix upload`); keep the style concise, optionally prefixing the affected module (`Player: fix jump delay`). Bundle related changes and avoid mixing asset imports with gameplay code. Pull requests must complete `.github/pull_request_template.md`: provide a description, link issues (`Fixes #123`), record test environments, tick relevant checklists, and include screenshots or GIFs for gameplay-affecting changes.

## LDtk & Asset Tips
Always run "Install / Auto-add command" on `Assets/Data/LDtkLevel/LDtkLevelTest.ldtk` after cloning so LDtk exports update `.ldtkt` assets. Commit both the `.ldtk` sources and generated `.ldtkt` files to keep designers in sync. Large sprites, animations, and renders should stay in Git LFS; re-exported art belongs in `Assets/Art` or `renders`, never in `Temp`.
