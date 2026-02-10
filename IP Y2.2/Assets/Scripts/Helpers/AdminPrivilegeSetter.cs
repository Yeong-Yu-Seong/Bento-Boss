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
        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Initialize the database reference using the URL from your config
                _dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                
                // Execute the update for your specific admin UID
                SetAdminStatus("9VCARaSv99UUp71C6G8XzCRRj0H3", true);
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    /// <summary>
    /// Updates the isAdmin field for a specific user in the Realtime Database.
    /// </summary>
    /// <param name="uid">The unique user ID.</param>
    /// <param name="status">The boolean value for isAdmin.</param>
    private void SetAdminStatus(string uid, bool status)
    {
        if (string.IsNullOrEmpty(uid)) return;

        // Reference: users/<uid>
        DatabaseReference userRef = _dbReference.Child("users").Child(uid);

        // Use a Dictionary to perform a partial update (leaves email/username intact)
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "isAdmin", status }
        };

        userRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(task => {
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