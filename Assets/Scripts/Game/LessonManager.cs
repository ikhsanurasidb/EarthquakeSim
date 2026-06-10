using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GazeVR
{
    /// <summary>Progress event carrying (found, total) hazard counts.</summary>
    [System.Serializable]
    public class HazardProgressEvent : UnityEvent<int, int> { }

    /// <summary>
    /// Central lesson logic for the earthquake-awareness drill. Registers every gaze hazard in
    /// the scene, tracks how many have been identified, raises progress / completion events, and
    /// automatically starts the earthquake drill a configurable delay after the last hazard is found.
    /// </summary>
    public class LessonManager : MonoBehaviour
    {
        /// <summary>Convenience singleton so UI can find the lesson without a serialized reference.</summary>
        public static LessonManager Instance { get; private set; }

        [Header("Attendance")]
        public ClassroomAttendance attendance;

        [Header("Auto-registration")]
        [Tooltip("If true, every GazeInteractable in the scene that counts toward the lesson is " +
                 "registered automatically on Awake.")]
        public bool autoRegisterSceneHazards = true;

        [Header("Earthquake Drill")]
        [Tooltip("Seconds to wait after the last hazard is found before the drill starts " +
                 "automatically. Set to 0 to disable auto-start (manual trigger only).")]
        public float autoStartDrillDelay = 3f;

        [Header("Events")]
        [Tooltip("Fired the first time a particular hazard is identified.")]
        public GazeInteractableEvent onHazardFound = new GazeInteractableEvent();

        [Tooltip("Fired whenever the found count changes: (found, total).")]
        public HazardProgressEvent onProgress = new HazardProgressEvent();

        [Tooltip("Fired once when every hazard has been identified.")]
        public UnityEvent onAllHazardsFound = new UnityEvent();

        [Tooltip("Fired when the earthquake drill shaking phase begins.")]
        public UnityEvent onEarthquakeDrillStarted = new UnityEvent();

        readonly List<GazeInteractable> _hazards = new List<GazeInteractable>();
        readonly HashSet<GazeInteractable> _found = new HashSet<GazeInteractable>();

        public int TotalHazards => _hazards.Count;
        public int FoundHazards => _found.Count;
        public bool DrillStarted { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (autoRegisterSceneHazards)
            {
                foreach (var gi in FindObjectsByType<GazeInteractable>(FindObjectsInactive.Exclude))
                {
                    if (gi.countsTowardLesson) Register(gi);
                }
            }
        }

        void Start()
        {
            // Let listeners (e.g. the HUD) initialize to the starting count.
            onProgress.Invoke(FoundHazards, TotalHazards);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Registers a hazard with the lesson and subscribes to its first discovery.</summary>
        public void Register(GazeInteractable hazard)
        {
            if (hazard == null || _hazards.Contains(hazard)) return;
            _hazards.Add(hazard);
            hazard.onFirstDiscovered.AddListener(HandleFirstDiscovered);
        }

        void HandleFirstDiscovered(GazeInteractable hazard)
        {
            if (!_found.Add(hazard)) return;

            onHazardFound.Invoke(hazard);
            onProgress.Invoke(FoundHazards, TotalHazards);

            if (TotalHazards > 0 && FoundHazards >= TotalHazards)
            {
                onAllHazardsFound.Invoke();

                if (autoStartDrillDelay > 0f)
                    StartCoroutine(DelayedDrillStart(autoStartDrillDelay));
            }
        }

        IEnumerator DelayedDrillStart(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            StartEarthquakeDrill();
        }

        /// <summary>
        /// Begins the earthquake drill shaking phase. If <see cref="autoStartDrillDelay"/> is
        /// greater than zero this is called automatically once all hazards have been found;
        /// it can also be triggered manually (e.g. from a UI button or a test harness).
        /// </summary>
        public void StartEarthquakeDrill()
        {
            if (DrillStarted) return;
            DrillStarted = true;

            Debug.Log("[LessonManager] Earthquake drill started.");
            onEarthquakeDrillStarted.Invoke();
        }

        // ── Attendance pass-throughs ─────────────────────────────────────────

        public bool AttendanceMeetsThreshold => attendance == null || attendance.MeetsThreshold;
        public int PresentStudents => attendance != null ? attendance.present : 0;
        public int TotalStudents => attendance != null ? attendance.total : 0;
    }
}
