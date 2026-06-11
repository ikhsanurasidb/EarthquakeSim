using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// World-space startup menu shown the first time the app is launched (or after a domain reload
    /// in the Editor). Players can toggle recording mode (VR off) and start an optional tutorial
    /// before beginning the lesson.
    ///
    /// On a Play Again reload, <see cref="GameSettings.StartupShown"/> is already true so the menu
    /// hides itself immediately and begins the lesson without interruption.
    /// </summary>
    public class StartupMenu : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject menuPanel;

        [Header("Status labels")]
        public Text vrModeLabel;
        public Text titleLabel;

        [Header("Gaze buttons")]
        [Tooltip("Toggles VR / Recording mode.")]
        public GazeInteractable vrToggleButton;
        [Tooltip("Starts the tutorial practice sequence.")]
        public GazeInteractable tutorialButton;
        [Tooltip("Dismisses the menu and begins the lesson.")]
        public GazeInteractable startGameButton;

        [Header("Dependencies")]
        public TutorialManager tutorialManager;
        public CardboardStartup cardboardStartup;

        void Start()
        {
            // Play Again: skip menu entirely.
            if (GameSettings.StartupShown)
            {
                if (menuPanel != null) menuPanel.SetActive(false);
                BeginLesson();
                return;
            }

            if (menuPanel != null) menuPanel.SetActive(true);

            PlaceInFrontOfCamera();
            UpdateLabels();

            if (vrToggleButton != null)
                vrToggleButton.onSelected.AddListener(_ => OnVRToggle());
            if (tutorialButton != null)
                tutorialButton.onSelected.AddListener(_ => OnTutorial());
            if (startGameButton != null)
                startGameButton.onSelected.AddListener(_ => OnStartGame());
        }

        // ── Button handlers ──────────────────────────────────────────────────

        void OnVRToggle()
        {
            GameSettings.ForceDesktopMode = !GameSettings.ForceDesktopMode;
            UpdateLabels();
        }

        void OnTutorial()
        {
            if (tutorialManager == null) return;
            if (menuPanel != null) menuPanel.SetActive(false);
            tutorialManager.StartTutorial(OnTutorialComplete);
        }

        void OnTutorialComplete()
        {
            // Return to the startup menu after the tutorial.
            if (menuPanel != null) menuPanel.SetActive(true);
        }

        void OnStartGame()
        {
            GameSettings.StartupShown = true;
            if (menuPanel != null) menuPanel.SetActive(false);
            if (cardboardStartup != null) cardboardStartup.InitializeVR();
            BeginLesson();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        void BeginLesson()
        {
            var lesson = LessonManager.Instance;
            if (lesson != null) lesson.BeginLesson();
        }

        void UpdateLabels()
        {
            if (vrModeLabel != null)
            {
                vrModeLabel.text = GameSettings.ForceDesktopMode
                    ? "<color=#FFAA44>VR Mode: OFF  (Recording)</color>"
                    : "<color=#44BB66>VR Mode: ON</color>";
            }
        }

        void PlaceInFrontOfCamera()
        {
            var cam = WorldSpaceUI.ResolveCamera();
            WorldSpaceUI.PlaceInFront(transform, cam, 2.5f);
        }
    }
}
