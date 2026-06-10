using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// End-of-drill summary panel. Shown automatically when <see cref="LessonManager"/> fires
    /// <see cref="LessonManager.onGameFinished"/>.
    ///
    /// Displays every discovered item with its full description and recommended action, the
    /// final score, and a replay recommendation if the score is below 100 %.
    ///
    /// The panel is a world-space canvas placed in front of the player when shown so it is
    /// readable in both flat-screen and VR modes.
    /// </summary>
    public class SummaryPanel : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Root GameObject that is toggled to show/hide the panel.")]
        public GameObject panel;
        public Text titleText;
        public Text scoreText;
        public Text itemListText;
        public Text replayText;

        [Header("Placement")]
        [Tooltip("Distance in front of the player, in metres.")]
        public float distance = 2.5f;

        Transform _cam;
        bool _visible;

        void Awake()
        {
            ResolveCamera();
            if (panel != null) panel.SetActive(false);
        }

        void Start()
        {
            var lesson = LessonManager.Instance;
            if (lesson != null)
                lesson.onGameFinished.AddListener(Show);
        }

        void OnDestroy()
        {
            var lesson = LessonManager.Instance;
            if (lesson != null)
                lesson.onGameFinished.RemoveListener(Show);
        }

        /// <summary>Shows the summary for the given normalised score (0–1).</summary>
        public void Show(float score)
        {
            ResolveCamera();
            PlaceInFront();

            var lesson = LessonManager.Instance;
            int pct = lesson != null ? Mathf.RoundToInt(score * 100f) : 0;

            // ── Title ─────────────────────────────────────────────────────────
            if (titleText != null)
                titleText.text = "Lesson Complete!";

            // ── Score ─────────────────────────────────────────────────────────
            if (scoreText != null && lesson != null)
            {
                string coverLine = lesson.DidTakeCover
                    ? "\u2713 You took cover under a desk"
                    : "\u2717 You did not take cover";

                scoreText.text =
                    $"Score: {pct}%\n" +
                    $"Discoveries: {lesson.FoundCategories}/{lesson.TotalCategories}\n" +
                    coverLine;
            }

            // ── Item list ─────────────────────────────────────────────────────
            if (itemListText != null && lesson != null)
            {
                var found = lesson.GetFoundItems();
                var sb = new StringBuilder();
                foreach (var item in found)
                {
                    string badge = SeverityBadge(item.severity);
                    sb.AppendLine($"<b>{item.displayName}</b>  {badge}");
                    sb.AppendLine(item.description);
                    sb.AppendLine($"<i>What to do: {item.recommendedAction}</i>");
                    sb.AppendLine();
                }
                itemListText.text = sb.ToString();
            }

            // ── Replay prompt ─────────────────────────────────────────────────
            if (replayText != null)
            {
                replayText.text = pct >= 100
                    ? "Excellent! Perfect score — you found everything and took cover!"
                    : "Play again to discover more items and earn a perfect score!";
            }

            if (panel != null) panel.SetActive(true);
            _visible = true;
        }

        void LateUpdate()
        {
            if (!_visible || _cam == null) return;
            // Always face the player.
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.position, Vector3.up);
        }

        void PlaceInFront()
        {
            if (_cam == null) return;
            Vector3 fwd = _cam.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();
            transform.position = _cam.position + fwd * distance;
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.position, Vector3.up);
        }

        void ResolveCamera()
        {
            if (_cam != null) return;
            if (Camera.main != null) _cam = Camera.main.transform;
        }

        static string SeverityBadge(ItemSeverity s) => s switch
        {
            ItemSeverity.Danger => "[DANGER]",
            ItemSeverity.Caution => "[CAUTION]",
            _ => "[SAFE]",
        };
    }
}
