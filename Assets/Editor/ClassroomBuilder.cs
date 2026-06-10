using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;

namespace GazeVR.EditorTools
{
    /// <summary>
    /// One-click builder for the gaze-based earthquake-awareness classroom scene.
    /// Run <b>GazeVR ▸ Build Earthquake Classroom Scene</b> and it will:
    ///   1. sanitize all materials (no magenta),
    ///   2. create a fresh scene with lighting and a big classroom shell,
    ///   3. place desks, chairs, a teacher and a 50%+ attendance of students,
    ///   4. build the player rig: camera + TrackedPoseDriver + gaze reticle + pointer + EarthquakeShaker,
    ///   5. create the world-space hazard popup and HUD (with earthquake warning panel),
    ///   6. tag selected props as gaze hazards with educational descriptions,
    ///   7. attach ShakeableObject to furniture and rattleable hazard props,
    ///   8. save the scene and register it as build scene 0.
    ///
    /// The scene is built and saved via this editor menu (not RunCommand), so the result persists.
    /// </summary>
    public static class ClassroomBuilder
    {
        // Prefab roots.
        const string Props = "Assets/Environments/School/Prefabs/props/";
        const string Chars = "Assets/Characters/LowPoly/Prefabs/";
        const string ScenePath = "Assets/Scenes/EarthquakeClassroom.unity";

        // Room dimensions (meters).
        const float W = 18f;   // width  (x: -9 .. 9)
        const float D = 14f;   // depth  (z: -7 .. 7), front (board/teacher) at +z
        const float H = 3.6f;  // height

        static readonly string[] StudentVariants =
        {
            "male01_1", "male02_1", "male03_2", "male01_2", "male02_3", "male03_3",
            "male01_3", "male02_2", "male03_1",
        };

        [MenuItem("GazeVR/Build Earthquake Classroom Scene")]
        public static void Build()
        {
            // 1. Make sure no prop will render magenta.
            MaterialSanitizer.SanitizeAllPrefabs(out _, out _, out _);

            // 2. Fresh empty scene.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildRoomShell();
            var lesson = BuildManagers(out ClassroomAttendance attendance);
            Transform cam = BuildPlayerRig();
            // EarthquakeShaker lives on the Player root (parent of the camera) so the
            // TrackedPoseDriver/GazePointer on the camera are unaffected by the shake.
            cam.parent.gameObject.AddComponent<EarthquakeShaker>();
            HazardPopup popup = BuildPopup(cam);
            BuildHud(cam);

            int totalSeats = BuildClassroomAndCharacters(out int presentStudents);
            attendance.present = presentStudents;
            attendance.total = totalSeats;

            BuildHazards();

            // 3. Save and register the scene.
            MaterialSanitizer.EnsureFolder("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterBuildScene(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ClassroomBuilder] Built '{ScenePath}'. Students present {presentStudents}/{totalSeats} " +
                      $"({(totalSeats > 0 ? 100f * presentStudents / totalSeats : 0f):0}%). " +
                      "Open it and press Play (hold right-mouse to look, left-click/space to select).");
        }

        // ---------------------------------------------------------------- Lighting

        static void BuildLighting()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -32f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
            RenderSettings.ambientIntensity = 1f;
        }

        // ---------------------------------------------------------------- Room shell

        static void BuildRoomShell()
        {
            var root = new GameObject("Classroom").transform;

            Material floorMat = Mat("Floor", new Color(0.74f, 0.70f, 0.62f));
            Material wallMat = Mat("Wall", new Color(0.86f, 0.87f, 0.84f));
            Material ceilMat = Mat("Ceiling", new Color(0.93f, 0.93f, 0.93f));

            // Floor (Plane is 10x10 m at scale 1).
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root, false);
            floor.transform.localScale = new Vector3(W / 10f, 1f, D / 10f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Walls + ceiling (cubes carry box colliders so they block the gaze ray).
            Box("Wall_Front", root, new Vector3(0, H / 2f, D / 2f), new Vector3(W, H, 0.2f), wallMat);
            Box("Wall_Back", root, new Vector3(0, H / 2f, -D / 2f), new Vector3(W, H, 0.2f), wallMat);
            Box("Wall_Left", root, new Vector3(-W / 2f, H / 2f, 0), new Vector3(0.2f, H, D), wallMat);
            Box("Wall_Right", root, new Vector3(W / 2f, H / 2f, 0), new Vector3(0.2f, H, D), wallMat);
            Box("Ceiling", root, new Vector3(0, H, 0), new Vector3(W, 0.2f, D), ceilMat);
        }

