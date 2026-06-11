using UnityEngine;
using UnityEngine.InputSystem;

namespace GazeVR
{
    /// <summary>
    /// Casts a ray from the camera's forward vector (the player's gaze) and drives gaze interaction.
    ///
    /// <para><b>Selection modes:</b>
    /// <list type="bullet">
    ///   <item><b>VR device active</b> — <em>dwell selection</em>: gaze at an object for
    ///   <see cref="dwellDuration"/> seconds to select it. The reticle fills as a progress indicator.</item>
    ///   <item><b>Editor / desktop</b> — <em>trigger selection</em>: left-click, Space, or screen-tap.
    ///   Right-mouse-drag provides free-look for playtesting without a headset.</item>
    /// </list>
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GazePointer : MonoBehaviour
    {
        [Header("Gaze ray")]
        [Tooltip("Maximum gaze distance in metres (Cardboard convention: 20 m).")]
        public float maxDistance = 20f;

        [Tooltip("Layers the gaze ray can hit. Include walls so they block the gaze.")]
        public LayerMask raycastMask = ~0;

        [Header("Reticle (child of the camera)")]
        [Tooltip("The reticle dot transform. Moved to the gazed surface; dilates on hover.")]
        public Transform reticle;
        public float reticleRestScale = 1f;
        public float reticleHoverScale = 1.8f;
        public float reticleScaleSpeed = 8f;
        [Tooltip("Distance at which to park the reticle when nothing is gazed at.")]
        public float reticleRestDistance = 6f;
        [Tooltip("Angular-size factor — keeps the reticle roughly constant on screen.")]
        public float reticleAngularSize = 0.02f;

        [Header("Dwell selection (VR)")]
        [Tooltip("Seconds to keep the gaze on an object to select it (VR / device mode only).")]
        public float dwellDuration = 1.5f;
        [Tooltip("The reticle grows up to this multiplier of reticleHoverScale during dwell.")]
        public float dwellReticleGrowth = 1.5f;

        [Header("Editor / desktop testing")]
        [Tooltip("Allow right-mouse-drag to look around when no headset is active.")]
        public bool enableEditorMouseLook = true;
        public float mouseLookSpeed = 0.12f;

        // ── Runtime ─────────────────────────────────────────────────────────

        /// <summary>The interactable currently under the gaze, or null.</summary>
        public GazeInteractable Current { get; private set; }

        /// <summary>Dwell progress towards the next selection (0 = none, 1 = complete).</summary>
        public float DwellProgress { get; private set; }

        float _reticleScale;
        float _pitch, _yaw;
        GazeInteractable _dwellTarget;
        float _dwellTimer;

        void Awake()
        {
            _reticleScale = reticleRestScale;
            Vector3 e = transform.localEulerAngles;
            _pitch = NormalizeAngle(e.x);
            _yaw = e.y;
        }

        void Update()
        {
            MaybeMouseLook();

            // ── Raycast ──────────────────────────────────────────────────────
            GazeInteractable hovered = null;
            float surfaceDistance = reticleRestDistance;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
                                maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                surfaceDistance = hit.distance;
                hovered = hit.collider.GetComponentInParent<GazeInteractable>();
            }

            // ── Hover transitions ────────────────────────────────────────────
            if (hovered != Current)
            {
                if (Current != null) Current.OnPointerExit();
                Current = hovered;
                if (Current != null) Current.OnPointerEnter();

                // Reset dwell whenever gaze moves to a different target.
                _dwellTarget = null;
                _dwellTimer = 0f;
                DwellProgress = 0f;
            }

            // ── Selection ────────────────────────────────────────────────────
            if (UnityEngine.XR.XRSettings.isDeviceActive && !GameSettings.ForceDesktopMode)
            {
                // VR mode: dwell selection
                if (hovered != null)
                {
                    if (hovered == _dwellTarget)
                    {
                        _dwellTimer += Time.unscaledDeltaTime;
                        DwellProgress = Mathf.Clamp01(_dwellTimer / dwellDuration);

                        if (_dwellTimer >= dwellDuration)
                        {
                            hovered.OnPointerClick();
                            // Require looking away and back before triggering again.
                            _dwellTarget = null;
                            _dwellTimer = 0f;
                            DwellProgress = 0f;
                        }
                    }
                    else
                    {
                        _dwellTarget = hovered;
                        _dwellTimer = 0f;
                        DwellProgress = 0f;
                    }
                }
                else
                {
                    _dwellTarget = null;
                    _dwellTimer = 0f;
                    DwellProgress = 0f;
                }
            }
            else
            {
                // Editor / desktop mode: trigger-press selection
                DwellProgress = 0f;
                if (Current != null && TriggerPressedThisFrame())
                    Current.OnPointerClick();
            }

            UpdateReticle(surfaceDistance, hovered != null);
        }

        void UpdateReticle(float distance, bool interactive)
        {
            if (reticle == null) return;

            float target;
            if (DwellProgress > 0f)
                // Reticle grows as dwell progresses, giving the player clear VR feedback.
                target = Mathf.Lerp(reticleHoverScale, reticleHoverScale * dwellReticleGrowth, DwellProgress);
            else
                target = interactive ? reticleHoverScale : reticleRestScale;

            _reticleScale = Mathf.Lerp(_reticleScale, target, Time.unscaledDeltaTime * reticleScaleSpeed);

            float d = Mathf.Clamp(distance, 0.5f, maxDistance);
            reticle.localPosition = new Vector3(0f, 0f, d);
            reticle.localScale = Vector3.one * (_reticleScale * d * reticleAngularSize);
        }

        bool TriggerPressedThisFrame()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { if (Google.XR.Cardboard.Api.IsTriggerPressed) return true; } catch { }
#endif
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;

            Touchscreen ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) return true;

            return false;
        }

        void MaybeMouseLook()
        {
            if (!enableEditorMouseLook) return;
            if (UnityEngine.XR.XRSettings.isDeviceActive && !GameSettings.ForceDesktopMode) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed) return;

            Vector2 delta = mouse.delta.ReadValue();
            _yaw += delta.x * mouseLookSpeed;
            _pitch -= delta.y * mouseLookSpeed;
            _pitch = Mathf.Clamp(_pitch, -85f, 85f);
            transform.localEulerAngles = new Vector3(_pitch, _yaw, 0f);
        }

        static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            return a;
        }
    }
}
