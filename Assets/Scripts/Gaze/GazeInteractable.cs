using UnityEngine;
using UnityEngine.Events;

namespace GazeVR
{
    /// <summary>UnityEvent carrying the interactable it refers to (serializable for the inspector).</summary>
    [System.Serializable]
    public class GazeInteractableEvent : UnityEvent<GazeInteractable> { }

    /// <summary>
    /// Makes a classroom object selectable by gaze. Attach it to a GameObject that has a Collider,
    /// fill in the description data in the inspector, and the object will:
    ///   * pulse slightly larger while it is gazed at, and shrink back when the gaze leaves,
    ///   * remember whether it has ever been discovered,
    ///   * show its hazard popup and notify the lesson when selected (Cardboard trigger / click).
    ///
    /// The handler method names (OnPointerEnter / OnPointerExit / OnPointerClick) deliberately match
    /// the messages broadcast by Google's <c>CardboardReticlePointer</c>, so this component works
    /// whether it is driven by <see cref="GazePointer"/> or by Google's reticle via SendMessage.
    /// No code changes are needed to make a new object selectable – just add this component.
    /// </summary>
    [DisallowMultipleComponent]
    public class GazeInteractable : MonoBehaviour
    {
        [Header("Description (shown in the popup)")]
        [Tooltip("Short title shown in the popup header.")]
        public string displayName = "Object";

        [Tooltip("Severity – controls the popup header color and the lesson messaging.")]
        public HazardSeverity severity = HazardSeverity.Caution;

        [TextArea(2, 4)]
        [Tooltip("Why this object matters during an earthquake.")]
        public string description = "Describe why this object matters during an earthquake.";

        [TextArea(1, 3)]
        [Tooltip("The recommended action for the student.")]
        public string recommendedAction = "What should the student do?";

        [Header("Lesson")]
        [Tooltip("If true, discovering this object counts toward the lesson's hazard tally.")]
        public bool countsTowardLesson = true;

        [Header("Hover feedback")]
        [Tooltip("Scale multiplier applied while the object is gazed at.")]
        public float hoverScale = 1.08f;

        [Tooltip("How quickly the object grows / shrinks (higher = snappier).")]
        public float pulseSpeed = 8f;

        [Header("Events")]
        /// <summary>Raised the first time this object is ever selected.</summary>
        public GazeInteractableEvent onFirstDiscovered = new GazeInteractableEvent();
        /// <summary>Raised every time this object is selected.</summary>
        public GazeInteractableEvent onSelected = new GazeInteractableEvent();

        /// <summary>True once this object has been selected at least once.</summary>
        public bool Discovered { get; private set; }

        /// <summary>True while the gaze reticle is resting on this object.</summary>
        public bool IsHovered { get; private set; }

        Vector3 _baseScale;
        Vector3 _targetScale;

        void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
        }

        void Update()
        {
            // Smoothly ease toward the current target scale (uses unscaled time so the pulse keeps
            // working even if the earthquake drill later slows or pauses game time).
            transform.localScale = Vector3.Lerp(
                transform.localScale, _targetScale, Time.unscaledDeltaTime * pulseSpeed);
        }

        // --- Gaze pointer messages (sent by GazePointer or Google's CardboardReticlePointer) ---

        public void OnPointerEnter()
        {
            IsHovered = true;
            _targetScale = _baseScale * Mathf.Max(1f, hoverScale);
        }

        public void OnPointerExit()
        {
            IsHovered = false;
            _targetScale = _baseScale;
        }

        public void OnPointerClick()
        {
            Select();
        }

        /// <summary>Selects this object: marks it discovered (first time) and fires its events.</summary>
        public void Select()
        {
            if (!Discovered)
            {
                Discovered = true;
                onFirstDiscovered.Invoke(this);
            }

            onSelected.Invoke(this);
        }

        void OnDisable()
        {
            // Make sure we don't leave the object stuck in its enlarged hover state.
            if (IsHovered)
            {
                OnPointerExit();
            }
        }
    }
}
