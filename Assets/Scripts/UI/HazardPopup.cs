using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// World-space popup that shows a selected item's name, severity, description and recommended
    /// action. During active gameplay phases (<see cref="GamePhase.Exploring"/> and
    /// <see cref="GamePhase.Earthquake"/>) the popup is intentionally suppressed — descriptions
    /// are revealed in the <see cref="SummaryPanel"/> at the end of the drill to encourage the
    /// student to keep exploring without being immediately rewarded with information.
    ///
    /// The popup can still be shown manually (e.g. in a review/tutorial mode) by calling
    /// <see cref="Show"/> directly. Set <see cref="autoSubscribeAllItems"/> to true for that use case.
    /// </summary>
    public class HazardPopup : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The visual content that is shown/hidden (kept separate from this active root).")]
        public GameObject panel;
        public Image headerBackground;
        public Text headerText;
        public Text bodyText;

        [Tooltip("Transform the popup orients toward (the player's camera). Defaults to Camera.main.")]
        public Transform follow;

        [Header("Behaviour")]
        [Tooltip("Auto-subscribe to every GazeInteractable.onSelected in the scene.\n" +
                 "Disable during gameplay — descriptions are shown in SummaryPanel instead.")]
        public bool autoSubscribeAllItems = false;

        [Header("Placement")]
        [Tooltip("Distance in front of the player, in metres.")]
        public float distance = 2.2f;
        public float verticalOffset = -0.15f;

        [Header("Severity colours")]
        public Color dangerColor = new Color(0.83f, 0.18f, 0.18f);
        public Color cautionColor = new Color(0.95f, 0.66f, 0.13f);
        public Color safeColor = new Color(0.20f, 0.62f, 0.30f);

        Transform _cam;
        bool _shown;

        void Awake()
        {
            ResolveCamera();
        }

        void Start()
        {
            if (autoSubscribeAllItems)
            {
                foreach (var gi in FindObjectsByType<GazeInteractable>(FindObjectsInactive.Exclude))
                    gi.onSelected.AddListener(Show);
            }
            Hide();
        }

        /// <summary>
        /// Populates and shows the popup for the given item.
        /// During <see cref="GamePhase.Exploring"/> and <see cref="GamePhase.Earthquake"/> phases
        /// the call is silently ignored to preserve the exploration tension.
        /// </summary>
        public void Show(GazeInteractable item)
        {
            if (item == null) return;

            // Only show during Summary — suppress during all active gameplay phases
            // (Exploring, Earthquake, Evacuation) to keep the player focused.
            var lesson = LessonManager.Instance;
            if (lesson != null && lesson.CurrentPhase != GamePhase.Summary)
                return;

            ResolveCamera();

            if (headerText != null)
                headerText.text = $"{item.displayName}  —  {SeverityLabel(item.severity)}";

            if (bodyText != null)
                bodyText.text = $"{item.description}\n\n<b>What to do:</b> {item.recommendedAction}";

            if (headerBackground != null)
                headerBackground.color = SeverityColor(item.severity);

            if (panel != null) panel.SetActive(true);
            _shown = true;
            PlaceInFront();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            _shown = false;
        }

        void LateUpdate()
        {
            if (!_shown) return;
            WorldSpaceUI.FaceCamera(transform, _cam);
        }

        void PlaceInFront()
        {
            WorldSpaceUI.PlaceInFront(transform, _cam, distance, verticalOffset);
        }

        void ResolveCamera()
        {
            _cam = WorldSpaceUI.ResolveCamera(follow != null ? follow : _cam);
        }

        static string SeverityLabel(ItemSeverity s) => s switch
        {
            ItemSeverity.Danger => "DANGER",
            ItemSeverity.Caution => "CAUTION",
            _ => "SAFE",
        };

        Color SeverityColor(ItemSeverity s) => s switch
        {
            ItemSeverity.Danger => dangerColor,
            ItemSeverity.Caution => cautionColor,
            _ => safeColor,
        };
    }
}
