/// <summary>
/// File: XRCameraCollision.cs
/// Author: Jayden Wong
/// Description: Pushes the XR Origin away from walls when the VR camera overlaps with collision geometry.
/// </summary>
using UnityEngine;

public class XRCameraCollision : MonoBehaviour
{
  [SerializeField] private CharacterController characterController;
  [SerializeField] private Transform mainCamera;
  [SerializeField] private LayerMask collisionLayers;
  [SerializeField] private float cameraColliderRadius = 0.2f;
  [SerializeField] private float minDistanceFromWall = 0.3f;

  private Vector3 previousValidPosition;

  private void Start()
  {
    characterController = GetComponent<CharacterController>();

    if (mainCamera == null)
    {
      mainCamera = transform.Find("Camera Offset/Main Camera");

      if (mainCamera == null)
      {
        Debug.LogError("Main Camera not found. Assign manually in Inspector.");
      }
    }

    previousValidPosition = transform.position;
  }

  private void LateUpdate()
  {
    if (mainCamera == null || characterController == null)
      return;

    Vector3 cameraWorldPosition = mainCamera.position;

    Collider[] hitColliders = Physics.OverlapSphere(cameraWorldPosition, cameraColliderRadius, collisionLayers, QueryTriggerInteraction.Ignore);

    if (hitColliders.Length > 0)
    {
      Vector3 pushDirection = Vector3.zero;

      foreach (Collider col in hitColliders)
      {
        Vector3 closestPoint = col.ClosestPoint(cameraWorldPosition);
        Vector3 directionFromWall = cameraWorldPosition - closestPoint;
        float distanceToWall = directionFromWall.magnitude;

        if (distanceToWall < minDistanceFromWall)
        {
          float pushAmount = minDistanceFromWall - distanceToWall;
          pushDirection += directionFromWall.normalized * pushAmount;
        }
      }

      if (pushDirection.magnitude > 0.001f)
      {
        // Disable CharacterController temporarily so we can move the transform directly
        characterController.enabled = false;
        transform.position += pushDirection;
        characterController.enabled = true;

        previousValidPosition = transform.position;
      }
    }
    else
    {
      previousValidPosition = transform.position;
    }
  }

  private void OnDrawGizmosSelected()
  {
    if (mainCamera != null)
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(mainCamera.position, cameraColliderRadius);

      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(mainCamera.position, minDistanceFromWall);
    }
  }
}
