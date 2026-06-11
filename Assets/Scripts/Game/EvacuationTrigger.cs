using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Attach to the exit-door GameObject (which must also have a <see cref="GazeInteractable"/>).
    /// When the player gazes at the door during the <see cref="GamePhase.Evacuation"/> phase,
    /// this calls <see cref="LessonManager.CompleteEvacuation"/> to advance to the summary.
    /// </summary>
    [RequireComponent(typeof(GazeInteractable))]
    public class EvacuationTrigger : MonoBehaviour
    {
        void Start()
        {
            GetComponent<GazeInteractable>().onSelected.AddListener(OnSelected);
        }

        void OnDestroy()
        {
            var gi = GetComponent<GazeInteractable>();
            if (gi != null) gi.onSelected.RemoveListener(OnSelected);
        }

        void OnSelected(GazeInteractable _)
        {
            var lesson = LessonManager.Instance;
            if (lesson != null && lesson.CurrentPhase == GamePhase.Evacuation)
                lesson.CompleteEvacuation();
        }
    }
}
