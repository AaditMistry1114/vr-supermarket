using UnityEngine;

/// <summary>
/// VR Player Adapter — currently a harmless stub for future VR support.
///
/// HOW TO ENABLE VR LATER:
///   1. Install "XR Interaction Toolkit" from the Package Manager.
///   2. Create an XR Origin rig and assign it to xrOriginRig below.
///   3. Tick the "enableVRMode" checkbox on this component.
///
/// Until then, this script always activates Desktop mode and does nothing else.
/// </summary>
public class VRPlayerAdapter : MonoBehaviour
{
    [Header("Mode Toggle")]
    [Tooltip("Only tick this after the XR Interaction Toolkit package is installed.")]
    public bool enableVRMode = false;

    [Header("Player Rigs")]
    [Tooltip("The standard FPS CharacterController rig.")]
    public GameObject fpsPlayerRig;

    [Tooltip("The XR Origin rig for VR — assign after installing XR Interaction Toolkit.")]
    public GameObject xrOriginRig;

    private void Start()
    {
        if (enableVRMode && IsVRDeviceConnected())
            ActivateVR();
        else
            ActivateDesktop();
    }

    // ─── Private ──────────────────────────────────────────────────────────────

    private bool IsVRDeviceConnected()
    {
        // TODO: Replace with XR Management device check when XR ITK is installed.
        // e.g. return UnityEngine.XR.XRSettings.isDeviceActive;
        return false;
    }

    private void ActivateDesktop()
    {
        if (fpsPlayerRig != null) fpsPlayerRig.SetActive(true);
        if (xrOriginRig  != null) xrOriginRig.SetActive(false);
        Debug.Log("[VRPlayerAdapter] Desktop mode active.");
    }

    private void ActivateVR()
    {
        if (fpsPlayerRig != null) fpsPlayerRig.SetActive(false);
        if (xrOriginRig  != null) xrOriginRig.SetActive(true);
        Debug.Log("[VRPlayerAdapter] VR mode active.");
    }
}
