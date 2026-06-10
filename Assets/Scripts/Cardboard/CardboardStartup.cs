using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.XR.Cardboard;
#endif

namespace GazeVR
{
    /// <summary>
    /// Initializes the Google Cardboard XR session at runtime:
    ///   * keeps the screen awake and at full brightness,
    ///   * scans / loads the viewer (lens) parameters via QR code on first run,
    ///   * recenters the head tracker on a long trigger press,
    ///   * reloads parameters when the viewer changes,
    ///   * lets the close (X) button quit and the gear button rescan the viewer.
    ///
    /// All Cardboard API calls are compiled only for Android device builds; in the Editor this is a
    /// no-op so the scene can be previewed without a headset.
    /// </summary>
    public class CardboardStartup : MonoBehaviour
    {
        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.brightness = 1.0f;

            // Prompt the QR scan only if we have not stored a viewer profile yet.
            if (!Api.HasDeviceParams())
            {
                Api.ScanDeviceParams();
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        void Update()
        {
            if (Api.IsGearButtonPressed)
            {
                Api.ScanDeviceParams();
            }

            if (Api.IsCloseButtonPressed)
            {
                Application.Quit();
            }

            if (Api.IsTriggerHeldPressed)
            {
                Api.Recenter();
            }

            if (Api.HasNewDeviceParams())
            {
                Api.ReloadDeviceParams();
            }

            Api.UpdateScreenParams();
        }
#endif
    }
}
