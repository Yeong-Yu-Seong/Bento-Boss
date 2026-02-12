using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;

namespace BentoBoss.FirebaseManagers
{
  /// <summary>
  /// Handles database operations for user data
  /// Database path: users/{userId}
  /// </summary>
  public class DatabaseManager : MonoBehaviour
  {
    public static DatabaseManager Instance { get; private set; }

    private DatabaseReference _database;

    void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;
      DontDestroyOnLoad(gameObject);

      FirebaseManager.Instance.OnFirebaseReady += () =>
      {
        var firebaseDb = global::Firebase.Database.FirebaseDatabase.DefaultInstance;
        _database = firebaseDb.RootReference;
        Debug.Log("[Database] Ready");
      };
    }

    /// <summary>
    /// Save user data (email + username) after registration
    /// Path: users/{userId}
    /// </summary>
    public async Task<FirebaseResult<bool>> SaveUserData(string userId, string email, string username)
    {
      if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username))
        return new FirebaseResult<bool>(false, false, "Invalid user data");

      try
      {
        UserData userData = new UserData
        {
          email = email,
          username = username
        };

        await _database.Child("users").Child(userId).SetValueAsync(userData.ToDictionary());

        Debug.Log($"[Database] User saved: {email} ({username})");
        return new FirebaseResult<bool>(true, true);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Database] Save failed: {ex.Message}");
        return new FirebaseResult<bool>(false, false, ex.Message);
      }
    }

    /// <summary>
    /// Check if a username is already taken
    /// </summary>
    public async Task<FirebaseResult<bool>> CheckUsernameExists(string username)
    {
      if (string.IsNullOrEmpty(username))
        return new FirebaseResult<bool>(false, false, "Invalid username");

      try
      {
        var snapshot = await _database.Child("users")
            .OrderByChild("username")
            .EqualTo(username)
            .GetValueAsync();

        bool exists = snapshot.Exists && snapshot.ChildrenCount > 0;
        Debug.Log($"[Database] Username '{username}' exists: {exists}");
        return new FirebaseResult<bool>(true, exists);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Database] Username check failed: {ex.Message}");
        return new FirebaseResult<bool>(false, false, ex.Message);
      }
    }

    /// <summary>
    /// Fetch user data by userId
    /// Path: users/{userId}
    /// </summary>
    public async Task<FirebaseResult<UserData>> GetUserData(string userId)
    {
      if (string.IsNullOrEmpty(userId))
        return new FirebaseResult<UserData>(false, null, "Invalid userId");

      try
      {
        var snapshot = await _database.Child("users").Child(userId).GetValueAsync();

        if (!snapshot.Exists)
        {
          Debug.LogWarning($"[Database] No data for user: {userId}");
          return new FirebaseResult<UserData>(true, null);
        }

        UserData userData = new UserData
        {
          email = snapshot.Child("email").Value?.ToString(),
          username = snapshot.Child("username").Value?.ToString()
        };

        Debug.Log($"[Database] Fetched user: {userData.email} ({userData.username})");
        return new FirebaseResult<UserData>(true, userData);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Database] Fetch failed: {ex.Message}");
        return new FirebaseResult<UserData>(false, null, ex.Message);
      }
    }

    /// <summary>
    /// Save a complete game session with summary, inventory, and transaction history
    /// Path: sessions/{userId}/{sessionId}
    /// </summary>
    public async Task<FirebaseResult<bool>> SaveSessionData(string userId, string sessionId, FirebaseSessionData sessionData)
    {
      if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId) || sessionData == null)
        return new FirebaseResult<bool>(false, false, "Invalid session data");

      try
      {
        await _database.Child("sessions").Child(userId).Child(sessionId)
            .SetValueAsync(sessionData.ToDictionary());

        Debug.Log($"[Database] Session saved for user {userId}, key: {sessionId}");
        return new FirebaseResult<bool>(true, true);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Database] Session save failed: {ex.Message}");
        return new FirebaseResult<bool>(false, false, ex.Message);
      }
    }

    /// <summary>
    /// Push only the changed inventory field using UpdateChildrenAsync.
    /// Avoids overwriting all 8 fields when only 1 item changed.
    /// </summary>
    public async void PushInventoryFieldLive(string userId, string sessionId, string entryKey, int newValue)
    {
      try
      {
        var update = new Dictionary<string, object> { { entryKey, newValue } };
        await _database.Child("sessions").Child(userId).Child(sessionId)
            .Child("inventory_logs").UpdateChildrenAsync(update);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[Database] Live inventory field push failed: {ex.Message}");
      }
    }

    /// <summary>
    /// Push a single transaction entry in real-time
    /// Path: sessions/{userId}/{sessionId}/transaction_history/{orderId}
    /// </summary>
    public async void PushTransactionLive(string userId, string sessionId, string orderId, Dictionary<string, object> data)
    {
      try
      {
        await _database.Child("sessions").Child(userId).Child(sessionId)
            .Child("transaction_history").Child(orderId).SetValueAsync(data);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[Database] Live transaction push failed: {ex.Message}");
      }
    }

    /// <summary>
    /// Partially update session_summary fields without overwriting siblings
    /// Path: sessions/{userId}/{sessionId}/session_summary
    /// </summary>
    public async void PushSummaryFieldsLive(string userId, string sessionId, Dictionary<string, object> fields)
    {
      try
      {
        await _database.Child("sessions").Child(userId).Child(sessionId)
            .Child("session_summary").UpdateChildrenAsync(fields);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[Database] Live summary push failed: {ex.Message}");
      }
    }

    /// <summary>
    /// Push elapsed timer value in real-time
    /// Path: sessions/{userId}/{sessionId}/session_summary/total_time_seconds
    /// </summary>
    public async void PushTimerLive(string userId, string sessionId, float elapsed)
    {
      try
      {
        await _database.Child("sessions").Child(userId).Child(sessionId)
            .Child("session_summary").Child("total_time_seconds").SetValueAsync(elapsed);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[Database] Live timer push failed: {ex.Message}");
      }
    }

    /// <summary>
    /// Retrieves all sessions for a user and calculates aggregate statistics
    /// Path: sessions/{userId}
    /// </summary>
    public async Task<FirebaseResult<AggregateStats>> GetAggregateStats(string userId)
    {
      try
      {
        if (_database == null)
        {
          Debug.LogError("[DatabaseManager] Database not initialized");
          return new FirebaseResult<AggregateStats>(false, null, "Database not initialized");
        }

        // Query sessions/{userId}/
        DatabaseReference sessionsRef = _database.Child("sessions").Child(userId);
        var snapshot = await sessionsRef.GetValueAsync();

        // Initialize aggregate stats
        var stats = new AggregateStats
        {
          totalSessions = 0,
          bestScore = 0,
          recentScore = 0,
          bestGrade = "F",
          totalOrdersCompleted = 0,
          foodAccuracyPercent = 0f,
          changeAccuracyPercent = 0f,
          highestBalance = 0f,
          totalPlaytimeSeconds = 0f
        };

        // If no sessions exist, return zeros
        if (!snapshot.Exists || !snapshot.HasChildren)
        {
          Debug.Log($"[DatabaseManager] No sessions found for user {userId}");
          return new FirebaseResult<AggregateStats>(true, stats);
        }

        // Accumulators for accuracy calculation
        int totalFoodCorrect = 0;
        int totalFoodWrong = 0;
        int totalChangeCorrect = 0;
        int totalChangeWrong = 0;

        string mostRecentSessionId = "";

        // Iterate through all sessions
        foreach (var sessionSnapshot in snapshot.Children)
        {
          stats.totalSessions++;

          // Track most recent session (sessions are formatted yyyyMMdd_HHmm)
          string sessionId = sessionSnapshot.Key;
          if (string.Compare(sessionId, mostRecentSessionId) > 0)
          {
            mostRecentSessionId = sessionId;
          }

          var summary = sessionSnapshot.Child("session_summary");
          if (!summary.Exists) continue;

          // Parse values with safe fallbacks
          int score = summary.Child("final_score").Value != null
              ? int.Parse(summary.Child("final_score").Value.ToString())
              : 0;

          string grade = summary.Child("grade").Value?.ToString() ?? "F";

          float balance = summary.Child("final_balance").Value != null
              ? float.Parse(summary.Child("final_balance").Value.ToString())
              : 0f;

          float time = summary.Child("total_time_seconds").Value != null
              ? float.Parse(summary.Child("total_time_seconds").Value.ToString())
              : 0f;

          int foodCorrect = summary.Child("food_correct_count").Value != null
              ? int.Parse(summary.Child("food_correct_count").Value.ToString())
              : 0;

          int foodWrong = summary.Child("food_wrong_count").Value != null
              ? int.Parse(summary.Child("food_wrong_count").Value.ToString())
              : 0;

          int changeCorrect = summary.Child("change_correct_count").Value != null
              ? int.Parse(summary.Child("change_correct_count").Value.ToString())
              : 0;

          int changeWrong = summary.Child("change_wrong_count").Value != null
              ? int.Parse(summary.Child("change_wrong_count").Value.ToString())
              : 0;

          // Accumulate
          if (score > stats.bestScore)
            stats.bestScore = score;

          if (balance > stats.highestBalance)
            stats.highestBalance = balance;

          if (GetGradePriority(grade) > GetGradePriority(stats.bestGrade))
            stats.bestGrade = grade;

          stats.totalPlaytimeSeconds += time;
          stats.totalOrdersCompleted += (foodCorrect + foodWrong);

          totalFoodCorrect += foodCorrect;
          totalFoodWrong += foodWrong;
          totalChangeCorrect += changeCorrect;
          totalChangeWrong += changeWrong;

          // If this is the most recent session, store its score
          if (sessionId == mostRecentSessionId)
          {
            stats.recentScore = score;
          }
        }

        // Calculate accuracy percentages
        int totalFoodOrders = totalFoodCorrect + totalFoodWrong;
        if (totalFoodOrders > 0)
          stats.foodAccuracyPercent = (float)totalFoodCorrect / totalFoodOrders * 100f;

        int totalChangeOrders = totalChangeCorrect + totalChangeWrong;
        if (totalChangeOrders > 0)
          stats.changeAccuracyPercent = (float)totalChangeCorrect / totalChangeOrders * 100f;

        Debug.Log($"[DatabaseManager] Retrieved aggregate stats for {userId}: {stats.totalSessions} sessions");

        return new FirebaseResult<AggregateStats>(true, stats);
      }
      catch (Exception e)
      {
        Debug.LogError($"[DatabaseManager] Error getting aggregate stats: {e.Message}");
        return new FirebaseResult<AggregateStats>(false, null, e.Message);
      }
    }

    /// <summary>
    /// Helper method to compare grade priority (S > A > B > C > D > F)
    /// </summary>
    private static int GetGradePriority(string grade)
    {
      switch (grade?.ToUpper())
      {
        case "S": return 6;
        case "A": return 5;
        case "B": return 4;
        case "C": return 3;
        case "D": return 2;
        case "F": return 1;
        default: return 0;
      }
    }
  }
}
