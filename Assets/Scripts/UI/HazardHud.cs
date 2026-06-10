using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// Heads-up display: shows hazard-found progress, a completion banner when all hazards are
    /// identified, and an earthquake warning banner when the drill starts.
    ///
    /// Lives on a world-space canvas parented to the camera so it renders correctly in stereo VR.
    /// </summary>
    public class HazardHud : MonoBehaviour
    {
        [Header("Hazard counter")]
        public Text counterText;

        [Header("Completion banner (all hazards found)")]
        public GameObject completionBanner;
        public Text completionText;

        [Header("Earthquake warning (drill started)")]
        public GameObject drillWarningPanel;
        public Text drillWarningText;

        [Header("Content")]
        [TextArea]
        public string completionMessage =
            "All hazards identified!\nGet ready — earthquake incoming!";

        [TextArea]
        public string drillWarningMessage =
            "EARTHQUAKE!\nDROP  •  COVER  •  HOLD ON";

        void Start()
        {
            if (completionBanner != null) completionBanner.SetActive(false);
            if (drillWarningPanel != null) drillWarningPanel.SetActive(false);

            var lesson = LessonManager.Instance;
            if (lesson != null)
            {
                lesson.onProgress.AddListener(OnProgress);
                lesson.onAllHazardsFound.AddListener(OnAllFound);
                lesson.onEarthquakeDrillStarted.AddListener(OnDrillStarted);
                OnProgress(lesson.FoundHazards, lesson.TotalHazards);
            }
            else if (counterText != null)
            {
                counterText.text = "Hazards found: 0/0";
            }
        }

        void OnDestroy()
        {
            var lesson = LessonManager.Instance;
            if (lesson == null) return;
            lesson.onProgress.RemoveListener(OnProgress);
            lesson.onAllHazardsFound.RemoveListener(OnAllFound);
            lesson.onEarthquakeDrillStarted.RemoveListener(OnDrillStarted);
        }

        void OnProgress(int found, int total)
        {
            if (counterText != null)
                counterText.text = $"Hazards found: {found}/{total}";
        }

        void OnAllFound()
        {
            if (completionText != null) completionText.text = completionMessage;
            if (completionBanner != null) completionBanner.SetActive(true);
        }

        void OnDrillStarted()
        {
            if (drillWarningText != null) drillWarningText.text = drillWarningMessage;
            if (drillWarningPanel != null) drillWarningPanel.SetActive(true);

            // The completion banner has done its job; swap it out for the warning.
            if (completionBanner != null) completionBanner.SetActive(false);
        }
    }
}
