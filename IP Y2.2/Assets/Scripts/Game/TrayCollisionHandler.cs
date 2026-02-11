using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class TrayCollisionHandler : MonoBehaviour
{
  [SerializeField] private TrayValidator trayValidator;

  private void OnCollisionEnter(Collision collision)
  {
    if (trayValidator == null) return;

    GameObject itemObj = collision.rigidbody != null ? collision.rigidbody.gameObject : collision.gameObject;
    if (itemObj == null) return;

    if (trayValidator.IsMoneyCollected(itemObj))
    {
      Rigidbody rb = itemObj.GetComponent<Rigidbody>();
      if (rb != null && !rb.isKinematic)
      {
        StartCoroutine(FreezeMoneyAfterDelay(rb, 0.5f));
      }
    }
  }

  private IEnumerator FreezeMoneyAfterDelay(Rigidbody rb, float delay)
  {
    yield return new WaitForSeconds(delay);

    if (rb == null) yield break;

    XRGrabInteractable grab = rb.GetComponent<XRGrabInteractable>();
    if (grab != null && grab.isSelected) yield break;

    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
    rb.useGravity = false;
    rb.isKinematic = true;
  }

  private void OnCollisionExit(Collision collision)
  {
    // No logic needed here; TrayValidator handles removal accounting
  }
}
