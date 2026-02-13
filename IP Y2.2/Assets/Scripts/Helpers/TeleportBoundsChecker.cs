/// <summary>
/// File: TeleportBoundsChecker.cs
/// Author: Jayden Wong
/// Description: Prevents the player's head from passing through walls or leaving the play area by pushing the XR Origin back.
/// </summary>
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportBoundsChecker : MonoBehaviour
{
  [Header("Core References")]
  [SerializeField] private Transform xrOrigin;
  [SerializeField] private Transform headCamera;
  [SerializeField] private BoxCollider playAreaBounds;

  [Header("Collision Settings")]
  [SerializeField] private LayerMask barrierLayers;
  [SerializeField] private float checkRadius = 0.25f;

  private Vector3 _lastValidHeadPosition;
  private CharacterController _characterController;
  private bool _isInitialized = false;

  private void Awake()
  {
    if (xrOrigin == null) xrOrigin = transform;
    _characterController = xrOrigin.GetComponent<CharacterController>();

    _lastValidHeadPosition = headCamera.position;
    _isInitialized = true;
  }

  private void LateUpdate()
  {
    if (!_isInitialized) return;

    Vector3 currentHeadPos = headCamera.position;
    Vector3 movementDir = currentHeadPos - _lastValidHeadPosition;
    float distance = movementDir.magnitude;
    bool isInvalid = false;

    // SphereCast along movement direction to catch fast head movements that skip frames
    if (distance > 0.001f)
    {
      if (Physics.SphereCast(_lastValidHeadPosition, checkRadius, movementDir.normalized, out RaycastHit hit, distance, barrierLayers, QueryTriggerInteraction.Ignore))
      {
        Debug.LogWarning($"[Bounds] Tunneling detected! Hit {hit.collider.name}.");
        isInvalid = true;
      }
    }

    if (!isInvalid)
    {
      bool insideWall = Physics.CheckSphere(currentHeadPos, checkRadius, barrierLayers, QueryTriggerInteraction.Ignore);
      bool outsideZone = !IsPointInsideOBB(playAreaBounds, currentHeadPos);

      if (insideWall || outsideZone)
      {
        isInvalid = true;
      }
    }

    if (isInvalid)
    {
      PushBackToSafety(_lastValidHeadPosition, currentHeadPos);
    }
    else
    {
      _lastValidHeadPosition = currentHeadPos;
    }
  }

  private void PushBackToSafety(Vector3 targetHeadWorldPos, Vector3 currentHeadWorldPos)
  {
    Vector3 offset = targetHeadWorldPos - currentHeadWorldPos;

    // Zero out Y to prevent the rig from flying up/down when looking vertically
    offset.y = 0;

    if (_characterController != null) _characterController.enabled = false;

    xrOrigin.position += offset;

    if (_characterController != null) _characterController.enabled = true;
  }

  private bool IsPointInsideOBB(BoxCollider box, Vector3 worldPoint)
  {
    Vector3 localPoint = box.transform.InverseTransformPoint(worldPoint);
    Vector3 pointRelativeToCenter = localPoint - box.center;
    Vector3 halfSize = box.size * 0.5f;

    return Mathf.Abs(pointRelativeToCenter.x) <= halfSize.x &&
           Mathf.Abs(pointRelativeToCenter.y) <= halfSize.y &&
           Mathf.Abs(pointRelativeToCenter.z) <= halfSize.z;
  }
}
