using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace GazeVR.EditorTools
{
    /// <summary>
    /// Applies the player settings recommended for a Google Cardboard build on Android:
    /// linear color, OpenGLES3, IL2CPP + ARM64 (required by Google Play), API level 24+, and a
    /// landscape orientation (Cardboard renders side-by-side stereo in landscape).
    ///
    /// The Cardboard XR loader itself is already enabled for Android under
    /// <c>Project Settings ▸ XR Plug-in Management</c>; this just aligns the rest of the player
    /// settings. Run it from <b>GazeVR ▸ Configure Android + Cardboard Player Settings</b>.
    /// </summary>
    public static class AndroidCardboardSetup
    {
        [MenuItem("GazeVR/Configure Android + Cardboard Player Settings")]
        public static void Configure()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            Debug.Log("[AndroidCardboardSetup] Applied: Linear color, OpenGLES3, IL2CPP/ARM64, " +
                      "minSdk 24, Landscape-Left. (Cardboard XR loader is enabled via XR Plug-in Management.)");
        }
    }
}
