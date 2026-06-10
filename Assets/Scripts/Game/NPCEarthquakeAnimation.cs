using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Drives the earthquake-response animation for a single NPC.
    ///
    /// When <see cref="LessonManager.onEarthquakeDrillStarted"/> fires, this component sets
    /// the <c>IsHiding</c> bool on the NPC's Animator, which triggers the
    /// <em>StandingToCrouch</em> → <em>Hiding</em> transition that was added to every
    /// character controller by the ClassroomAnimationPatcher editor tool.
    ///
    /// Attach to the <b>root</b> of each NPC prefab instance (the parent that contains
    /// the Animator component in its children).
    /// </summary>
    public class NPCEarthquakeAnimation : MonoBehaviour
    {
        static readonly int IsHidingHash = Animator.StringToHash("IsHiding");

        Animator _animator;

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
            if (_animator == null) return;
            _animator.SetBool(IsHidingHash, true);
        }
    }
}
