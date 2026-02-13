/// <summary>
/// File: MenuSetupBootstrap.cs
/// Author: Jayden Wong
/// Description: Instantiates the MenuSetup prefab on Awake if one does not already exist from a previous scene.
/// </summary>
using UnityEngine;

public class MenuSetupBootstrap : MonoBehaviour
{
  [SerializeField] private GameObject menuSetupPrefab;

  void Awake()
  {
    if (AuthUIController.Instance == null)
    {
      if (menuSetupPrefab != null)
      {
        Instantiate(menuSetupPrefab);
        Debug.Log("[Bootstrap] MenuSetup instantiated from prefab");
      }
      else
      {
        Debug.LogError("[Bootstrap] MenuSetup prefab not assigned!");
      }
    }
    else
    {
      Debug.Log("[Bootstrap] MenuSetup already exists (returning from game), skipping instantiation");
    }
  }
}
