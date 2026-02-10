using UnityEngine;

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
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (trayValidator == null) return;

        GameObject itemObj = collision.rigidbody != null ? collision.rigidbody.gameObject : collision.gameObject;
        if (itemObj == null) return;

        if (trayValidator.IsMoneyCollected(itemObj))
        {
            Rigidbody rb = itemObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}