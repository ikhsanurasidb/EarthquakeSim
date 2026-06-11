using System;
using UnityEngine;
using UnityEngine.UI;

namespace GazeVR
{
    /// <summary>
    /// Runs a short gaze-selection practice sequence before the main lesson.
    /// Shows one practice target at a time; the player gazes (dwell) or clicks it to advance.
    /// After all targets are selected, fires the completion callback.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("Tutorial UI")]
        public GameObject tutorialPanel;
        public Text instructionText;

        [Header("Practice targets (GazeInteractable objects in the scene)")]
        [Tooltip("Assign 2-3 highlighted target GameObjects. They are hidden until the tutorial starts.")]
        public GazeInteractable[] practiceTargets;

        Action _onComplete;
        int _step;

        void Awake()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            foreach (var t in practiceTargets)
                if (t != null) t.gameObject.SetActive(false);
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void StartTutorial(Action onComplete)
        {
            _onComplete = onComplete;
            _step = 0;
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
            ActivateStep(_step);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        void ActivateStep(int step)
        {
            if (step >= practiceTargets.Length)
            {
                CompleteTutorial();
                return;
            }

            // Show only the current target.
            for (int i = 0; i < practiceTargets.Length; i++)
                if (practiceTargets[i] != null)
                    practiceTargets[i].gameObject.SetActive(i == step);

            var target = practiceTargets[step];
            if (target != null) target.onSelected.AddListener(OnTargetSelected);

            UpdateInstruction(step);
        }

        void OnTargetSelected(GazeInteractable target)
        {
            target.onSelected.RemoveListener(OnTargetSelected);
            target.gameObject.SetActive(false);
            _step++;
            ActivateStep(_step);
        }

        void UpdateInstruction(int step)
        {
            if (instructionText == null) return;
            instructionText.text = step switch
            {
                0 => "TUTORIAL\n\nLook at the glowing button and hold your gaze to select it.\nIn VR: keep your gaze on it until it activates.\nOn screen: click or tap it.",
                1 => "Great job!\n\nYou selected it! Try the next one.",
                _ => "Almost done! Select the last target."
            };
        }

        void CompleteTutorial()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            foreach (var t in practiceTargets)
                if (t != null) t.gameObject.SetActive(false);
            _onComplete?.Invoke();
        }
    }
}
