namespace GazeVR
{
    /// <summary>
    /// Severity classification for a gaze-selectable classroom object. Drives the popup header
    /// color and helps the student reason about earthquake risk.
    /// </summary>
    public enum HazardSeverity
    {
        /// <summary>A safe place or a recommended action (green).</summary>
        Safe = 0,

        /// <summary>Be careful – a potential hazard worth noting (amber).</summary>
        Caution = 1,

        /// <summary>A clear danger during an earthquake (red).</summary>
        Danger = 2
    }
}
