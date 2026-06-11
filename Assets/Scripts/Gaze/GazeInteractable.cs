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
    ///   * notify the lesson and fire its own events when selected.
    ///
    /// <para>
    /// Set <see cref="categoryId"/> to group multiple instances of the same object type
    /// (e.g., all student desks) so that finding any one of them counts as one discovery.
    /// Leave it empty to use <see cref="displayName"/> as the implicit category.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class GazeInteractable : MonoBehaviour
    {
        [Header("Description")]
        [Tooltip("Short title shown in the popup / summary.")]
        public string displayName = "Object";

        [Tooltip("Optional. Objects sharing the same category ID count as a single discovery.\n" +
                 "Leave empty to use displayName as the category (each name = one category).")]
        public string categoryId;

        [Tooltip("Importance level — controls the summary card colour.")]
        public ItemSeverity severity = ItemSeverity.Caution;

        [TextArea(2, 4)]
        [Tooltip("Educational description shown in the post-drill summary.")]
        public string description = "Describe what makes this object notable during an earthquake.";

        [TextArea(1, 3)]
        [Tooltip("Recommended safety action shown in the post-drill summary.")]
        public string recommendedAction = "What should the student do?";

        [Header("Lesson")]
        [Tooltip("If true, discovering this object (category) counts toward the lesson total.")]
        public bool countsTowardLesson = true;

        [Header("Hover feedback")]
        [Tooltip("Scale multiplier applied while the object is gazed at.")]
        public float hoverScale = 1.08f;

        [Tooltip("How quickly the object grows/shrinks on hover (higher = snappier).")]
        public float pulseSpeed = 8f;

        [Header("Events")]
        /// <summary>Raised the first time this specific instance is ever selected.</summary>
        public GazeInteractableEvent onFirstDiscovered = new GazeInteractableEvent();
        /// <summary>Raised every time this object is selected.</summary>
        public GazeInteractableEvent onSelected = new GazeInteractableEvent();

        /// <summary>True once this instance has been selected at least once.</summary>
        public bool Discovered { get; private set; }

        /// <summary>True while the gaze reticle is resting on this object.</summary>
        public bool IsHovered { get; private set; }

        /// <summary>The resolved category identifier (categoryId if set, otherwise displayName).</summary>
        public string CategoryId => string.IsNullOrWhiteSpace(categoryId) ? displayName : categoryId;

        Vector3 _baseScale;
        Vector3 _targetScale;

        void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale, _targetScale, Time.unscaledDeltaTime * pulseSpeed);
        }

        // ── Gaze pointer messages ────────────────────────────────────────────

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
            if (IsHovered) OnPointerExit();
        }
    }
}
