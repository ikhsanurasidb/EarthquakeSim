using UnityEngine;
using UnityEngine.Events;

namespace GazeVR
{
    /// <summary>
    /// Applies Perlin-noise positional shake to this transform when the earthquake drill begins.
    ///
    /// Attach to the <b>Player</b> root (the parent of the camera), not to the camera itself.
    /// That way the TrackedPoseDriver and GazePointer remain the sole owners of the camera's
    /// rotation, and the shake is felt as pure floor-vibration — realistic and comfortable in
    /// both 3-DOF Cardboard VR and flat-screen play.
    ///
    /// When the shake completes naturally, <see cref="onShakeComplete"/> is fired so
    /// <see cref="LessonManager"/> can transition to the Summary phase.
    /// </summary>
    public class EarthquakeShaker : MonoBehaviour
    {
        /// <summary>The active shaker in the scene, or null.</summary>
        public static EarthquakeShaker Current { get; private set; }

        [Header("Profile")]
        [Tooltip("Total duration of the shake in seconds.")]
        public float duration = 8f;

        [Tooltip("0–1 normalised time → 0–1 intensity envelope.\n" +
                 "Default: quick ramp-up, sustained peak, gradual decay.")]
        public AnimationCurve intensityCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 8f),
            new Keyframe(0.12f, 1f, 0f, 0f),
            new Keyframe(0.65f, 1f, 0f, -2f),
            new Keyframe(1f, 0f, -3f, 0f)
        );

        [Tooltip("Peak lateral (X) positional displacement in metres.")]
        public float positionAmplitude = 0.07f;

        [Tooltip("Vertical (Y) displacement scale relative to the lateral amplitude.")]
        [Range(0f, 1f)]
        public float verticalScale = 0.45f;

        [Tooltip("How fast the Perlin noise scrolls — controls the perceived vibration frequency.")]
        public float frequency = 14f;

        [Header("Rotation (optional)")]
        [Tooltip("Peak rotational offset in degrees. Leave at 0 for 3-DOF Cardboard comfort;\n" +
                 "small values (≤ 1°) are fine for flat-screen play.")]
        public float rotationAmplitude = 0f;

        [Header("Auto-wiring")]
        [Tooltip("When true, subscribes to LessonManager.onEarthquakeDrillStarted on Start.")]
        public bool subscribeToLessonManager = true;

        [Header("Events")]
        [Tooltip("Fired when the shake sequence completes naturally (not when Stop is called manually).")]
        public UnityEvent onShakeComplete = new UnityEvent();

        // ── Runtime ─────────────────────────────────────────────────────────

        /// <summary>True while a shake sequence is running.</summary>
        public bool IsShaking { get; private set; }

        /// <summary>Normalised intensity in [0, 1], driven by <see cref="intensityCurve"/>.</summary>
        public float Intensity { get; private set; }

        float _elapsed;
        Vector3 _originLocalPos;
        Quaternion _originLocalRot;
        float _seedX, _seedY, _seedPitch, _seedYaw;

        // ── Unity callbacks ──────────────────────────────────────────────────

        void Awake()
        {
            Current = this;
            _seedX = Random.Range(0f, 512f);
            _seedY = Random.Range(0f, 512f);
            _seedPitch = Random.Range(0f, 512f);
            _seedYaw = Random.Range(0f, 512f);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            if (subscribeToLessonManager && LessonManager.Instance != null)
                LessonManager.Instance.onEarthquakeDrillStarted.AddListener(Play);
        }

        void LateUpdate()
        {
            if (!IsShaking) return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= duration)
            {
                StopInternal(natural: true);
                return;
            }

            float t = _elapsed / duration;
            Intensity = intensityCurve.Evaluate(t);

            float s = _elapsed * frequency;
            float dx = (Mathf.PerlinNoise(s + _seedX, 0f) - 0.5f) * 2f * positionAmplitude * Intensity;
            float dy = (Mathf.PerlinNoise(s + _seedY, 1f) - 0.5f) * 2f * positionAmplitude * verticalScale * Intensity;

            transform.localPosition = _originLocalPos + new Vector3(dx, dy, 0f);

            if (rotationAmplitude > 0f)
            {
                float dp = (Mathf.PerlinNoise(s + _seedPitch, 2f) - 0.5f) * 2f * rotationAmplitude * Intensity;
                float dy2 = (Mathf.PerlinNoise(s + _seedYaw, 3f) - 0.5f) * 2f * rotationAmplitude * Intensity * 0.4f;
                transform.localRotation = _originLocalRot * Quaternion.Euler(dp, dy2, 0f);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Starts (or restarts) the shake sequence.</summary>
        public void Play()
        {
            _originLocalPos = transform.localPosition;
            _originLocalRot = transform.localRotation;
            _elapsed = 0f;
            IsShaking = true;
        }

        /// <summary>Stops the shake immediately (does NOT fire <see cref="onShakeComplete"/>).</summary>
        public void Stop() => StopInternal(natural: false);

        void StopInternal(bool natural)
        {
            IsShaking = false;
            Intensity = 0f;
            transform.localPosition = _originLocalPos;
            transform.localRotation = _originLocalRot;

            if (natural) onShakeComplete.Invoke();
        }
    }
}
