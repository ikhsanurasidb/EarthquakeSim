using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Tracks classroom attendance (how many students are present versus the full roster).
    /// The scene builder sets these counts when it populates the desks. The lesson treats
    /// attendance as acceptable when at least <see cref="passRatio"/> of the students are present
    /// (50% by default).
    /// </summary>
    public class ClassroomAttendance : MonoBehaviour
    {
        [Tooltip("Number of students physically present in the classroom.")]
        [Min(0)] public int present;

        [Tooltip("Total students on the roster (present + absent).")]
        [Min(0)] public int total;

        [Tooltip("Minimum present/total ratio for attendance to pass.")]
        [Range(0f, 1f)] public float passRatio = 0.5f;

        /// <summary>Fraction of students present (0..1).</summary>
        public float Ratio => total > 0 ? (float)present / total : 0f;

        /// <summary>True when present students meet or exceed the pass ratio.</summary>
        public bool MeetsThreshold => Ratio >= passRatio;
    }
}
