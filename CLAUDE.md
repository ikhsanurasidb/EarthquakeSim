# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**GazeBasedVR** is a Unity 6 VR application built around gaze-based interaction in a school/classroom environment. It uses the Universal Render Pipeline (URP) and Unity's new Input System with XR controller bindings.

## Unity Editor Commands

Unity has no CLI hot-reload — all scene/asset changes require the Editor open. Use Unity MCP tools (`mcp__unity-mcp__*`) to interact with the Editor directly from this session.

Run tests via Unity CLI (batch mode):
```
Unity.exe -batchmode -runTests -projectPath "C:\Users\USER\GazeBasedVR" -testPlatform EditMode -testResults results.xml
Unity.exe -batchmode -runTests -projectPath "C:\Users\USER\GazeBasedVR" -testPlatform PlayMode -testResults results.xml
```

Build via Unity CLI:
```
Unity.exe -batchmode -quit -projectPath "C:\Users\USER\GazeBasedVR" -buildTarget StandaloneWindows64 -executeMethod BuildScript.Build
```

Find Unity.exe under: `C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe`

## Architecture

### Render Pipeline
URP 17.4.0 (Unity 6). Two renderer data assets exist for platform targeting:
- `Assets/Settings/PC_RPAsset.asset` + `PC_Renderer.asset` — desktop/PC quality
- `Assets/Settings/Mobile_RPAsset.asset` + `Mobile_Renderer.asset` — mobile/standalone XR quality

Color space: **Linear**.

### Input System
Uses **Unity Input System 1.19.0** (not the legacy `Input` class). The action asset is `Assets/InputSystem_Actions.inputactions` with two action maps:
- **Player**: Move (Vector2), Look (Vector2), Jump, Sprint, Crouch, Interact (Hold), Attack, Previous, Next
- **UI**: Navigate, Submit, Cancel, Point, Click, ScrollWheel, TrackedDevicePosition/Orientation

Control schemes: `Keyboard&Mouse`, `Gamepad`, `Touch`, `Joystick`, **`XR`** (XRController bindings are wired for all major player and UI actions).

### Key Packages
| Package | Version | Purpose |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.4.0 | URP rendering |
| `com.unity.inputsystem` | 1.19.0 | New Input System |
| `com.unity.modules.xr` + `com.unity.modules.vr` | 1.0.0 | XR/VR runtime |
| `com.unity.ai.navigation` | 2.0.11 | NavMesh for NPC/character movement |
| `com.unity.ai.assistant` | 2.5.0-pre.2 | Unity AI Assistant (Muse) |
| `com.unity.timeline` | 1.8.11 | Cutscenes / sequencing |
| `com.unity.visualscripting` | 1.9.10 | Visual scripting support |
| `com.unity.test-framework` | 1.6.0 | NUnit-based testing |

### Asset Structure
```
Assets/
├── Characters/LowPoly/        # Low-poly male character variants (male01–03 × 3 outfits), glasses, sofa
│   ├── Materials/             # 13 flat-color materials (no textures)
│   ├── Models/                # FBX source files
│   └── Prefabs/               # Ready-to-instantiate prefabs
├── Environments/School/       # Classroom/school environment
│   ├── props/                 # FBX props: chairs, desks, computers, books, boards, doors, bus, fire
│   ├── road/                  # Road/exterior assets
│   ├── Prefabs/               # Environment prefabs
│   └── DemoScene/             # Reference scenes (all_.unity, all_objects.unity)
├── Scenes/SampleScene.unity   # Main working scene
├── Settings/                  # URP pipeline and volume profiles
└── InputSystem_Actions.inputactions
```

### C# Scripts
No custom gameplay scripts exist yet — only Unity template scripts (`Readme.cs`, `ReadmeEditor.cs`). All new scripts go under `Assets/` in a logical subfolder (e.g., `Assets/Scripts/Gaze/`, `Assets/Scripts/XR/`).

### XR / Gaze Implementation Notes
- The VR/XR modules are included but no XR SDK (OpenXR, Oculus, etc.) is in `manifest.json` yet — this will need to be added via Package Manager before XR builds work.
- Gaze interaction will likely use `Physics.Raycast` from the camera/HMD forward direction, or Unity's XR Interaction Toolkit (not yet installed).
- The Input System already has `TrackedDevicePosition` and `TrackedDeviceOrientation` UI actions bound to `<XRController>`, providing the foundation for controller-based UI interaction.
