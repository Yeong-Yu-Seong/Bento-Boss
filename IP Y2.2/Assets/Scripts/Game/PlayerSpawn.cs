/// <summary>
/// File: PlayerSpawn.cs
/// Author: Jayden Wong
/// Description: Moves the XR Origin to the spawn point, compensating for headset camera offset and rotation.
/// </summary>
using UnityEngine;
using Unity.XR.CoreUtils;

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
    // Default to zero offset if camera data is NaN to prevent UI Frustum errors
    Vector3 camLocalPos = cam.transform.localPosition;
    if (float.IsNaN(camLocalPos.x)) camLocalPos = Vector3.zero;

    Vector3 rigToHeadOffset = cam.transform.TransformDirection(camLocalPos);
    rigToHeadOffset.y = 0f;

    xrOrigin.transform.position = transform.position - rigToHeadOffset;

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
