using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Shared helpers for world-space UI canvases that need to place themselves in front of the
    /// player and continuously face the camera. Eliminates the identical PlaceInFront /
    /// ResolveCamera code that was duplicated across SummaryPanel, HazardPopup, and StartupMenu.
    /// </summary>
    public static class WorldSpaceUI
    {
        /// <summary>
        /// Returns <paramref name="current"/> if non-null, otherwise Camera.main's transform.
        /// Caches nothing — callers should store the result.
        /// </summary>
        public static Transform ResolveCamera(Transform current = null)
        {
            if (current != null) return current;
            return Camera.main != null ? Camera.main.transform : null;
        }

        /// <summary>
        /// Positions <paramref name="target"/> <paramref name="distance"/> metres ahead of
        /// <paramref name="cam"/> on the horizontal plane and rotates it to face the camera.
        /// </summary>
        public static void PlaceInFront(Transform target, Transform cam,
                                        float distance, float verticalOffset = 0f)
        {
            if (cam == null) return;
            Vector3 fwd = cam.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();
            target.position = cam.position + fwd * distance + Vector3.up * verticalOffset;
            FaceCamera(target, cam);
        }

        /// <summary>Rotates <paramref name="target"/> so it faces <paramref name="cam"/>.</summary>
        public static void FaceCamera(Transform target, Transform cam)
        {
            if (cam == null) return;
            Vector3 dir = target.position - cam.position;
            if (dir.sqrMagnitude > 0.0001f)
                target.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
