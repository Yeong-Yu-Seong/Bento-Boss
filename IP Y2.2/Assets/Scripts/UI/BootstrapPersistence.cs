using UnityEngine;

public class BootstrapPersistence : MonoBehaviour
{
  private static BootstrapPersistence _instance;

  void Awake()
  {
    if (_instance != null && _instance != this)
    {
      Destroy(gameObject);
      return;
    }

    _instance = this;
    DontDestroyOnLoad(gameObject);
    Debug.Log("[Bootstrap] Made persistent");
  }
}
