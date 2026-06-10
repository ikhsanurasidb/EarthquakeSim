# GazeBasedVR — Architecture & Design Documentation

A Unity 6 VR application for earthquake-safety education in a school classroom.
Students use **gaze** to identify hazards, then experience a procedural earthquake drill.

---

## Table of Contents

1. [Tech Stack](#1-tech-stack)
2. [High-Level Architecture](#2-high-level-architecture)
3. [Scene Hierarchy](#3-scene-hierarchy)
4. [Runtime Systems](#4-runtime-systems)
   - 4.1 [Gaze Input System](#41-gaze-input-system)
   - 4.2 [Lesson & Progression System](#42-lesson--progression-system)
   - 4.3 [Earthquake Simulation System](#43-earthquake-simulation-system)
   - 4.4 [UI System](#44-ui-system)
   - 4.5 [XR / Cardboard Layer](#45-xr--cardboard-layer)
5. [Class Relationships](#5-class-relationships)
6. [Design Patterns](#6-design-patterns)
7. [End-to-End Program Flow](#7-end-to-end-program-flow)
8. [Event Architecture](#8-event-architecture)
9. [Editor Tooling](#9-editor-tooling)
10. [Platform & Build Configuration](#10-platform--build-configuration)

---

## 1. Tech Stack

| Layer | Technology | Version |
|---|---|---|
| Engine | Unity 6 | 6000.x |
| Render Pipeline | Universal Render Pipeline (URP) | 17.4.0 |
| XR SDK | Google Cardboard XR Plugin | via Package Manager |
| Input | Unity Input System | 1.19.0 |
| Head Tracking | `TrackedPoseDriver` (XR Core Utils) | built-in |
| Navigation (future) | Unity AI Navigation / NavMesh | 2.0.11 |
| Scripting | C# (.NET Standard 2.1) | — |
| Testing | Unity Test Framework (NUnit) | 1.6.0 |
| Color Space | Linear | — |
| Target Platform | Android (Cardboard) + Editor (desktop) | — |

---

## 2. High-Level Architecture

The application is divided into five horizontal layers. Each layer depends only on the ones below it.

```mermaid
flowchart TD
    subgraph XR["XR / Platform Layer"]
        CS[CardboardStartup\nQR scan · recenter · brightness]
        TPD[TrackedPoseDriver\n3-DOF head rotation]
    end

    subgraph INPUT["Gaze Input Layer"]
        GP[GazePointer\nraycast · reticle · trigger]
        GI[GazeInteractable\nhover pulse · click events]
    end

    subgraph GAME["Game Logic Layer"]
        LM[LessonManager\nhazard registry · progress · drill trigger]
        CA[ClassroomAttendance\npresent / total / passRatio]
        ES[EarthquakeShaker\nPerlin-noise positional shake]
        SO[ShakeableObject\nper-prop secondary rattle]
    end

    subgraph UI["UI / Feedback Layer"]
        HP[HazardPopup\nworld-space card · severity colour]
        HH[HazardHud\ncounter · completion · drill warning]
    end

    subgraph SHARED["Shared / Data"]
        HS[HazardSeverity enum\nSafe · Caution · Danger]
    end

    XR --> INPUT
    INPUT --> GAME
    GAME --> UI
    SHARED --> INPUT
    SHARED --> UI
    SHARED --> GAME
```

---

## 3. Scene Hierarchy

```mermaid
flowchart TD
    ROOT([Scene Root])

    ROOT --> DL[Directional Light]
    ROOT --> CLASS[Classroom\nwalls · floor · ceiling]
    ROOT --> LMN[LessonManager\nLessonManager · ClassroomAttendance · CardboardStartup]

    ROOT --> PLAYER[Player\nEarthquakeShaker]
    PLAYER --> CAM[Main Camera\nCamera · TrackedPoseDriver · GazePointer]
    CAM --> RET[GazeReticle\nQuad Mesh · reticle material]
    CAM --> HUD[HazardHUD\nWorld-Space Canvas]
    HUD --> CTR[Counter\nText]
    HUD --> BANNER[CompletionBanner\nImage + Text]
    HUD --> DWP[DrillWarningPanel\nImage + Text]

    ROOT --> POPUP[HazardPopup\nWorld-Space Canvas]
    POPUP --> PANEL[Panel\nhidden by default]
    PANEL --> HDR[Header\nImage + Text]
    PANEL --> BODY[BodyText\nText]

    ROOT --> FURN[Furniture\nShakeableObject on each child]
    FURN --> DESKS[12× table3]
    FURN --> CHAIRS[12× chair]
    FURN --> T1[table1\nteacher's desk]
    FURN --> BRD[Board]

    ROOT --> PPL[People\nstudent + teacher prefabs]

    ROOT --> HAZ[Hazards\nShakeableObject on each child except door]
    HAZ --> LOCK[3× locker\nGazeInteractable on one]
    HAZ --> RACK[rack]
    HAZ --> SHOW[showcase]
    HAZ --> JAL[jalousie\nwindow]
    HAZ --> PROJ[projector]
    HAZ --> SPK[speaker]
    HAZ --> DOOR[a door\nno ShakeableObject]
    HAZ --> SDESK[SturdyDesk]
```

---

## 4. Runtime Systems

### 4.1 Gaze Input System

**Scripts:** `GazePointer`, `GazeInteractable`

`GazePointer` fires a `Physics.Raycast` from the camera's forward vector every frame — this *is* the gaze on a 3-DOF Cardboard headset. It drives hover transitions and dispatches the trigger-press to the currently-hovered `GazeInteractable`.

`GazeInteractable` is a data + behaviour component placed on any collidable prop. It owns the hover-pulse scale animation and the Discovered flag. It deliberately uses Unity `SendMessage`-compatible method names (`OnPointerEnter`, `OnPointerExit`, `OnPointerClick`) so it works with both `GazePointer` and Google's legacy `CardboardReticlePointer` without a code change.

**Click logic:**

```mermaid
flowchart TD
    A([OnPointerClick]) --> B{Discovered\nalready?}
    B -- No --> C[Set Discovered = true]
    C --> D[Fire onFirstDiscovered\n→ LessonManager counts it]
    D --> E[Fire onSelected\n→ HazardPopup.Show]
    B -- Yes --> E
```

**Editor / desktop fallback:** When no XR device is active, `GazePointer` enables right-mouse-drag mouse-look so the entire mechanic is testable in the Unity Editor without a headset.

---

### 4.2 Lesson & Progression System

**Scripts:** `LessonManager`, `ClassroomAttendance`

`LessonManager` is the central coordinator. On `Awake` it auto-discovers every `GazeInteractable` with `countsTowardLesson = true` and subscribes to each one's `onFirstDiscovered` event. It maintains an internal `HashSet` of found hazards so duplicate selections are idempotent.

```mermaid
flowchart LR
    GI1[GazeInteractable A] -->|onFirstDiscovered| LM
    GI2[GazeInteractable B] -->|onFirstDiscovered| LM
    GI3[GazeInteractable ...] -->|onFirstDiscovered| LM
    LM[LessonManager] -->|onProgress| HH[HazardHud]
    LM -->|onAllHazardsFound| HH
    LM -->|"WaitForSecondsRealtime(delay)"| DRILL[StartEarthquakeDrill]
    DRILL -->|onEarthquakeDrillStarted| ES[EarthquakeShaker]
    DRILL -->|onEarthquakeDrillStarted| HH
```

`ClassroomAttendance` is a simple data container (present / total / passRatio). It exists as a separate component to keep headcount concerns outside of `LessonManager` and to allow independent testing.

**Automatic drill trigger:** When the last hazard is found, `LessonManager` starts a `WaitForSecondsRealtime` coroutine. After `autoStartDrillDelay` seconds (default 3 s), `StartEarthquakeDrill()` is called. Using `WaitForSecondsRealtime` means the delay is immune to `Time.timeScale` changes.

---

### 4.3 Earthquake Simulation System

**Scripts:** `EarthquakeShaker`, `ShakeableObject`

Two cooperating components produce layered shake: one for the player's view, one for the environment.

```mermaid
flowchart TD
    START([onEarthquakeDrillStarted]) --> ES

    subgraph ES["EarthquakeShaker (on Player root)"]
        direction TB
        A1[Record _originLocalPos] --> A2[elapsed = 0]
        A2 --> A3["LateUpdate every frame"]
        A3 --> A4["t = elapsed / duration\nIntensity = curve.Evaluate(t)"]
        A4 --> A5["dx = Perlin(t·freq + seedX) × amplitude × Intensity\ndy = Perlin(t·freq + seedY) × amplitude × verticalScale × Intensity"]
        A5 --> A6[transform.localPosition = origin + offset]
        A6 --> A7{elapsed >= duration?}
        A7 -- No --> A3
        A7 -- Yes --> A8[Stop: restore origin, Intensity=0]
    end

    ES -->|"Current.IsShaking\nCurrent.Intensity\nCurrent.frequency"| SO

    subgraph SO["ShakeableObject (×35 props)"]
        direction TB
        B1["LateUpdate: poll EarthquakeShaker.Current"]
        B1 --> B2{IsShaking?}
        B2 -- No --> B3[snap back to _restLocalPos]
        B2 -- Yes --> B4["Perlin noise with own seeds\n+ random freq multiplier"]
        B4 --> B5[localPosition + localRotation offset]
    end
```

**Key design decision — polling vs. events for `ShakeableObject`:** With 35+ shakeable objects in the scene, subscribing each one to `onEarthquakeDrillStarted` would require 35 subscribe/unsubscribe calls and would leave dangling listeners if an object is destroyed mid-shake. Polling `EarthquakeShaker.Current` each `LateUpdate` is zero-allocation and self-cleaning — if `Current` is null or `IsShaking` is false the object simply snaps back, with no cleanup required.

**VR comfort:** `EarthquakeShaker` only displaces the Player's `localPosition` (lateral X + subtle vertical Y). Rotation is disabled by default (`rotationAmplitude = 0`). This prevents fighting with the `TrackedPoseDriver` rotation on the camera, which would cause disorienting conflicts in a 3-DOF Cardboard headset.

**Per-object randomisation:** Each `ShakeableObject` seeds its six Perlin-noise axes with `Random.Range(0, 512)` in `Awake` and multiplies `EarthquakeShaker.frequency` by a random ±30 % factor. This ensures a room full of identical desks still rattles organically.

---

### 4.4 UI System

**Scripts:** `HazardPopup`, `HazardHud`

Both canvases use **World Space** render mode — required for correct stereo rendering in Cardboard VR. Screen-space overlays only render to one eye.

| Canvas | Parent | Purpose |
|---|---|---|
| `HazardHUD` | Main Camera | Locked to the player's view; always visible |
| `HazardPopup` | Scene root | Floats in world space; faces the player via `LookRotation` |

**HazardPopup** subscribes to every `GazeInteractable.onSelected` in `Start`. When shown, it places itself `distance` metres in front of the player's horizontal gaze direction (Y clamped to avoid extreme up/down placement), then uses `LateUpdate` to keep facing the player as they move.

**HazardHud** drives three mutually exclusive panels based on lesson state:

```mermaid
stateDiagram-v2
    direction LR
    [*] --> Searching: Start\n(counter shown)
    Searching --> AllFound: onAllHazardsFound\n(green CompletionBanner)
    AllFound --> DrillActive: onEarthquakeDrillStarted\n(red DrillWarningPanel\nCompletionBanner hidden)
```

`HazardSeverity` drives the popup's header colour without any conditional logic in `HazardPopup` itself — the enum value is mapped to a colour via a C# `switch` expression, keeping all colour decisions in one place.

---

### 4.5 XR / Cardboard Layer

**Script:** `CardboardStartup`

`CardboardStartup` handles all Google Cardboard API calls. Every Cardboard API call is wrapped in `#if UNITY_ANDROID && !UNITY_EDITOR`, making this component a no-op in the Editor and on non-Android targets. This means the rest of the codebase is completely platform-agnostic.

| Responsibility | How |
|---|---|
| Keep screen awake | `Screen.sleepTimeout = SleepTimeout.NeverSleep` |
| Load viewer lens params | `Api.ScanDeviceParams()` on first run |
| Recenter HMD | `Api.Recenter()` on long trigger hold |
| Reload changed params | `Api.ReloadDeviceParams()` when `HasNewDeviceParams()` |
| Quit | `Application.Quit()` on close button |

Head rotation is provided by the `TrackedPoseDriver` component on the camera, set to `RotationOnly` tracking. This preserves the manually set eye height (`y = 1.6 m`) rather than snapping to the HMD origin.

---

## 5. Class Relationships

```mermaid
classDiagram
    direction TB

    class GazePointer {
        +float maxDistance
        +Transform reticle
        +GazeInteractable Current
        +bool enableEditorMouseLook
        -MaybeMouseLook()
        -UpdateReticle()
        -TriggerPressedThisFrame() bool
    }

    class GazeInteractable {
        +string displayName
        +HazardSeverity severity
        +string description
        +string recommendedAction
        +bool countsTowardLesson
        +bool Discovered
        +bool IsHovered
        +GazeInteractableEvent onFirstDiscovered
        +GazeInteractableEvent onSelected
        +OnPointerEnter()
        +OnPointerExit()
        +OnPointerClick()
        +Select()
    }

    class HazardSeverity {
        <<enumeration>>
        Safe
        Caution
        Danger
    }

    class LessonManager {
        +Instance$ LessonManager
        +float autoStartDrillDelay
        +int TotalHazards
        +int FoundHazards
        +bool DrillStarted
        +HazardProgressEvent onProgress
        +UnityEvent onAllHazardsFound
        +UnityEvent onEarthquakeDrillStarted
        +Register(GazeInteractable)
        +StartEarthquakeDrill()
        -HandleFirstDiscovered()
        -DelayedDrillStart() IEnumerator
    }

    class ClassroomAttendance {
        +int present
        +int total
        +float passRatio
        +float Ratio
        +bool MeetsThreshold
    }

    class EarthquakeShaker {
        +Current$ EarthquakeShaker
        +bool IsShaking
        +float Intensity
        +float duration
        +float frequency
        +float positionAmplitude
        +bool subscribeToLessonManager
        +Play()
        +Stop()
    }

    class ShakeableObject {
        +float positionAmplitude
        +float rotationAmplitude
        +float frequencyMultiplier
    }

    class HazardPopup {
        +Transform follow
        +float distance
        +Show(GazeInteractable)
        +Hide()
        -PlaceInFront()
    }

    class HazardHud {
        +Text counterText
        +GameObject completionBanner
        +GameObject drillWarningPanel
        +Text drillWarningText
    }

    class CardboardStartup {
        <<Android only>>
    }

    GazePointer "1" --> "0..1" GazeInteractable : hovers / clicks
    GazeInteractable --> HazardSeverity : classified by

    LessonManager "1" o-- "0..*" GazeInteractable : registers
    LessonManager "1" *-- "1" ClassroomAttendance : owns

    EarthquakeShaker ..> LessonManager : subscribes onDrillStarted
    ShakeableObject ..> EarthquakeShaker : polls Current each frame

    HazardPopup ..> GazeInteractable : subscribes onSelected
    HazardHud ..> LessonManager : subscribes onProgress\nonAllHazardsFound\nonDrillStarted

    HazardPopup --> HazardSeverity : maps to colour
```

---

## 6. Design Patterns

### Singleton (Convenience Singleton)

`LessonManager.Instance` and `EarthquakeShaker.Current` expose a global access point without relying on `FindObjectOfType` (which is slow). Both self-null on `OnDestroy` so they are scene-scoped and safe across scene reloads.

```
// Any script can do this without a serialized reference:
LessonManager.Instance?.StartEarthquakeDrill();
EarthquakeShaker.Current?.IsShaking
```

### Observer (UnityEvent / Publish–Subscribe)

`LessonManager` fires four typed events. Listeners subscribe in their own `Start` and unsubscribe in `OnDestroy`, keeping coupling unidirectional — the game logic layer has no knowledge of the UI layer.

```mermaid
flowchart LR
    PUB["LessonManager\n(Publisher)"]
    PUB -->|onHazardFound| A[inspector callbacks]
    PUB -->|onProgress| B[HazardHud.OnProgress]
    PUB -->|onAllHazardsFound| C[HazardHud.OnAllFound]
    PUB -->|onEarthquakeDrillStarted| D[EarthquakeShaker.Play]
    PUB -->|onEarthquakeDrillStarted| E[HazardHud.OnDrillStarted]
```

`GazeInteractable` also uses this pattern, with `onFirstDiscovered` and `onSelected` events that the popup and lesson manager subscribe to independently.

### Polling (Pull model for secondary shakers)

`ShakeableObject` **polls** `EarthquakeShaker.Current` instead of subscribing to `onEarthquakeDrillStarted`. Rationale: with 35+ instances, event subscription would require 35 Add/Remove listener calls and manual cleanup if an object is destroyed during a shake. Polling is zero-allocation and self-cleaning — no subscribe, no unsubscribe, no dangling reference risk.

### Entity–Component (Unity's model)

Behaviour is composed from small, single-purpose components rather than deep inheritance trees:

| Component | Single responsibility |
|---|---|
| `GazeInteractable` | Hover feedback + click event dispatch |
| `EarthquakeShaker` | Player-rig shake driven by intensity curve |
| `ShakeableObject` | Prop rattle, polling the shaker |
| `CardboardStartup` | Platform initialisation only |
| `ClassroomAttendance` | Headcount data only |

### Builder (Editor Tool)

`ClassroomBuilder` is a static editor-only builder that constructs the entire scene procedurally from configuration constants (`W`, `D`, `H`, prefab paths). This separates scene construction from runtime logic and allows the scene to be regenerated cleanly. The builder is intentionally unreachable at runtime (`Assets/Editor/` folder).

### Strategy (via Enum dispatch)

`HazardSeverity` acts as a strategy selector. Instead of subclassing `GazeInteractable` for each type of hazard, a single enum value drives header colour in `HazardPopup` and messaging tone. New severity levels can be added by extending the enum without touching class hierarchies.

### Façade

`LessonManager` exposes `TotalHazards`, `FoundHazards`, `DrillStarted`, and its four events as the only public surface. The internal `List<GazeInteractable>` (registry) and `HashSet<GazeInteractable>` (found set) are completely private.

### Graceful Degradation (Platform Abstraction)

`CardboardStartup` and `GazePointer` are designed so the full scene runs in the Unity Editor without any VR device:
- All `Google.XR.Cardboard.Api` calls compile away via `#if UNITY_ANDROID && !UNITY_EDITOR`
- `GazePointer.enableEditorMouseLook` provides right-mouse-drag look and left-click/Space for the trigger
- `TriggerPressedThisFrame()` checks Cardboard, Mouse, Keyboard, and Touchscreen in priority order

---

## 7. End-to-End Program Flow

```mermaid
sequenceDiagram
    actor Student
    participant CS as CardboardStartup
    participant TPD as TrackedPoseDriver
    participant GP as GazePointer
    participant GI as GazeInteractable
    participant LM as LessonManager
    participant HP as HazardPopup
    participant HH as HazardHud
    participant ES as EarthquakeShaker
    participant SO as ShakeableObject ×35

    Note over CS,LM: ── App Start ──
    CS->>CS: Screen never-sleep, load viewer params (Android)
    LM->>LM: Awake: find all GazeInteractables,\nsubscribe onFirstDiscovered on each
    LM->>HH: Start: onProgress(0, 8)\n→ "Hazards found: 0/8"
    ES->>LM: Start: subscribe to onEarthquakeDrillStarted

    Note over Student,HH: ── Hazard Discovery Loop (repeats per hazard) ──
    Student->>TPD: rotate head toward object
    TPD->>GP: update camera rotation
    GP->>GP: Raycast → hit collider
    GP->>GI: OnPointerEnter() → scale up
    Student->>GP: press trigger / click / tap
    GP->>GI: OnPointerClick()
    GI->>GI: Discovered = true (first time)
    GI->>LM: onFirstDiscovered(this)
    LM->>HH: onProgress(N, 8) → update counter
    GI->>HP: onSelected(this)
    HP->>HP: Show: place in front, set severity colour

    Note over LM,HH: ── All Hazards Found ──
    LM->>HH: onAllHazardsFound\n→ CompletionBanner shown\n"Get ready — earthquake incoming!"
    LM->>LM: StartCoroutine(DelayedDrillStart, 3 s)

    Note over LM,SO: ── Earthquake Drill (after 3 s delay) ──
    LM->>LM: StartEarthquakeDrill()\nDrillStarted = true
    LM->>ES: onEarthquakeDrillStarted → Play()
    LM->>HH: onEarthquakeDrillStarted → DrillWarningPanel shown\n"EARTHQUAKE! DROP • COVER • HOLD ON"

    loop every frame for 8 seconds
        ES->>ES: LateUpdate: Perlin-noise X/Y offset\non Player.localPosition
        SO->>ES: poll IsShaking + Intensity + frequency
        SO->>SO: LateUpdate: independent rattle\n(random seeds, ±30% freq)
    end

    ES->>ES: elapsed ≥ duration → Stop()\nrestore Player.localPosition
    SO->>SO: LateUpdate: IsShaking=false → snap to rest
```

---

## 8. Event Architecture

All cross-system communication uses Unity `UnityEvent` or the typed `GazeInteractableEvent` / `HazardProgressEvent` wrappers. No `GetComponent` calls happen at event-dispatch time — all listeners are registered once in `Start`.

```mermaid
flowchart TB
    subgraph EMITTERS["Event Emitters"]
        GI["GazeInteractable\nonFirstDiscovered\nonSelected"]
    end

    subgraph BROKER["Event Broker"]
        LM["LessonManager\nonHazardFound\nonProgress\nonAllHazardsFound\nonEarthquakeDrillStarted"]
    end

    subgraph LISTENERS["Listeners"]
        HP[HazardPopup]
        HH[HazardHud]
        ES[EarthquakeShaker]
        INS[Inspector callbacks\n(optional, inspector-wired)]
    end

    GI -- "onFirstDiscovered\n(registered in LM.Awake)" --> LM
    GI -- "onSelected\n(registered in HP.Start)" --> HP

    LM -- "onProgress" --> HH
    LM -- "onAllHazardsFound" --> HH
    LM -- "onEarthquakeDrillStarted" --> ES
    LM -- "onEarthquakeDrillStarted" --> HH
    LM -- "onHazardFound\nonAllHazardsFound\nonEarthquakeDrillStarted" --> INS
```

**Subscription lifecycle:**

| Component | Subscribes in | Unsubscribes in |
|---|---|---|
| `LessonManager` | `Awake` (to each `GazeInteractable`) | automatic (GazeInteractable destroyed = no-op) |
| `EarthquakeShaker` | `Start` | implicit (`LessonManager` destroyed = no-op) |
| `HazardPopup` | `Start` | — (popup outlives hazards) |
| `HazardHud` | `Start` | `OnDestroy` |

---

## 9. Editor Tooling

All editor scripts live under `Assets/Editor/` and are stripped from builds.

### ClassroomBuilder

**Menu:** `GazeVR ▸ Build Earthquake Classroom Scene`

Procedurally constructs the entire `EarthquakeClassroom.unity` scene from scratch:

```mermaid
flowchart TD
    A([Run Builder]) --> B[MaterialSanitizer.SanitizeAllPrefabs\nno magenta in URP]
    B --> C[EditorSceneManager.NewScene]
    C --> D[BuildLighting\nDirectional + ambient]
    D --> E[BuildRoomShell\n18×14×3.6 m box]
    E --> F[BuildManagers\nLessonManager · Attendance · CardboardStartup]
    F --> G[BuildPlayerRig\nPlayer · Camera · TPD · GazePointer · Reticle\n+ EarthquakeShaker on Player root]
    G --> H[BuildPopup\nworld-space HazardPopup canvas]
    H --> I[BuildHud\nworld-space HazardHud canvas\n+ DrillWarningPanel]
    I --> J[BuildClassroomAndCharacters\n12 desks · 12 chairs + ShakeableObject\nteacher · board · students]
    J --> K[BuildHazards\n8 hazards with GazeInteractable\n+ ShakeableObject on rattleable props]
    K --> L[Save scene · register as Build Scene 0]
```

### MaterialSanitizer

**Menu:** `GazeVR ▸ Fix Materials (No Magenta)`

Scans all prefabs under `Assets/Environments` and `Assets/Characters` and repairs materials that would render as magenta under URP:

- **Null slot** → replaced with a shared `URP_Fallback.mat`
- **Standalone `.mat` asset** → shader upgraded in-place to `Universal Render Pipeline/Lit` (colour and texture preserved)
- **Embedded FBX material** → a URP copy is generated in `Assets/_GazeVR/Materials/` (the original FBX is read-only)

### AndroidCardboardSetup

**Menu:** `GazeVR ▸ Configure Android + Cardboard Player Settings`

One-click player settings alignment for a Cardboard Google Play submission:

| Setting | Value | Reason |
|---|---|---|
| Color Space | Linear | Required by URP |
| Graphics API | OpenGLES3 | Cardboard minimum |
| Scripting Backend | IL2CPP | Google Play requirement |
| Architecture | ARM64 | Google Play 64-bit requirement |
| Min SDK | API 25 | Cardboard SDK minimum |
| Orientation | Landscape Left | Cardboard stereo split-screen |

---

## 10. Platform & Build Configuration

### Render Pipeline

Two URP pipeline assets exist for quality targeting:

| Asset | Target | Notes |
|---|---|---|
| `PC_RPAsset` + `PC_Renderer` | Desktop / Editor | Higher quality, shadow distance |
| `Mobile_RPAsset` + `Mobile_Renderer` | Android / Cardboard | Optimised for mobile GPU |

### Input System

The project uses the **new Input System** (`com.unity.inputsystem 1.19.0`) exclusively. The action asset `InputSystem_Actions.inputactions` defines two action maps:

- **Player**: Move, Look, Jump, Sprint, Interact (Hold), Attack, Previous, Next
- **UI**: Navigate, Submit, Cancel, Point, Click, ScrollWheel, TrackedDevicePosition / Orientation

Control schemes include `Keyboard&Mouse`, `Gamepad`, `Touch`, and **`XR`** (with `<XRController>` bindings). `GazePointer` reads directly from `Mouse.current` / `Keyboard.current` / `Touchscreen.current` rather than the action asset for simplicity, since gaze input has no rebinding requirement.

### Namespace

All runtime scripts live in the `GazeVR` namespace. Editor tools live in `GazeVR.EditorTools`. This prevents accidental name collisions with Unity built-ins and third-party packages.

### Code Folder Layout

```
Assets/
├── Editor/
│   ├── AndroidCardboardSetup.cs   ← build tooling
│   ├── ClassroomBuilder.cs        ← scene generator
│   └── MaterialSanitizer.cs       ← URP material fixer
├── Scripts/
│   ├── HazardSeverity.cs          ← shared enum
│   ├── Cardboard/
│   │   └── CardboardStartup.cs    ← XR platform init
│   ├── Game/
│   │   ├── ClassroomAttendance.cs ← headcount data
│   │   ├── EarthquakeShaker.cs    ← player-rig shake
│   │   ├── LessonManager.cs       ← lesson coordinator
│   │   └── ShakeableObject.cs     ← prop rattle
│   ├── Gaze/
│   │   ├── GazeInteractable.cs    ← selectable prop
│   │   └── GazePointer.cs         ← gaze raycast + reticle
│   └── UI/
│       ├── HazardHud.cs           ← camera-locked HUD
│       └── HazardPopup.cs         ← world-space info card
├── Scenes/
│   └── EarthquakeClassroom.unity
└── Settings/
    ├── PC_RPAsset.asset
    ├── Mobile_RPAsset.asset
    └── ...
```
