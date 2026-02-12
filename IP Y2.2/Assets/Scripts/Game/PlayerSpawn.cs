using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Moves the XR Origin to the spawn point. 
/// Handles cases where no headset is present to prevent NaN transform corruption.
/// </summary>
[DefaultExecutionOrder(100)]
public class PlayerSpawn : MonoBehaviour
{
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
    // If data is invalid/NaN, default to zero offset to prevent UI Frustum errors.
    Vector3 camLocalPos = cam.transform.localPosition;
    if (float.IsNaN(camLocalPos.x)) camLocalPos = Vector3.zero;

    // Calculate horizontal offset
    Vector3 rigToHeadOffset = cam.transform.TransformDirection(camLocalPos);
    rigToHeadOffset.y = 0f;

    // Apply Position
    xrOrigin.transform.position = transform.position - rigToHeadOffset;

    // Apply Rotation
    ApplyCorrectedRotation(xrOrigin, cam);
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