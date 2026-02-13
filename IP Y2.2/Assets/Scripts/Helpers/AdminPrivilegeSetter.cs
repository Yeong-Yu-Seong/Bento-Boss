/// <summary>
/// File: AdminPrivilegeSetter.cs
/// Author: Jayden Wong
/// Description: One-time utility that sets the isAdmin flag for a specific user in the Firebase Realtime Database.
/// </summary>
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AdminPrivilegeSetter : MonoBehaviour
{
  private DatabaseReference _dbReference;

  private void Start()
  {
    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
    {
      DependencyStatus dependencyStatus = task.Result;
      if (dependencyStatus == DependencyStatus.Available)
      {
        _dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        SetAdminStatus("9VCARaSv99UUp71C6G8XzCRRj0H3", true);
      }
      else
      {
        Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
      }
    });
  }

  /// <summary>
  /// Updates the isAdmin field for a specific user without overwriting other fields
  /// </summary>
  private void SetAdminStatus(string uid, bool status)
  {
    if (string.IsNullOrEmpty(uid)) return;

    DatabaseReference userRef = _dbReference.Child("users").Child(uid);

    // Partial update so email/username fields are left intact
    Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "isAdmin", status }
        };

    userRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
    {
      if (task.IsCompletedSuccessfully)
      {
        Debug.Log($"[Firebase] Successfully set isAdmin to {status} for UID: {uid}");
      }
      else
      {
        Debug.LogError($"[Firebase] Failed to update admin status: {task.Exception}");
      }
    });
  }
}
