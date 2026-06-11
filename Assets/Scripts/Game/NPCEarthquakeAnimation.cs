using System.Collections;
using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Drives the earthquake-response animation for a single NPC.
    ///
    /// On <see cref="LessonManager.onEarthquakeDrillStarted"/>:
    ///   1. Fires the <c>StartHiding</c> Animator trigger (one-shot, auto-resets).
    ///   2. Slides the NPC forward toward its nearest desk so it looks like it is
    ///      actually taking cover underneath, then holds the <em>Hiding</em> pose.
    ///
    /// Attach to the <b>root</b> NPC GameObject (parent that has or contains the Animator).
    /// </summary>
    public class NPCEarthquakeAnimation : MonoBehaviour
    {
        static readonly int StartHidingHash = Animator.StringToHash("StartHiding");

        [Header("Cover slide")]
        [Tooltip("Explicit desk/table for this NPC to slide under. " +
                 "Leave empty to auto-find the nearest object whose name contains deskNameKeyword.")]
        public Transform deskTarget;

        [Tooltip("Name substring used for auto-finding a desk when deskTarget is not assigned. " +
                 "Matches case-insensitively against GameObject names (e.g. \"table\", \"desk\").")]
        public string deskNameKeyword = "table";

        [Tooltip("How far (metres) to slide toward the nearest desk when hiding starts.")]
        public float slideDistance = 0.7f;

        [Tooltip("Duration of the position slide in seconds.")]
        public float slideDuration = 0.6f;

        [Tooltip("How far below the desk surface to lower the NPC (simulates crouching under).")]
        public float crouchYOffset = -0.1f;

        Animator _animator;
        Coroutine _slideRoutine;

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            var lesson = LessonManager.Instance;
            if (lesson != null)
                lesson.onEarthquakeDrillStarted.AddListener(OnEarthquakeDrillStarted);
        }

        void OnDestroy()
        {
            var lesson = LessonManager.Instance;
            if (lesson != null)
                lesson.onEarthquakeDrillStarted.RemoveListener(OnEarthquakeDrillStarted);
        }

        void OnEarthquakeDrillStarted()
        {
            // Fire trigger — it auto-resets so AnyState won't re-enter StandingToCrouch
            if (_animator != null)
                _animator.SetTrigger(StartHidingHash);

            // Slide toward nearest desk and lower slightly
            if (_slideRoutine != null) StopCoroutine(_slideRoutine);
            _slideRoutine = StartCoroutine(SlideUnderDesk());
        }

        IEnumerator SlideUnderDesk()
        {
            // Resolve the target desk: use the explicit override if set, otherwise find the
            // nearest GameObject whose name contains deskNameKeyword (case-insensitive).
            // Student desks are plain furniture with no GazeInteractable, so we search by name.
            Transform target = deskTarget;
            if (target == null && !string.IsNullOrEmpty(deskNameKeyword))
            {
                string keyword = deskNameKeyword.ToLowerInvariant();
                float bestDist = float.MaxValue;
                foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (!go.name.ToLowerInvariant().Contains(keyword)) continue;
                    float d = (go.transform.position - transform.position).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; target = go.transform; }
                }
            }

            // Direction toward desk (horizontal only)
            Vector3 dir = Vector3.zero;
            if (target != null)
            {
                dir = target.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f) dir = dir.normalized;
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + dir * slideDistance
                               + Vector3.up * crouchYOffset;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            transform.position = endPos;
        }
    }
}
