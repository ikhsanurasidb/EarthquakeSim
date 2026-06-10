# Changelog

All notable changes to this project are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

### Added

- **Gaze interaction system** (`Assets/Scripts/Gaze/`)
  - `GazePointer` — casts a physics ray from the camera forward direction each frame; selects a `GazeInteractable` after a configurable dwell time and fires an Interact action.
  - `GazeInteractable` — component placed on scene objects; holds display name, severity, description, and action text exposed to the popup.
  - `HazardSeverity` enum — `None / Low / Medium / High`, shared across gaze and UI layers.
  - Gaze reticle shader and materials (`Assets/_GazeVR/Shaders/GazeReticle.shader`, `Assets/_GazeVR/Materials/`).

- **Game logic** (`Assets/Scripts/Game/`)
  - `LessonManager` — singleton that registers hazard interactables, tracks found count, and fires `onProgress` / `onAllHazardsFound` Unity Events.
  - `ClassroomAttendance` — manages student character activation and seating assignment.

- **UI system** (`Assets/Scripts/UI/`)
  - `HazardPopup` — world-space canvas that positions itself in front of the player when a hazard is selected; header background color reflects severity (green / yellow / red).
  - `HazardHud` — world-space HUD counter (`Hazards found: X/Y`) parented to the camera; shows a completion banner once all hazards are identified.

- **EarthquakeClassroom scene** (`Assets/Scenes/EarthquakeClassroom.unity`)
  - Classroom room geometry (floor, four walls, ceiling).
  - 12 student desks (`table3`) with paired chairs, one teacher desk (`table1`), and a whiteboard.
  - 10 low-poly student characters seated at desks.
  - 10 tagged hazard objects: three lockers, storage rack, glass display cabinet, window jalousie, ceiling projector, wall speaker, exit door, and one safe object (sturdy desk).
  - Gaze reticle, HazardHUD, and HazardPopup wired to `LessonManager`.

- **Google Cardboard XR plugin** (`com.google.xr.cardboard` via Git URL)
  - `CardboardStartup` script initialises stereo rendering at runtime.
  - `AndroidCardboardSetup` editor script automates Cardboard-specific player settings.
  - Custom Android Gradle templates (`Assets/Plugins/Android/`) enabling the Cardboard native library.
  - XR loader and settings assets (`Assets/XR/`).

- **Editor tools** (`Assets/Editor/`)
  - `ClassroomBuilder` — procedurally places desks and chairs in a grid layout.
  - `MaterialSanitizer` — batch-repairs null material slots on school prop prefabs.

### Changed

- Replaced all 12 `table2` student desks in the classroom with `table3`.
- Upgraded all school prop prefabs (`Assets/Environments/School/Prefabs/props/`) to Unity 6
  serialization format: `Transform.serializedVersion 2`, `MeshCollider.serializedVersion 5`,
  corrected null material slots, added `m_ConstrainProportionsScale`,
  `m_RayTracingAccelStructBuildFlags`, and physics layer mask fields.
- Renamed Unity product from *GazeBasedVR* to *EarthquakeSim* in `ProjectSettings`.
- Added `EarthquakeClassroom` to Editor Build Settings at index 0.
- Configured Android build for Cardboard XR: landscape orientation, custom Gradle templates,
  Vulkan pre-transform disabled, `androidApplicationEntry` set to Activity mode.

### Fixed

- HUD counter (`Counter`) was anchored to the bottom of the HUD canvas with insufficient offset,
  causing it to clip 15 px below the canvas edge. Moved anchor to top-center with
  `anchoredPosition.Y = -55` and updated text alignment to `UpperCenter`.
