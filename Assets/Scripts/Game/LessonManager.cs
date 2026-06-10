using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GazeVR
{
    /// <summary>Progress event: (categoriesFound, totalCategories).</summary>
    [System.Serializable]
    public class ItemProgressEvent : UnityEvent<int, int> { }

    /// <summary>Score event: passes the final normalised score (0–1).</summary>
    [System.Serializable]
    public class ScoreEvent : UnityEvent<float> { }

    /// <summary>Current phase of the lesson.</summary>
    public enum GamePhase { Exploring, Earthquake, Summary }

    /// <summary>
    /// Central lesson coordinator. On <c>Awake</c> it registers every
    /// <see cref="GazeInteractable"/> in the scene, groups them by
    /// <see cref="GazeInteractable.CategoryId"/>, and tracks how many unique categories
    /// the student has discovered.
    ///
    /// <para>After all tracked categories are found the earthquake drill starts
    /// automatically (after <see cref="autoStartDrillDelay"/> seconds). Once the shake
    /// ends the lesson moves to the Summary phase and broadcasts
    /// <see cref="onGameFinished"/> with the final score.</para>
    ///
    /// <para>Score formula: 80 % from category discoveries + 20 % for taking cover
    /// (points split evenly across categories for the item portion).</para>
    /// </summary>
    public class LessonManager : MonoBehaviour
    {
        public static LessonManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Attendance")]
        public ClassroomAttendance attendance;

        [Header("Auto-registration")]
        [Tooltip("Automatically register every GazeInteractable in the scene on Awake.")]
        public bool autoRegisterSceneItems = true;

        [Header("Earthquake Drill")]
        [Tooltip("Seconds after all categories are found before the drill starts. 0 = manual only.")]
        public float autoStartDrillDelay = 3f;

        [Header("Events")]
        [Tooltip("Fired every time any item is selected (including repeats).")]
        public GazeInteractableEvent onItemSelected = new GazeInteractableEvent();

        [Tooltip("Fired the first time a NEW category is discovered.")]
        public GazeInteractableEvent onNewCategoryFound = new GazeInteractableEvent();

        [Tooltip("Fired whenever the discovered-category count changes: (found, total).")]
        public ItemProgressEvent onProgress = new ItemProgressEvent();

        [Tooltip("Fired once when every tracked category has been discovered.")]
        public UnityEvent onAllItemsFound = new UnityEvent();

        [Tooltip("Fired when the earthquake drill begins.")]
        public UnityEvent onEarthquakeDrillStarted = new UnityEvent();

        [Tooltip("Fired when the player successfully takes cover.")]
        public UnityEvent onCoverTaken = new UnityEvent();

        [Tooltip("Fired when the drill ends and summary is ready. Passes the score (0–1).")]
        public ScoreEvent onGameFinished = new ScoreEvent();

        // ── Runtime state ────────────────────────────────────────────────────

        /// <summary>Current lesson phase.</summary>
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Exploring;

        public bool DrillStarted { get; private set; }
        public bool DidTakeCover { get; private set; }

        /// <summary>Total number of unique tracked categories.</summary>
        public int TotalCategories => _totalTrackedCategories;

        /// <summary>Number of unique tracked categories the student has discovered.</summary>
        public int FoundCategories => _foundCategories.Count;

        // ── Private ──────────────────────────────────────────────────────────

        // All registered items (may contain many items per category).
        readonly List<GazeInteractable> _allItems = new List<GazeInteractable>();

        // First representative per category (used for summary display).
        readonly Dictionary<string, GazeInteractable> _categoryReps
            = new Dictionary<string, GazeInteractable>();

        // Set of category IDs that have been discovered.
        readonly HashSet<string> _foundCategories = new HashSet<string>();

        int _totalTrackedCategories;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (autoRegisterSceneItems)
            {
                foreach (var gi in FindObjectsByType<GazeInteractable>(FindObjectsInactive.Exclude))
                    Register(gi);
            }

            // Count how many unique categories actually count toward the lesson.
            var tracked = new HashSet<string>();
            foreach (var item in _allItems)
            {
                if (item.countsTowardLesson) tracked.Add(item.CategoryId);
            }
            _totalTrackedCategories = tracked.Count;
        }

        void Start()
        {
            onProgress.Invoke(FoundCategories, TotalCategories);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Registers an item with the lesson.</summary>
        public void Register(GazeInteractable item)
        {
            if (item == null || _allItems.Contains(item)) return;
            _allItems.Add(item);
            item.onFirstDiscovered.AddListener(HandleFirstDiscovered);
            item.onSelected.AddListener(HandleSelected);

            // Store the first representative for each category.
            string cat = item.CategoryId;
            if (!_categoryReps.ContainsKey(cat))
                _categoryReps[cat] = item;
        }

        /// <summary>Returns the representative <see cref="GazeInteractable"/> for every
        /// category that has been found, for use in the summary screen.</summary>
        public List<GazeInteractable> GetFoundItems()
        {
            var result = new List<GazeInteractable>();
            foreach (var catId in _foundCategories)
            {
                if (_categoryReps.TryGetValue(catId, out var rep))
                    result.Add(rep);
            }
            return result;
        }

        /// <summary>Computes the final score (0–1).
        /// 80 % is earned from category discoveries; 20 % for taking cover.</summary>
        public float CalculateScore()
        {
            float itemScore = _totalTrackedCategories > 0
                ? (float)FoundCategories / _totalTrackedCategories
                : 0f;
            float coverScore = DidTakeCover ? 1f : 0f;
            return itemScore * 0.8f + coverScore * 0.2f;
        }

        /// <summary>Call when the player takes cover under a desk during the drill.</summary>
        public void TakeCover()
        {
            if (DidTakeCover || CurrentPhase != GamePhase.Earthquake) return;
            DidTakeCover = true;
            Debug.Log("[LessonManager] Player took cover.");
            onCoverTaken.Invoke();
        }

        /// <summary>Manually starts the earthquake drill (also called automatically).</summary>
        public void StartEarthquakeDrill()
        {
            if (DrillStarted) return;
            DrillStarted = true;
            CurrentPhase = GamePhase.Earthquake;
            Debug.Log("[LessonManager] Earthquake drill started.");

            // Subscribe to the shaker's completion so we can transition to Summary.
            var shaker = EarthquakeShaker.Current;
            if (shaker != null)
                shaker.onShakeComplete.AddListener(OnDrillComplete);
            else
                // No shaker in the scene — go to summary after a fallback delay.
                StartCoroutine(FallbackSummaryDelay());

            onEarthquakeDrillStarted.Invoke();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        void HandleSelected(GazeInteractable item)
        {
            onItemSelected.Invoke(item);
        }

        void HandleFirstDiscovered(GazeInteractable item)
        {
            string catId = item.CategoryId;
            bool isNewCategory = _foundCategories.Add(catId);

            if (isNewCategory)
            {
                onNewCategoryFound.Invoke(item);

                if (item.countsTowardLesson)
                {
                    onProgress.Invoke(FoundCategories, TotalCategories);

                    if (_totalTrackedCategories > 0 && FoundCategories >= _totalTrackedCategories)
                    {
                        onAllItemsFound.Invoke();
                        if (autoStartDrillDelay > 0f)
                            StartCoroutine(DelayedDrillStart(autoStartDrillDelay));
                    }
                }
            }
        }

        IEnumerator DelayedDrillStart(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            StartEarthquakeDrill();
        }

        IEnumerator FallbackSummaryDelay()
        {
            yield return new WaitForSecondsRealtime(8f);
            OnDrillComplete();
        }

        void OnDrillComplete()
        {
            if (CurrentPhase == GamePhase.Summary) return;
            CurrentPhase = GamePhase.Summary;
            float score = CalculateScore();
            Debug.Log($"[LessonManager] Drill complete. Score: {score * 100f:0}%");
            onGameFinished.Invoke(score);
        }

        // ── Attendance pass-throughs ─────────────────────────────────────────

        public bool AttendanceMeetsThreshold => attendance == null || attendance.MeetsThreshold;
        public int PresentStudents => attendance != null ? attendance.present : 0;
        public int TotalStudents => attendance != null ? attendance.total : 0;
    }
}
