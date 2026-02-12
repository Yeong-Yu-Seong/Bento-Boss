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
    // Strict null checks omitted for brevity, assuming previous setup
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

    // 1. Tunneling Check
    if (distance > 0.001f)
    {
      if (Physics.SphereCast(_lastValidHeadPosition, checkRadius, movementDir.normalized, out RaycastHit hit, distance, barrierLayers, QueryTriggerInteraction.Ignore))
      {
        Debug.LogWarning($"[Bounds] Tunneling detected! Hit {hit.collider.name}.");
        isInvalid = true;
      }
    }

    // 2. Static & Bounds Check
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
      // CRITICAL FIX: Push the Rig back so the Head returns to the last valid world coordinate
      PushBackToSafety(_lastValidHeadPosition, currentHeadPos);
    }
    else
    {
      // Only update valid position if we are actually safe
      _lastValidHeadPosition = currentHeadPos;
    }
  }

  private void PushBackToSafety(Vector3 targetHeadWorldPos, Vector3 currentHeadWorldPos)
  {
    // Calculate how far the head moved into danger
    Vector3 offset = targetHeadWorldPos - currentHeadWorldPos;

    // Zero out Y to prevent the rig from flying up/down if you look down
    offset.y = 0;

    if (_characterController != null) _characterController.enabled = false;

    // Move the Rig by the offset required to put the head back at the safe spot
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
