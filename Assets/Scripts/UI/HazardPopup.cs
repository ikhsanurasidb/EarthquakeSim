using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// World-space popup that shows a selected hazard's name, severity, description and recommended
    /// action. It places itself in front of the player when shown and continuously turns to face
    /// them. World space is used on purpose: a screen-space overlay does not render correctly in
    /// stereo VR.
    ///
    /// The root object stays active (so this script keeps running); only the <see cref="panel"/>
    /// child is toggled to show/hide. On Start it subscribes to every hazard's onSelected event, so
    /// no manual wiring is required.
    /// </summary>
    public class HazardPopup : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The visual content that is shown/hidden (kept separate from this active root).")]
        public GameObject panel;
        public Image headerBackground;
        public Text headerText;   // "<name> — SEVERITY"
        public Text bodyText;     // description + recommended action

        [Tooltip("Transform the popup orients toward (the player's camera). Defaults to Camera.main.")]
        public Transform follow;

        [Header("Behaviour")]
        [Tooltip("Automatically show this popup for every GazeInteractable selected in the scene.")]
        public bool autoSubscribeAllHazards = true;

        [Header("Placement")]
        [Tooltip("Distance in front of the player, in meters.")]
        public float distance = 2.2f;
        public float verticalOffset = -0.15f;

        [Header("Severity colors")]
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
            if (autoSubscribeAllHazards)
            {
                foreach (var gi in FindObjectsByType<GazeInteractable>(FindObjectsInactive.Exclude))
                {
                    gi.onSelected.AddListener(Show);
                }
            }
            Hide();
        }

        /// <summary>Populates and shows the popup for the given hazard.</summary>
        public void Show(GazeInteractable hazard)
        {
            if (hazard == null) return;
            ResolveCamera();

            if (headerText != null)
                headerText.text = $"{hazard.displayName}  —  {SeverityLabel(hazard.severity)}";

            if (bodyText != null)
                bodyText.text = $"{hazard.description}\n\n<b>What to do:</b> {hazard.recommendedAction}";

            if (headerBackground != null)
                headerBackground.color = SeverityColor(hazard.severity);

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
            if (!_shown || _cam == null) return;
            // Always face the player.
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.position, Vector3.up);
        }

        void PlaceInFront()
        {
            if (_cam == null) return;

            Vector3 forward = _cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            transform.position = _cam.position + forward * distance + Vector3.up * verticalOffset;
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.position, Vector3.up);
        }

        void ResolveCamera()
        {
            if (_cam != null) return;
            if (follow != null) _cam = follow;
            else if (Camera.main != null) _cam = Camera.main.transform;
        }

        static string SeverityLabel(HazardSeverity s) => s switch
        {
            HazardSeverity.Danger => "DANGER",
            HazardSeverity.Caution => "CAUTION",
            _ => "SAFE",
        };

        Color SeverityColor(HazardSeverity s) => s switch
        {
            HazardSeverity.Danger => dangerColor,
            HazardSeverity.Caution => cautionColor,
            _ => safeColor,
        };
    }
}