        // ---------------------------------------------------------------- Managers

        static LessonManager BuildManagers(out ClassroomAttendance attendance)
        {
            var go = new GameObject("LessonManager");
            attendance = go.AddComponent<ClassroomAttendance>();
            attendance.passRatio = 0.5f;

            var lesson = go.AddComponent<LessonManager>();
            lesson.attendance = attendance;
            lesson.autoRegisterSceneHazards = true;

            go.AddComponent<CardboardStartup>();
            return lesson;
        }

        // ---------------------------------------------------------------- Player rig

        static Transform BuildPlayerRig()
        {
            var player = new GameObject("Player").transform;
            player.position = new Vector3(0f, 0f, -6f); // seated at the back, facing the front (+z)

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f); // eye height

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.85f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            camGo.AddComponent<AudioListener>();

            // Head tracking: TrackedPoseDriver rotates the camera with the phone (Cardboard 3-DOF).
            // RotationOnly keeps our 1.6 m eye height instead of snapping to the HMD origin.
            var tpd = camGo.AddComponent<TrackedPoseDriver>();
            tpd.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            var pointer = camGo.AddComponent<GazePointer>();

            // Gaze reticle (double-sided, draws on top).
            var reticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reticle.name = "GazeReticle";
            Object.DestroyImmediate(reticle.GetComponent<Collider>());
            reticle.transform.SetParent(camGo.transform, false);
            reticle.transform.localPosition = new Vector3(0f, 0f, 6f);
            reticle.transform.localScale = Vector3.one * 0.1f;
            reticle.GetComponent<Renderer>().sharedMaterial = ReticleMaterial();
            pointer.reticle = reticle.transform;

