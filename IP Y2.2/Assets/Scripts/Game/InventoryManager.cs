/// <summary>
/// File: InventoryManager.cs
/// Author: Jayden Wong
/// Description: Tracks item counts across XR sockets, updates stock display UI, and handles trash disposal.
/// </summary>
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

public class InventoryStockDisplay : MonoBehaviour
{
  [Header("--- Stock Counting ---")]
  public List<XRSocketInteractor> mySockets;
  public TextMeshProUGUI stockTextLeft;
  public TextMeshProUGUI stockTextRight;

  private string[] leftColumnFoods = { "Apple", "Banana", "Orange", "Strawberry" };

  private string[] rightColumnFoods = { "Bento1", "Bento2", "Blueberry", "GreenTea" };

  private Dictionary<string, string> displayNames = new Dictionary<string, string>
    {
        { "Bento1", "BentoSet 1" },
        { "Bento2", "BentoSet 2" }
    };

  [Header("--- Trash Disposal ---")]
  public TextMeshProUGUI trashCountText;
  private int trashDisposed = 0;
  public int TrashDisposed => trashDisposed;

  void Start()
  {
    foreach (var socket in mySockets)
    {
      if (socket != null)
      {
        socket.selectEntered.AddListener(OnSocketChanged);
        socket.selectExited.AddListener(OnSocketChanged);
      }
    }

    UpdateStockDisplay();
    UpdateTrashDisplay();
  }

  void OnDisable()
  {
    foreach (var socket in mySockets)
    {
      if (socket != null)
      {
        socket.selectEntered.RemoveListener(OnSocketChanged);
        socket.selectExited.RemoveListener(OnSocketChanged);
      }
    }
  }

  void OnSocketChanged(BaseInteractionEventArgs args)
  {
    UpdateStockDisplay();
    if (SessionLogger.Instance != null) SessionLogger.Instance.PushInventoryNow();
  }

  /// <summary>
  /// Returns a dictionary of item tag names to their current count across all sockets
  /// </summary>
  public Dictionary<string, int> CountFoodByType()
  {
    var counts = new Dictionary<string, int>();

    foreach (var food in leftColumnFoods)
    {
      counts[food] = 0;
    }
    foreach (var food in rightColumnFoods)
    {
      counts[food] = 0;
    }

    foreach (var socket in mySockets)
    {
      if (socket == null || !socket.hasSelection) continue;

      var selected = socket.interactablesSelected;
      if (selected.Count == 0) continue;

      GameObject obj = (selected[0] as MonoBehaviour)?.gameObject;
      if (obj == null) continue;

      string tag = obj.tag;
      if (counts.ContainsKey(tag))
      {
        counts[tag]++;
      }
    }

    return counts;
  }

  void UpdateStockDisplay()
  {
    var counts = CountFoodByType();

    UpdateColumnText(stockTextLeft, leftColumnFoods, counts);
    UpdateColumnText(stockTextRight, rightColumnFoods, counts);
  }

  void UpdateColumnText(TextMeshProUGUI textComponent, string[] foodList, Dictionary<string, int> counts)
  {
    if (textComponent == null) return;

    var lines = new List<string>();

    foreach (var food in foodList)
    {
      int count = counts[food];
      string displayName = displayNames.ContainsKey(food) ? displayNames[food] : food;

      string colorTag = count == 0 ? "<color=red>" : "";
      string closeTag = count == 0 ? "</color>" : "";

      lines.Add($"{colorTag}{displayName}: {count}{closeTag}");
    }

    textComponent.text = string.Join("\n", lines);
  }

  void UpdateTrashDisplay()
  {
    if (trashCountText != null)
    {
      trashCountText.text = $"Trash Disposed: {trashDisposed}";
    }
  }

  /// <summary>
  /// Increments trash count, updates UI, pushes inventory to Firebase, and destroys the object
  /// </summary>
  public void DisposeTrash(GameObject trashedObject)
  {
    if (trashedObject == null) return;

    trashDisposed++;
    UpdateTrashDisplay();
    if (SessionLogger.Instance != null) SessionLogger.Instance.PushInventoryNow();
    Destroy(trashedObject);
  }
}
