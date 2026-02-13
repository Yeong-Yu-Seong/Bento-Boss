/// <summary>
/// File: SocketFollowDrawer.cs
/// Author: Jayden Wong
/// Description: Makes an XR socket interactor follow a drawer transform by maintaining a local offset.
/// </summary>
using UnityEngine;

public class SocketFollowDrawer : MonoBehaviour
{
  public Transform drawerTransform;
  private Vector3 localOffset;
  private Quaternion localRotation;

  void Start()
  {
    if (drawerTransform == null)
    {
      Debug.LogError("SocketFollowDrawer: drawerTransform is not assigned!");
      return;
    }

    localOffset = drawerTransform.InverseTransformPoint(transform.position);
    localRotation = Quaternion.Inverse(drawerTransform.rotation) * transform.rotation;
  }

  void LateUpdate()
  {
    if (drawerTransform == null) return;

    transform.position = drawerTransform.TransformPoint(localOffset);
    transform.rotation = drawerTransform.rotation * localRotation;
  }
}
