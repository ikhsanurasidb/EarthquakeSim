using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// End-of-drill summary panel. Shown automatically when <see cref="LessonManager"/> fires
    /// <see cref="LessonManager.onGameFinished"/>. Displays a score, a colour-coded list of
    /// every discovered item with its description and recommended action, and a replay prompt.
    ///
    /// The panel is a world-space canvas placed 2.5 m in front of the player and always faces them.
    /// </summary>
    public class SummaryPanel : MonoBehaviour
    {
        [Header("References")]
        public GameObject panel;
        public Text titleText;
        public Text scoreText;
        public Text itemListText;
        public Text replayText;

        [Header("Placement")]
        public float distance = 2.5f;

        // ── Severity colours (hex strings for Unity rich-text) ───────────────
        const string ColDanger = "#E05555";
        const string ColCaution = "#E0A030";
        const string ColSafe = "#44BB66";
        const string ColAction = "#88AAEE";
        const string ColMuted = "#AAAAAA";

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
            if (lesson != null) lesson.onGameFinished.AddListener(Show);
        }

        void OnDestroy()
        {
            var lesson = LessonManager.Instance;
            if (lesson != null) lesson.onGameFinished.RemoveListener(Show);
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void Show(float score)
        {
            ResolveCamera();
            PlaceInFront();

            var lesson = LessonManager.Instance;
            int pct = lesson != null ? Mathf.RoundToInt(score * 100f) : 0;

            WriteTitle();
            WriteScore(lesson, pct);
            WriteItemList(lesson);
            WriteReplay(pct);

            if (panel != null) panel.SetActive(true);
            _visible = true;
        }

        // ── Section builders ─────────────────────────────────────────────────

        void WriteTitle()
        {
            if (titleText == null) return;
            titleText.text = "Lesson Complete";
        }

        void WriteScore(LessonManager lesson, int pct)
        {
            if (scoreText == null || lesson == null) return;

            string coverPart = lesson.DidTakeCover
                ? $"<color={ColSafe}>\u2713 Took cover</color>"
                : $"<color={ColDanger}>\u2717 No cover taken  <color={ColMuted}>(-20 pts)</color></color>";

            scoreText.text =
                $"<size=46><b>Score:  {pct}%</b></size>\n" +
                $"<color={ColMuted}>Discoveries: </color>" +
                $"<b>{lesson.FoundCategories} / {lesson.TotalCategories}</b>" +
                $"     {coverPart}";
        }

        void WriteItemList(LessonManager lesson)
        {
            if (itemListText == null || lesson == null) return;

            var found = lesson.GetFoundItems();
            if (found.Count == 0)
            {
                itemListText.text =
                    $"<color={ColMuted}><i>No items discovered this round.</i></color>";
                return;
            }

            var sb = new StringBuilder();
            foreach (var item in found)
            {
                (string col, string label) = item.severity switch
                {
                    ItemSeverity.Danger => (ColDanger, "DANGER"),
                    ItemSeverity.Caution => (ColCaution, "CAUTION"),
                    _ => (ColSafe, "SAFE")
                };

                // Badge + name
                sb.Append($"<color={col}><b>[{label}]</b></color>  <b>{item.displayName}</b>\n");

                // Description
                sb.Append($"<color={ColMuted}>{item.description}</color>\n");

                // Recommended action (highlighted)
                sb.Append($"<color={ColAction}>\u2192 {item.recommendedAction}</color>\n");

                // Blank separator
                sb.Append('\n');
            }

            itemListText.text = sb.ToString();
        }

        void WriteReplay(int pct)
        {
            if (replayText == null) return;
            replayText.text = pct >= 100
                ? $"<color=#FFDD44><b>\u2605  Perfect score — you found everything and took cover!  \u2605</b></color>"
                : $"<color={ColAction}><i>Play again to discover more items and earn a perfect score.</i></color>";
        }

        // ── LateUpdate / placement ────────────────────────────────────────────

        void LateUpdate()
        {
            if (!_visible || _cam == null) return;
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
    }
}
