using UnityEngine;

public class TrashCan : MonoBehaviour
{
    public InventoryStockDisplay stockManager;
    
    void OnTriggerEnter(Collider other)
    {
        // Check if the object has the "Trash" tag
        if (other.CompareTag("Trash"))
        {
            if (stockManager != null)
            {
                stockManager.DisposeTrash(other.gameObject);
            }
            else
            {
                // If no stock manager, just destroy it
                Destroy(other.gameObject);
            }
        }
    }
}