/// <summary>
/// File: BentoMilestoneUnlock.cs
/// Author: Jayden Wong
/// Description: Reveals bento restock crates once the player's profit reaches the milestone amount.
/// </summary>
using UnityEngine;

public class BentoMilestoneUnlock : MonoBehaviour
{
  [Header("Milestone Settings")]
  [SerializeField] private float milestoneAmount = 15f;

  [Header("Bento Restock Crates")]
  [Tooltip("Assign the bento restock crate GameObjects to reveal")]
  [SerializeField] private GameObject[] bentoCrates;

  private bool _unlocked = false;

  private void Start()
  {
    foreach (var crate in bentoCrates)
    {
      if (crate != null) crate.SetActive(false);
    }
  }

  private void Update()
  {
    if (_unlocked) return;
    if (EarningsTracker.Instance == null) return;
    if (EarningsTracker.Instance.CurrentProfit < milestoneAmount) return;

    _unlocked = true;
    foreach (var crate in bentoCrates)
    {
      if (crate != null) crate.SetActive(true);
    }
  }
}
