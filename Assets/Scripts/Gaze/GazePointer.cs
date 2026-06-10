using UnityEngine;
using UnityEngine.InputSystem;

namespace GazeVR
{
    /// <summary>
    /// Casts a ray forward from the camera (the user's gaze) and drives gaze interaction:
    /// it hovers <see cref="GazeInteractable"/> objects, dilates the reticle, and selects the
    /// hovered object when the trigger is pressed.
    ///
    /// On a Cardboard device the camera is rotated by the head tracker (a TrackedPoseDriver) and
    /// the viewer button is read through <c>Google.XR.Cardboard.Api</c>. In the Editor — where there
    /// is no headset — it falls back to right-mouse-drag look and mouse/space/touch for the trigger,
    /// so the whole mechanic can be play-tested without a phone.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GazePointer : MonoBehaviour
    {
        [Header("Gaze ray")]
        [Tooltip("Maximum gaze distance in meters (Cardboard convention is 20 m).")]
        public float maxDistance = 20f;

        [Tooltip("Layers the gaze ray can hit. Walls should be included so they block the gaze.")]
        public LayerMask raycastMask = ~0;

        [Header("Reticle (child of the camera)")]
        [Tooltip("The reticle dot transform. It is moved to the gazed surface and dilates on hover.")]
        public Transform reticle;
        public float reticleRestScale = 1f;
        public float reticleHoverScale = 1.8f;
        public float reticleScaleSpeed = 8f;
        [Tooltip("Distance at which to park the reticle when nothing is being gazed at.")]
        public float reticleRestDistance = 6f;
        [Tooltip("Angular size factor – the reticle scales with distance to stay roughly constant on screen.")]
        public float reticleAngularSize = 0.02f;

        [Header("Editor / desktop testing")]
        [Tooltip("Allow right-mouse-drag to look around when no headset is active (Editor & desktop).")]
        public bool enableEditorMouseLook = true;
        public float mouseLookSpeed = 0.12f;

        /// <summary>The interactable currently under the gaze, or null.</summary>
        public GazeInteractable Current { get; private set; }

        float _reticleScale;
        float _pitch, _yaw;

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

            GazeInteractable hovered = null;
            float surfaceDistance = reticleRestDistance;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
                                maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                surfaceDistance = hit.distance;
                hovered = hit.collider.GetComponentInParent<GazeInteractable>();
            }

            // Hover transitions.
            if (hovered != Current)
            {
                if (Current != null) Current.OnPointerExit();
                Current = hovered;
                if (Current != null) Current.OnPointerEnter();
            }

            // Selection.
            if (Current != null && TriggerPressedThisFrame())
            {
                Current.OnPointerClick();
            }

            UpdateReticle(surfaceDistance, hovered != null);
        }

        void UpdateReticle(float distance, bool interactive)
        {
            if (reticle == null) return;

            float target = interactive ? reticleHoverScale : reticleRestScale;
            _reticleScale = Mathf.Lerp(_reticleScale, target, Time.unscaledDeltaTime * reticleScaleSpeed);

            float d = Mathf.Clamp(distance, 0.5f, maxDistance);
            reticle.localPosition = new Vector3(0f, 0f, d);
            reticle.localScale = Vector3.one * (_reticleScale * d * reticleAngularSize);
        }

        bool TriggerPressedThisFrame()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // The Cardboard viewer button (and a screen tap) come through the Cardboard API.
            try
            {
                if (Google.XR.Cardboard.Api.IsTriggerPressed) return true;
            }
            catch
            {
                // XR session not ready yet – ignore and fall through to the standard input checks.
            }
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
            if (UnityEngine.XR.XRSettings.isDeviceActive) return; // a headset is driving the view

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed) return; // hold RMB to look around

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
