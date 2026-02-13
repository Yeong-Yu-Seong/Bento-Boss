/// <summary>
/// File: FirebaseManager.cs
/// Author: Jayden Wong
/// Description: Initializes the Firebase SDK and signals readiness to dependent managers.
/// </summary>
using System;
using UnityEngine;
using Firebase;
using Firebase.Extensions;

namespace BentoBoss.FirebaseManagers
{
  public class FirebaseManager : MonoBehaviour
  {
    public static FirebaseManager Instance { get; private set; }

    public event Action OnFirebaseReady;
    public bool IsReady { get; private set; }

    void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;
      DontDestroyOnLoad(gameObject);

      InitializeFirebase();
    }

    void InitializeFirebase()
    {
      FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
      {
        if (task.Result == DependencyStatus.Available)
        {
          IsReady = true;
          Debug.Log("[Firebase] Initialized successfully");
          OnFirebaseReady?.Invoke();
        }
        else
        {
          Debug.LogError($"[Firebase] Failed to initialize: {task.Result}");
        }
      });
    }
  }
}
