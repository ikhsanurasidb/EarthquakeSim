using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// Heads-up display camera overlay. Shows:
    /// <list type="bullet">
    ///   <item>A running count of discoveries (no total, to keep the player exploring).</item>
    ///   <item>A brief "+1 [Name]" notification each time a new category is found.</item>
    ///   <item>A green completion banner when all tracked items have been found.</item>
    ///   <item>A red earthquake warning when the drill starts.</item>
    ///   <item>A "FIND COVER!" prompt during the earthquake, replaced by "COVERED!" once taken.</item>
    ///   <item>A "SHAKING STOPPED! Evacuate!" prompt after the earthquake ends.</item>
    /// </list>
    /// </summary>
    public class HazardHud : MonoBehaviour
    {
        [Header("Discovery counter (no total shown)")]
        public Text counterText;

        [Header("New-discovery notification (+1 flash)")]
        public GameObject notifPanel;
        public Text notifText;
        [Tooltip("Seconds the +1 notification stays visible.")]
        public float notifDuration = 2.2f;

        [Header("Completion banner (all items found)")]
        public GameObject completionBanner;
        public Text completionText;

        [Header("Earthquake warning (drill started)")]
        public GameObject drillWarningPanel;
        public Text drillWarningText;

        [Header("Cover prompt (during earthquake)")]
        public GameObject coverPromptPanel;
        public Text coverPromptText;

        [Header("Evacuation prompt (after earthquake)")]
        public GameObject evacuationPromptPanel;
        public Text evacuationPromptText;

        [Header("Content")]
        [TextArea]
        public string completionMessage = "All items found!\nGet ready — earthquake incoming!";
        [TextArea]
        public string drillWarningMessage = "EARTHQUAKE!\nDROP  •  COVER  •  HOLD ON";
        public string coverPromptMessage = "FIND COVER!\nGaze at a sturdy desk";
        public string coveredMessage = "YOU'RE COVERED!\nHold on!";
        [TextArea]
        public string evacuationPromptMessage = "SHAKING STOPPED!\nGaze at the EXIT DOOR to evacuate.";

        Coroutine _notifRoutine;

        void Start()
        {
            SetActive(notifPanel, false);
            SetActive(completionBanner, false);
            SetActive(drillWarningPanel, false);
            SetActive(coverPromptPanel, false);
            SetActive(evacuationPromptPanel, false);

            var lesson = LessonManager.Instance;
            if (lesson == null)
            {
                if (counterText != null) counterText.text = "Discoveries: 0";
                return;
            }

            lesson.onProgress.AddListener(OnProgress);
            lesson.onNewCategoryFound.AddListener(OnNewCategoryFound);
            lesson.onAllItemsFound.AddListener(OnAllFound);
            lesson.onEarthquakeDrillStarted.AddListener(OnDrillStarted);
            lesson.onCoverTaken.AddListener(OnCoverTaken);
            lesson.onEvacuationStarted.AddListener(OnEvacuationStarted);
            lesson.onGameFinished.AddListener(OnGameFinished);

            OnProgress(lesson.FoundCategories, lesson.TotalCategories);
        }

        void OnDestroy()
        {
            var lesson = LessonManager.Instance;
            if (lesson == null) return;
            lesson.onProgress.RemoveListener(OnProgress);
            lesson.onNewCategoryFound.RemoveListener(OnNewCategoryFound);
            lesson.onAllItemsFound.RemoveListener(OnAllFound);
            lesson.onEarthquakeDrillStarted.RemoveListener(OnDrillStarted);
            lesson.onCoverTaken.RemoveListener(OnCoverTaken);
            lesson.onEvacuationStarted.RemoveListener(OnEvacuationStarted);
            lesson.onGameFinished.RemoveListener(OnGameFinished);
        }

        // ── Event handlers ───────────────────────────────────────────────────

        void OnProgress(int found, int total)
        {
            // Intentionally hide the total to keep the player curious.
            if (counterText != null)
                counterText.text = $"Discoveries: {found}";
        }

        void OnNewCategoryFound(GazeInteractable item)
        {
            ShowNotification($"+ {item.displayName}");
        }

        void OnAllFound()
        {
            if (completionText != null) completionText.text = completionMessage;
            SetActive(completionBanner, true);
        }

        void OnDrillStarted()
        {
            if (drillWarningText != null) drillWarningText.text = drillWarningMessage;
            SetActive(drillWarningPanel, true);
            SetActive(completionBanner, false);

            if (coverPromptText != null) coverPromptText.text = coverPromptMessage;
            SetActive(coverPromptPanel, true);
        }

        void OnCoverTaken()
        {
            if (coverPromptText != null) coverPromptText.text = coveredMessage;
            // Keep the panel visible so the player knows they succeeded.
        }

        void OnEvacuationStarted()
        {
            // Hide earthquake phase panels.
            SetActive(drillWarningPanel, false);
            SetActive(coverPromptPanel, false);

            if (evacuationPromptText != null) evacuationPromptText.text = evacuationPromptMessage;
            SetActive(evacuationPromptPanel, true);
        }

        void OnGameFinished(float _)
        {
            // Hide all HUD elements once the summary is shown.
            SetActive(evacuationPromptPanel, false);
            SetActive(drillWarningPanel, false);
            SetActive(coverPromptPanel, false);
            SetActive(completionBanner, false);
            SetActive(notifPanel, false);
        }

        // ── Notification ─────────────────────────────────────────────────────

        void ShowNotification(string text)
        {
            if (_notifRoutine != null) StopCoroutine(_notifRoutine);
            if (notifText != null) notifText.text = text;
            SetActive(notifPanel, true);
            _notifRoutine = StartCoroutine(HideNotifAfter(notifDuration));
        }

        IEnumerator HideNotifAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            SetActive(notifPanel, false);
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
