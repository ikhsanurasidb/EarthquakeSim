using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.XR.Cardboard;
#endif

namespace GazeVR
{
    /// <summary>
    /// Initializes the Google Cardboard XR session.
    /// When <see cref="GameSettings.ForceDesktopMode"/> is true (recording mode) the Cardboard
    /// session is not started and XR rendering is disabled so the app records as a flat view.
    ///
    /// Call <see cref="InitializeVR"/> after the startup menu to actually start Cardboard.
    /// </summary>
    public class CardboardStartup : MonoBehaviour
    {
        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        /// <summary>
        /// Called by <see cref="StartupMenu"/> once the player has made their mode choice.
        /// Starts Cardboard if VR is desired, or disables XR rendering for recording mode.
        /// </summary>
        public void InitializeVR()
        {
            if (GameSettings.ForceDesktopMode)
            {
                UnityEngine.XR.XRSettings.enabled = false;
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.brightness = 1.0f;
            if (!Api.HasDeviceParams())
                Api.ScanDeviceParams();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        void Update()
        {
            if (GameSettings.ForceDesktopMode) return;

            if (Api.IsGearButtonPressed)
                Api.ScanDeviceParams();

            if (Api.IsCloseButtonPressed)
                Application.Quit();

            if (Api.IsTriggerHeldPressed)
                Api.Recenter();

            if (Api.HasNewDeviceParams())
                Api.ReloadDeviceParams();

            Api.UpdateScreenParams();
        }
#endif
    }
}
