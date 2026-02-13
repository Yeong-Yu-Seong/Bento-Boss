/// <summary>
/// File: trashcan.cs
/// Author: Jayden Wong
/// Description: Detects items tagged as Trash entering the trigger and delegates disposal to the stock manager.
/// </summary>
using UnityEngine;

public class TrashCan : MonoBehaviour
{
  public InventoryStockDisplay stockManager;

  void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("Trash"))
    {
      if (stockManager != null)
      {
        stockManager.DisposeTrash(other.gameObject);
      }
      else
      {
        Destroy(other.gameObject);
      }
    }
  }
}
