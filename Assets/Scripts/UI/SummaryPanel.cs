using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// End-of-drill summary panel. Shown automatically when <see cref="LessonManager"/> fires
    /// <see cref="LessonManager.onGameFinished"/>. Displays a score, a colour-coded list of
    /// every discovered item with its description and recommended action, and a Play Again button.
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

        [Header("Play Again")]
        [Tooltip("GazeInteractable on the Play Again button collider (child of this canvas).")]
        public GazeInteractable playAgainInteractable;
        [Tooltip("Label text on the Play Again button.")]
        public Text playAgainButtonText;

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
            if (playAgainInteractable != null) playAgainInteractable.gameObject.SetActive(false);
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

            if (playAgainInteractable != null)
                playAgainInteractable.onSelected.RemoveListener(OnPlayAgainSelected);
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

            // Enable the Play Again button.
            if (playAgainInteractable != null)
            {
                playAgainInteractable.gameObject.SetActive(true);
                playAgainInteractable.onSelected.AddListener(OnPlayAgainSelected);
            }
            if (playAgainButtonText != null)
                playAgainButtonText.text = "Gaze here  •  PLAY AGAIN";
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

            int coverPenalty = Mathf.RoundToInt(LessonManager.CoverScoreWeight * 100f);
            string coverPart = lesson.DidTakeCover
                ? $"<color={ColSafe}>✓ Took cover</color>"
                : $"<color={ColDanger}>✗ No cover taken  <color={ColMuted}>(-{coverPenalty} pts)</color></color>";

            scoreText.text =
                $"<size=46><b>Score:  {pct}%</b></size>\n" +
                $"<color={ColMuted}>Discoveries: </color>" +
                $"<b>{lesson.FoundCategories}</b>" +
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
                sb.Append($"<color={ColAction}>→ {item.recommendedAction}</color>\n");

                // Blank separator
                sb.Append('\n');
            }

            itemListText.text = sb.ToString();
        }

        void WriteReplay(int pct)
        {
            if (replayText == null) return;
            replayText.text = pct >= 100
                ? $"<color=#FFDD44><b>★  Perfect score — you found everything and took cover!  ★</b></color>"
                : $"<color={ColAction}><i>Play again to discover more items and earn a perfect score.</i></color>";
        }

        // ── Play Again ───────────────────────────────────────────────────────

        void OnPlayAgainSelected(GazeInteractable _)
        {
            GameSettings.StartupShown = true; // skip startup menu on reload
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ── LateUpdate / placement ────────────────────────────────────────────

        void LateUpdate()
        {
            if (!_visible) return;
            WorldSpaceUI.FaceCamera(transform, _cam);
        }

        void PlaceInFront()
        {
            WorldSpaceUI.PlaceInFront(transform, _cam, distance);
        }

        void ResolveCamera()
        {
            _cam = WorldSpaceUI.ResolveCamera(_cam);
        }
    }
}
