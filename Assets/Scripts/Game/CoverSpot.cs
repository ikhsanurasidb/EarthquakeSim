using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Marks a sturdy desk or piece of furniture as a cover spot during the earthquake drill.
    ///
    /// During the <see cref="GamePhase.Earthquake"/> phase, when the player dwells on this object
    /// it triggers the cover action via <see cref="LessonManager.TakeCover"/>.
    ///
    /// Requires a <see cref="GazeInteractable"/> on the same GameObject so the object is
    /// selectable by gaze. The cover action is fired when <c>onSelected</c> fires and the
    /// game is in the Earthquake phase.
    /// </summary>
    [RequireComponent(typeof(GazeInteractable))]
    public class CoverSpot : MonoBehaviour
    {
        GazeInteractable _interactable;

        void Awake()
        {
            _interactable = GetComponent<GazeInteractable>();
        }

        void Start()
        {
            _interactable.onSelected.AddListener(OnSelected);
        }

        void OnDestroy()
        {
            if (_interactable != null)
                _interactable.onSelected.RemoveListener(OnSelected);
        }

        void OnSelected(GazeInteractable item)
        {
            var lesson = LessonManager.Instance;
            if (lesson == null || lesson.CurrentPhase != GamePhase.Earthquake) return;
            lesson.TakeCover();
        }
    }
}
