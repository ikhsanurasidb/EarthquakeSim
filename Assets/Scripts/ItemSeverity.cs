namespace GazeVR
{
    /// <summary>
    /// Importance / risk classification for a gaze-selectable classroom object.
    /// Drives the popup header colour and helps the student reason about safety.
    /// </summary>
    public enum ItemSeverity
    {
        /// <summary>A safe spot or a recommended action (green).</summary>
        Safe = 0,

        /// <summary>Worth noting — potential hazard (amber).</summary>
        Caution = 1,

        /// <summary>A clear danger during an earthquake (red).</summary>
        Danger = 2
    }
}
