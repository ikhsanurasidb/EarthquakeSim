using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Static bag of session and persistent settings.
    /// <see cref="ForceDesktopMode"/> is saved to PlayerPrefs so the recording-mode preference
    /// survives app restarts. The other flags are session-only (reset each launch).
    /// </summary>
    public static class GameSettings
    {
        const string PrefKeyDesktopMode = "GazeVR.ForceDesktopMode";

        static bool _forceDesktopMode = PlayerPrefs.GetInt(PrefKeyDesktopMode, 0) != 0;

        /// <summary>
        /// When true: GazePointer uses click/tap instead of dwell, and VR rendering is disabled.
        /// Persisted to PlayerPrefs across app restarts.
        /// </summary>
        public static bool ForceDesktopMode
        {
            get => _forceDesktopMode;
            set
            {
                _forceDesktopMode = value;
                PlayerPrefs.SetInt(PrefKeyDesktopMode, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Whether the player opted into the tutorial on this session.</summary>
        public static bool TutorialEnabled { get; set; } = false;

        /// <summary>
        /// Set to true after the startup menu has been dismissed so it is not shown again on
        /// Play Again. Intentionally session-only — resets on each app launch.
        /// </summary>
        public static bool StartupShown { get; set; } = false;
    }
}
