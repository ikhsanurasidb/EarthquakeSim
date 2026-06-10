namespace GazeVR
{
    /// <summary>
    /// Static bag of session-level settings that survive scene reloads.
    /// (Static fields are NOT reset by SceneManager.LoadScene, only by domain reload in the Editor.)
    /// </summary>
    public static class GameSettings
    {
        /// <summary>When true, GazePointer uses click/tap instead of dwell, and VR rendering is disabled.</summary>
        public static bool ForceDesktopMode { get; set; } = false;

        /// <summary>Whether the player opted into the tutorial on this session.</summary>
        public static bool TutorialEnabled { get; set; } = false;

        /// <summary>Set to true after the startup menu has been dismissed so it is not shown again on Play Again.</summary>
        public static bool StartupShown { get; set; } = false;
    }
}