            return camGo.transform;
        }

        // ---------------------------------------------------------------- UI

        static HazardPopup BuildPopup(Transform cam)
        {
            var go = new GameObject("HazardPopup", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(820, 560);
            go.transform.localScale = Vector3.one * 0.0019f;
            go.transform.position = cam.position + new Vector3(0, 0, 2.2f);

            var popup = go.AddComponent<HazardPopup>();
            popup.follow = cam;

            // Panel (toggled visual content).
            var panel = UINode("Panel", go.transform);
            Stretch(panel);
            panel.AddComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.96f);

            // Header bar (colored by severity at runtime).
            var header = UINode("Header", panel.transform);
            Anchor(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 110), new Vector2(0, -55));
            var headerImg = header.AddComponent<Image>();
            headerImg.color = new Color(0.83f, 0.18f, 0.18f);

            var headerText = UIText("HeaderText", header.transform, 44, TextAnchor.MiddleLeft, Color.white);
            Stretch(headerText.gameObject, 28, 28, 8, 8);
            headerText.fontStyle = FontStyle.Bold;
            headerText.text = "Hazard";

            var bodyText = UIText("BodyText", panel.transform, 32, TextAnchor.UpperLeft,
                                  new Color(0.92f, 0.92f, 0.92f));
            Anchor(bodyText.gameObject, new Vector2(0, 0), new Vector2(1, 1),
                   new Vector2(-56, -150), new Vector2(0, -55));
            bodyText.text = "Description";

            popup.panel = panel;
            popup.headerBackground = headerImg;
            popup.headerText = headerText;
            popup.bodyText = bodyText;
            panel.SetActive(false); // hidden in the saved scene; shown at runtime on selection
            return popup;
        }

        static void BuildHud(Transform cam)
        {
            // HUD lives on the camera (world-space, so it renders in stereo VR).
            var go = new GameObject("HazardHUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.transform.SetParent(cam, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1100, 760);
            go.transform.localScale = Vector3.one * 0.0014f;
            go.transform.localPosition = new Vector3(0f, -0.05f, 1.6f);

            var hud = go.AddComponent<HazardHud>();

            var counter = UIText("Counter", go.transform, 46, TextAnchor.LowerCenter,
                                 new Color(1f, 1f, 0.85f));
            Anchor(counter.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 90), new Vector2(0, 30));
            counter.fontStyle = FontStyle.Bold;
            counter.text = "Hazards found: 0/0";
            AddOutline(counter.gameObject);

            // Completion banner (hidden until all hazards are found) – prominent, upper-center.
            var banner = UINode("CompletionBanner", go.transform);
            Anchor(banner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(950, 170), new Vector2(0, 120));
            banner.AddComponent<Image>().color = new Color(0.12f, 0.45f, 0.20f, 0.95f);
            var bannerText = UIText("CompletionText", banner.transform, 36, TextAnchor.MiddleCenter, Color.white);
            Stretch(bannerText.gameObject, 20, 20, 12, 12);
            bannerText.fontStyle = FontStyle.Bold;

            hud.counterText = counter;
            hud.completionBanner = banner;
            hud.completionText = bannerText;
            banner.SetActive(false);

            // Earthquake drill warning (hidden until the drill starts).
            var drillPanel = UINode("DrillWarningPanel", go.transform);
            Anchor(drillPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                   new Vector2(950, 210), new Vector2(0, 20));
            drillPanel.AddComponent<Image>().color = new Color(0.78f, 0.08f, 0.08f, 0.97f);
            var drillText = UIText("DrillWarningText", drillPanel.transform,
                                   46, TextAnchor.MiddleCenter, Color.white);
            Stretch(drillText.gameObject, 20, 20, 14, 14);
            drillText.fontStyle = FontStyle.Bold;
            AddOutline(drillText.gameObject);

            hud.drillWarningPanel = drillPanel;
            hud.drillWarningText = drillText;
            drillPanel.SetActive(false);
        }

        // ---------------------------------------------------------------- Furniture & people

        /// <summary>Places desks, chairs, the teacher and students. Returns total seats; outputs present count.</summary>
        static int BuildClassroomAndCharacters(out int present)
        {
            var furniture = new GameObject("Furniture").transform;
            var people = new GameObject("People").transform;

            float[] cols = { -4.5f, -1.5f, 1.5f, 4.5f };
            float[] rows = { -3.0f, -0.5f, 2.0f }; // desk z per row (front of class is +z)

            int totalSeats = cols.Length * rows.Length;
            int presentTarget = Mathf.CeilToInt(totalSeats * 0.67f); // ~2/3 attend (>= 50%)
            present = 0;
            int seat = 0;

            foreach (float rz in rows)
            {
                foreach (float cx in cols)
                {
                    // Desk faces the student; chair and student sit behind it (lower z).
                    var desk = Place(Props + "table2.prefab", furniture, new Vector3(cx, 0, rz), 0f);
                    var chair = Place(Props + "chair.prefab", furniture, new Vector3(cx, 0, rz - 0.6f), 0f);
                    if (desk != null) desk.AddComponent<ShakeableObject>();
                    if (chair != null) chair.AddComponent<ShakeableObject>();

                    if (present < presentTarget)
                    {
                        string variant = StudentVariants[seat % StudentVariants.Length];
                        Place(Chars + variant + ".prefab", people, new Vector3(cx, 0, rz - 0.95f), 0f);
                        present++;
                    }
                    seat++;
                }
            }

            // Teacher at the front, facing the students.
            Place(Props + "table1.prefab", furniture, new Vector3(0f, 0f, 5.2f), 180f);
            Place(Chars + "male03_1.prefab", people, new Vector3(0f, 0f, 6.0f), 180f);

            // Teaching board on the front wall.
            var board = Place(Props + "board.prefab", furniture, new Vector3(0f, 1.6f, D / 2f - 0.2f), 180f, ground: false);
            if (board != null) board.name = "Board";

            // Teacher's desk can also rattle.
            // (already captured above as the table1 Place call — tag furniture children)
            foreach (Transform child in furniture)
            {
                if (child.GetComponent<ShakeableObject>() == null)
                    child.gameObject.AddComponent<ShakeableObject>();
            }

            return totalSeats;
        }

        // ---------------------------------------------------------------- Hazards

        static void BuildHazards()
        {
            var root = new GameObject("Hazards").transform;

            // Tall lockers along the left wall – topple hazard.
            var locker1 = Place(Props + "locker.prefab", root, new Vector3(-8.4f, 0, 1.0f), 90f);
            var locker2 = Place(Props + "locker.prefab", root, new Vector3(-8.4f, 0, 2.6f), 90f);
            var locker = Place(Props + "locker.prefab", root, new Vector3(-8.4f, 0, 4.2f), 90f);
            MakeHazard(locker, "Tall Lockers", HazardSeverity.Danger,
                "Tall, heavy lockers can topple over and block exits during strong shaking.",
                "Stay clear of tall furniture. Never stand or shelter right beside it.");
            if (locker1 != null) locker1.AddComponent<ShakeableObject>();
            if (locker2 != null) locker2.AddComponent<ShakeableObject>();
            if (locker != null) locker.AddComponent<ShakeableObject>();

            // Bookshelf / storage rack along the right wall – falling objects.
            var rack = Place(Props + "rack.prefab", root, new Vector3(8.3f, 0, 3.5f), -90f);
            MakeHazard(rack, "Storage Rack", HazardSeverity.Caution,
                "Items stored up high on open racks can fall and hit you.",
                "Keep heavy items low. Move away from shelves when shaking starts.");
            if (rack != null) rack.AddComponent<ShakeableObject>();

            // Glass display cabinet – broken glass.
            var showcase = Place(Props + "showcase.prefab", root, new Vector3(8.3f, 0, -1.0f), -90f);
            MakeHazard(showcase, "Glass Display Cabinet", HazardSeverity.Danger,
                "Glass cabinets can shatter and scatter sharp shards across the floor.",
                "Keep your distance from glass. Wear shoes and avoid the broken area afterwards.");
            if (showcase != null) showcase.AddComponent<ShakeableObject>();

            // Window blinds – glass hazard (wall-mounted).
            var window = Place(Props + "jalousie.prefab", root, new Vector3(-8.8f, 1.7f, -2.5f), 90f, ground: false);
            MakeHazard(window, "Window", HazardSeverity.Caution,
                "Windows can crack and break, sending glass inward.",
                "Stay back from windows. Move toward an interior wall.");

            // Ceiling projector – falling fixture.
            var projector = Place(Props + "projector.prefab", root, new Vector3(0f, H - 0.35f, 0f), 0f, ground: false);
            MakeHazard(projector, "Ceiling Projector", HazardSeverity.Danger,
                "Ceiling-mounted equipment can shake loose and fall straight down.",
                "Do not stand directly underneath. Get under a sturdy desk instead.");
            if (projector != null) projector.AddComponent<ShakeableObject>();

            // Wall speaker – caution.
            var speaker = Place(Props + "speaker.prefab", root, new Vector3(-8.7f, 2.8f, 5.5f), 90f, ground: false);
            MakeHazard(speaker, "Wall Speaker", HazardSeverity.Caution,
                "Mounted speakers and fixtures can drop from the wall.",
                "Avoid lingering directly below wall-mounted objects.");
            if (speaker != null) speaker.AddComponent<ShakeableObject>();

            // Exit door – safe evacuation route.
            var door = Place(Props + "a door.prefab", root, new Vector3(6f, 0, -D / 2f + 0.2f), 0f);
            MakeHazard(door, "Exit Door", HazardSeverity.Safe,
                "This is your evacuation route. Doorways can jam, so know where it is.",
                "Do NOT run during shaking. Calmly evacuate through here once it stops.");

            // A sturdy student desk – the safe Drop-Cover-Hold spot, right in front of the player.
            var safeDesk = Place(Props + "table2.prefab", root, new Vector3(0f, 0, -4.4f), 0f);
            if (safeDesk != null) safeDesk.name = "SturdyDesk";
            MakeHazard(safeDesk, "Sturdy Desk", HazardSeverity.Safe,
                "A strong desk is the best shelter from falling objects.",
                "DROP, COVER and HOLD ON: get under it and hold a leg until shaking stops.");
            if (safeDesk != null) safeDesk.AddComponent<ShakeableObject>();
        }

        // ---------------------------------------------------------------- Helpers

        static GameObject Place(string prefabPath, Transform parent, Vector3 pos, float yaw,
                                bool ground = true, float scaleMul = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[ClassroomBuilder] Missing prefab: {prefabPath}");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (!Mathf.Approximately(scaleMul, 1f)) go.transform.localScale *= scaleMul;
            if (ground) GroundTo(go, 0f);
            return go;
        }

        static GazeInteractable MakeHazard(GameObject go, string name, HazardSeverity severity,
                                           string description, string action)
        {
            if (go == null) return null;
            EnsureCollider(go);

            var gi = go.GetComponent<GazeInteractable>();
            if (gi == null) gi = go.AddComponent<GazeInteractable>();
            gi.displayName = name;
            gi.severity = severity;
            gi.description = description;
            gi.recommendedAction = action;
            gi.countsTowardLesson = true;
            return gi;
        }

        static void GroundTo(GameObject go, float floorY)
        {
            if (!TryGetBounds(go, out Bounds b)) return;
            float dy = floorY - b.min.y;
            go.transform.position += new Vector3(0f, dy, 0f);
        }

        static void EnsureCollider(GameObject go)
        {
            if (go.GetComponentInChildren<Collider>() != null) return;

            var bc = go.AddComponent<BoxCollider>();
            if (TryGetBounds(go, out Bounds b))
            {
                bc.center = go.transform.InverseTransformPoint(b.center);
                Vector3 s = go.transform.lossyScale;
                bc.size = new Vector3(
                    b.size.x / Mathf.Max(0.0001f, Mathf.Abs(s.x)),
                    b.size.y / Mathf.Max(0.0001f, Mathf.Abs(s.y)),
                    b.size.z / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
            }
        }

        static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = new Bounds(go.transform.position, Vector3.zero);
                return false;
            }
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        static Material Mat(string name, Color color)
        {
            MaterialSanitizer.EnsureFolder("Assets/_GazeVR/Materials");
            string path = $"Assets/_GazeVR/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(MaterialSanitizer.UrpLit) { name = name };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        static Material ReticleMaterial()
        {
            MaterialSanitizer.EnsureFolder("Assets/_GazeVR/Materials");
            const string path = "Assets/_GazeVR/Materials/GazeReticle.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader s = Shader.Find("GazeVR/ReticleOverlay");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(s) { name = "GazeReticle" };
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.25f, 0.95f, 1f, 1f));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.25f, 0.95f, 1f, 1f));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        static void RegisterBuildScene(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(path, true));
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path != path) scenes.Add(s);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---- tiny uGUI builders ----

        static GameObject UINode(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static Text UIText(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            var go = UINode(name, parent);
            var t = go.AddComponent<Text>();
            t.font = BuiltinFont();
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void AddOutline(GameObject go)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.85f);
            o.effectDistance = new Vector2(2, -2);
        }

        static Font BuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        static RectTransform RT(GameObject go) => (RectTransform)go.transform;

        static void Stretch(GameObject go, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            var rt = RT(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static void Anchor(GameObject go, Vector2 min, Vector2 max, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            var rt = RT(go);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
        }

        // ---------------------------------------------------------------- Box primitive

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
