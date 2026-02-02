# Repository Guidelines

## Project Structure & Module Organization
`Assets/Scripts` contains gameplay and UI logic. Core domain model types live in `Assets/Scripts/GameModel` (state, units, map cells, influence maps). Shared utilities are in `Assets/Scripts/YYZ`, while third-party binaries and wrappers are in `Assets/Scripts/Lib`.

Scenes and render settings are under `Assets/Scenes` and `Assets/Settings`. UI Toolkit layouts and styles are in `Assets/UIDocments` and `Assets/UIDocments/Styles` (keep the existing folder name). Runtime data and scenario content are in `Assets/StreamingAssets`.

## Build, Test, and Development Commands
- `"<UnityEditorPath>\\Unity.exe" -projectPath .` - open the project with Unity 6.
- `"<UnityEditorPath>\\Unity.exe" -batchmode -quit -projectPath .` - run a headless import and compile check.
- `dotnet build "Operation Brevity.slnx"` - compile generated C# project files from CLI (after Unity has generated them).

Use Unity Editor version `6000.3.2f1` (`ProjectSettings/ProjectVersion.txt`) to avoid asset reserialization churn.

## Coding Style & Naming Conventions
Use C# with 4-space indentation and UTF-8 text files. Follow existing conventions:
- `PascalCase` for types, methods, properties, and enums.
- `camelCase` for local variables and parameters.
- Unity-serialized fields are often public; keep inspector-facing names clear.

Keep scripts focused by feature (for example, map behavior in `GameModel/*`, UI interaction in `Overlay.cs`, `DialogRoot.cs`, and related files). Do not move or rename assets without committing matching `.meta` files.

## Testing Guidelines
There are currently no committed automated test assemblies. Validate changes with:
- Play Mode checks in `Assets/Scenes/SampleScene.unity`.
- Targeted regression checks for movement and pathfinding, combat resolution, and UI dialogs.

If adding tests, prefer Unity Test Framework with folders `Assets/Tests/EditMode` and `Assets/Tests/PlayMode`, and name files `*Tests.cs`.

## Commit & Pull Request Guidelines
Recent history uses short, imperative commit subjects (for example, `Add influence map and sounds`, `Update readme.md`). Keep subject lines concise and descriptive.

PRs should include scope summary, gameplay impact, manual test steps, and screenshots or GIFs for UI changes. Link related issues and call out any scenario or data file updates under `Assets/StreamingAssets`.
