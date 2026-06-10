using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// Heads-up display: a counter showing how many hazards have been found out of the total, plus a
    /// completion banner shown once every hazard is identified. It lives on a world-space canvas that
    /// is parented to the camera so it renders correctly in stereo VR.
    /// </summary>
    public class HazardHud : MonoBehaviour
    {
        [Header("References")]
        public Text counterText;
        public GameObject completionBanner;
        public Text completionText;

        [Header("Content")]
        [TextArea]
        public string completionMessage =
            "All hazards identified!\nStay calm and remember: Drop, Cover, Hold On.";

        void Start()
        {
            if (completionBanner != null) completionBanner.SetActive(false);

            var lesson = LessonManager.Instance;
            if (lesson != null)
            {
                lesson.onProgress.AddListener(OnProgress);
                lesson.onAllHazardsFound.AddListener(OnAllFound);
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
    }
}
