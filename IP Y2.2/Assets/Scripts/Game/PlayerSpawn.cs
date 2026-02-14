/// <summary>
/// File: PlayerSpawn.cs
/// Author: Jayden Wong
/// Description: Moves the XR Origin to the spawn point, normalizing all players to a fixed virtual height.
/// This ensures everyone experiences the world at the same scale.
/// </summary>
using UnityEngine;
using Unity.XR.CoreUtils;

[DefaultExecutionOrder(100)]
public class PlayerSpawn : MonoBehaviour
{
    [Header("Virtual Height Settings")]
    [Tooltip("Target virtual height for all players (recommended: 1.75-1.8m)")]
    [SerializeField] private float targetVirtualHeight = 1.75f;

    [Header("Editor Testing")]
    [Tooltip("Simulated player height in editor (only used when not in VR)")]
    [SerializeField] private float editorSimulatedHeight = 1.7f;

    private void Start()
    {
        var xrOrigin = Object.FindAnyObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError($"[PlayerSpawn] No {nameof(XROrigin)} found.", this);
            return;
        }

        var cam = xrOrigin.Camera;
        if (cam == null)
        {
            xrOrigin.transform.SetPositionAndRotation(transform.position, transform.rotation);
            return;
        }

        ExecuteSpawn(xrOrigin, cam);
    }

    private void ExecuteSpawn(XROrigin xrOrigin, Camera cam)
    {
        Vector3 camLocalPos = cam.transform.localPosition;
        if (float.IsNaN(camLocalPos.x)) camLocalPos = Vector3.zero;

        float rawHeadsetHeight = camLocalPos.y;

        // Editor fallback: if no tracking data, use simulated height
        bool isVRActive = rawHeadsetHeight > 0.01f;
        if (!isVRActive)
        {
            rawHeadsetHeight = editorSimulatedHeight;
            Debug.Log($"[PlayerSpawn] Editor mode detected. Using simulated height: {editorSimulatedHeight:F2}m");
        }

        Vector3 rigToHeadOffset = cam.transform.TransformDirection(new Vector3(camLocalPos.x, 0f, camLocalPos.z));

        Vector3 targetPosition = transform.position - rigToHeadOffset;
        targetPosition.y = transform.position.y;
        
        xrOrigin.transform.position = targetPosition;

        Transform cameraOffset = FindCameraOffset(xrOrigin, cam);

        if (cameraOffset != null)
        {
            Vector3 offsetPos = cameraOffset.localPosition;
            offsetPos.y = targetVirtualHeight - rawHeadsetHeight;
            cameraOffset.localPosition = offsetPos;

            Debug.Log($"[PlayerSpawn] Raw headset: {rawHeadsetHeight:F2}m | Camera Offset Y: {offsetPos.y:F2}m | Final virtual: {targetVirtualHeight:F2}m");
        }
        else
        {
            Debug.LogWarning("[PlayerSpawn] Camera Offset not found. Virtual height normalization skipped.");
        }

        ApplyCorrectedRotation(xrOrigin, cam);
    }

    private Transform FindCameraOffset(XROrigin xrOrigin, Camera cam)
    {
        if (cam.transform.parent != null && cam.transform.parent.name.Contains("Camera"))
        {
            return cam.transform.parent;
        }

        return xrOrigin.transform.Find("Camera Offset");
    }

    private void ApplyCorrectedRotation(XROrigin xrOrigin, Camera cam)
    {
        float cameraY = cam.transform.eulerAngles.y;
        if (float.IsNaN(cameraY)) cameraY = 0f;

        float rigY = xrOrigin.transform.eulerAngles.y;
        float offsetRotation = cameraY - rigY;
        float targetY = transform.eulerAngles.y - offsetRotation;

        xrOrigin.transform.rotation = Quaternion.Euler(0f, targetY, 0f);
    }
}