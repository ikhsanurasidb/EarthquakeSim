using UnityEngine;

namespace GazeVR
{
    /// <summary>
    /// Adds secondary earthquake shake motion to a prop or piece of furniture.
    ///
    /// Every frame the component polls <see cref="EarthquakeShaker.Current"/>: when the drill is
    /// active it displaces the object with Perlin-noise offsets scaled by the current intensity;
    /// when the drill ends it snaps back to the rest pose.
    ///
    /// Each instance is seeded randomly in <see cref="Awake"/>, so a room full of desks and chairs
    /// all rattle at slightly different phases and frequencies rather than moving in lockstep.
    ///
    /// Attach to any GameObject whose Transform you want to shake. The component modifies
    /// <c>localPosition</c> and <c>localRotation</c> only; it does not interfere with scale
    /// (used by <see cref="GazeInteractable"/> for hover feedback).
    /// </summary>
    public class ShakeableObject : MonoBehaviour
    {
        [Header("Amplitudes")]
        [Tooltip("Peak positional displacement in metres.")]
        public float positionAmplitude = 0.025f;

        [Tooltip("Peak rotational displacement in degrees.")]
        public float rotationAmplitude = 1.2f;

        [Header("Frequency variation")]
        [Tooltip("Base frequency multiplier relative to the EarthquakeShaker frequency.\n" +
                 "A random ±30 % variation is also applied per instance at startup.")]
        [Range(0.3f, 2f)]
        public float frequencyMultiplier = 1f;

        // ── Private state ────────────────────────────────────────────────────

        Vector3 _restLocalPos;
        Quaternion _restLocalRot;
        float _runtimeFreqMult;    // frequencyMultiplier + random variation baked in Awake
        float _seedX, _seedY, _seedZ;
        float _seedPitch, _seedYaw, _seedRoll;
        float _elapsed;
        bool _wasShaking;

        // ── Unity callbacks ──────────────────────────────────────────────────

        void Awake()
        {
            _restLocalPos = transform.localPosition;
            _restLocalRot = transform.localRotation;

            // Randomise frequency and noise seeds so every object feels independent.
            _runtimeFreqMult = frequencyMultiplier * Random.Range(0.72f, 1.38f);
            _seedX = Random.Range(0f, 512f);
            _seedY = Random.Range(0f, 512f);
            _seedZ = Random.Range(0f, 512f);
            _seedPitch = Random.Range(0f, 512f);
            _seedYaw = Random.Range(0f, 512f);
            _seedRoll = Random.Range(0f, 512f);
        }

        void LateUpdate()
        {
            var shaker = EarthquakeShaker.Current;
            bool shaking = shaker != null && shaker.IsShaking;

            if (!shaking)
            {
                if (_wasShaking)
                {
                    // Snap to rest so objects don't freeze mid-wobble.
                    transform.localPosition = _restLocalPos;
                    transform.localRotation = _restLocalRot;
                    _elapsed = 0f;
                    _wasShaking = false;
                }
                return;
            }

            _wasShaking = true;
            _elapsed += Time.unscaledDeltaTime;

            float intensity = shaker.Intensity;
            float s = _elapsed * shaker.frequency * _runtimeFreqMult;

            float dx = (Mathf.PerlinNoise(s + _seedX, 0f) - 0.5f) * 2f * positionAmplitude * intensity;
            float dy = (Mathf.PerlinNoise(s + _seedY, 1f) - 0.5f) * 2f * positionAmplitude * 0.4f * intensity;
            float dz = (Mathf.PerlinNoise(s + _seedZ, 2f) - 0.5f) * 2f * positionAmplitude * 0.25f * intensity;

            float dpitch = (Mathf.PerlinNoise(s + _seedPitch, 3f) - 0.5f) * 2f * rotationAmplitude * intensity;
            float dyaw = (Mathf.PerlinNoise(s + _seedYaw, 4f) - 0.5f) * 2f * rotationAmplitude * 0.5f * intensity;
            float droll = (Mathf.PerlinNoise(s + _seedRoll, 5f) - 0.5f) * 2f * rotationAmplitude * 0.3f * intensity;

            transform.localPosition = _restLocalPos + new Vector3(dx, dy, dz);
            transform.localRotation = _restLocalRot * Quaternion.Euler(dpitch, dyaw, droll);
        }

        void OnDisable()
        {
            transform.localPosition = _restLocalPos;
            transform.localRotation = _restLocalRot;
            _wasShaking = false;
        }
    }
}
