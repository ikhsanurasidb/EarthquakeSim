using System.Collections;
using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Animates the VR camera down from standing eye-height to under-desk height when the
    /// player takes cover during the earthquake drill.
    ///
    /// The camera's <c>localPosition.y</c> is lerped from <see cref="standingHeight"/>
    /// (1.6 m) to <see cref="crouchHeight"/> (0.5 m) over <see cref="crouchDuration"/>
    /// seconds. Because <see cref="UnityEngine.SpatialTracking.TrackedPoseDriver"/> only
    /// controls <em>rotation</em> (RotationOnly mode), modifying local position here does
    /// not conflict with head-tracking.
    ///
    /// Attach to <b>Main Camera</b>. Fires when <see cref="LessonManager.onCoverTaken"/>
    /// is raised (i.e. the player has dwelt on a <see cref="CoverSpot"/>).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerCoverAnimation : MonoBehaviour
    {
        [Header("Heights (local Y, relative to Player root)")]
        [Tooltip("Camera Y while standing — should match the value set in the player rig (default 1.6 m).")]
        public float standingHeight = 1.6f;

        [Tooltip("Camera Y when crouched under a desk.")]
        public float crouchHeight = 0.5f;

        [Header("Transition")]
        [Tooltip("Duration of the crouch-down movement in seconds.")]
        public float crouchDuration = 0.9f;

        [Tooltip("Easing curve for the camera movement (X = normalised time, Y = normalised position).")]
        public AnimationCurve crouchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        Coroutine _routine;

        void Start()
        {
            var lesson = LessonManager.Instance;
            if (lesson != null)
                lesson.onCoverTaken.AddListener(OnCoverTaken);
        }

        void OnDestroy()
        {
            var lesson = LessonManager.Instance;
            if (lesson != null)
                lesson.onCoverTaken.RemoveListener(OnCoverTaken);
        }

        void OnCoverTaken()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(CrouchDown());
        }

        IEnumerator CrouchDown()
        {
            float startY = transform.localPosition.y;
            float elapsed = 0f;

            while (elapsed < crouchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = crouchCurve.Evaluate(Mathf.Clamp01(elapsed / crouchDuration));
                Vector3 pos = transform.localPosition;
                pos.y = Mathf.Lerp(startY, crouchHeight, t);
                transform.localPosition = pos;
                yield return null;
            }

            // Snap to final position
            Vector3 final = transform.localPosition;
            final.y = crouchHeight;
            transform.localPosition = final;
        }
    }
}
