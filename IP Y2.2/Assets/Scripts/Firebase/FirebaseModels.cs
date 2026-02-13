/// <summary>
/// File: FirebaseModels.cs
/// Author: Jayden Wong
/// Description: Data models for Firebase storage including session summaries, inventory logs, transactions, and aggregate statistics.
/// </summary>
using System;
using System.Collections.Generic;

namespace BentoBoss.FirebaseManagers
{
  [Serializable]
  public class FirebaseResult<T>
  {
    public bool Success;
    public string ErrorMessage;
    public T Data;

    public FirebaseResult(bool success, T data = default, string error = "")
    {
      Success = success;
      Data = data;
      ErrorMessage = error;
    }
  }

  [Serializable]
  public class UserData
  {
    public string email;
    public string username;

    public Dictionary<string, object> ToDictionary()
    {
      return new Dictionary<string, object>
            {
                { "email", email },
                { "username", username }
            };
    }
  }

  [Serializable]
  public class SessionSummary
  {
    public float total_time_seconds;
    public float final_balance;
    public bool is_bento_unlocked;
    public int trash_disposed;
    public int food_correct_count;
    public int food_wrong_count;
    public int change_correct_count;
    public int change_wrong_count;
    public int final_score;
    public string grade;
    public string completed_at;

    /// <summary>
    /// Converts to dictionary with floats rounded to 2dp for clean monetary/time values in Firebase
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
      return new Dictionary<string, object>
      {
        { "total_time_seconds", (float)Math.Round(total_time_seconds, 2) },
        { "final_balance", (float)Math.Round(final_balance, 2) },
        { "is_bento_unlocked", is_bento_unlocked },
        { "trash_disposed", trash_disposed },
        { "food_correct_count", food_correct_count },
        { "food_wrong_count", food_wrong_count },
        { "change_correct_count", change_correct_count },
        { "change_wrong_count", change_wrong_count },
        { "final_score", final_score },
        { "grade", grade },
        { "completed_at", completed_at }
      };
    }
  }

  [Serializable]
  public class InventoryLog
  {
    public int apple_count;
    public int banana_count;
    public int orange_count;
    public int strawberry_count;
    public int bento_set_1_count;
    public int bento_set_2_count;
    public int blueberry_drink_count;
    public int green_tea_count;

    public Dictionary<string, object> ToDictionary()
    {
      return new Dictionary<string, object>
      {
        { "apple_count", apple_count },
        { "banana_count", banana_count },
        { "orange_count", orange_count },
        { "strawberry_count", strawberry_count },
        { "bento_set_1_count", bento_set_1_count },
        { "bento_set_2_count", bento_set_2_count },
        { "blueberry_drink_count", blueberry_drink_count },
        { "green_tea_count", green_tea_count }
      };
    }
  }

  [Serializable]
  public class TransactionEntry
  {
    public string order_id;
    public string requested_food;
    public int requested_food_qty;
    public string requested_drink;
    public int requested_drink_qty;
    public bool is_correct_item;
    public float order_cost;
    public float amount_paid;
    public float change_given;
    public bool is_change_correct;

    /// <summary>
    /// Converts to dictionary with floats rounded to 2dp for clean monetary values in Firebase
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
      return new Dictionary<string, object>
      {
        { "order_id", order_id },
        { "requested_food", requested_food },
        { "requested_food_qty", requested_food_qty },
        { "requested_drink", requested_drink },
        { "requested_drink_qty", requested_drink_qty },
        { "is_correct_item", is_correct_item },
        { "order_cost", (float)Math.Round(order_cost, 2) },
        { "amount_paid", (float)Math.Round(amount_paid, 2) },
        { "change_given", (float)Math.Round(change_given, 2) },
        { "is_change_correct", is_change_correct }
      };
    }
  }

  [Serializable]
  public class FirebaseSessionData
  {
    public SessionSummary session_summary;
    public InventoryLog inventory_logs;
    public List<TransactionEntry> transaction_history;

    /// <summary>
    /// Converts full session to dictionary, keying transactions by their order_id
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
      var transactionDict = new Dictionary<string, object>();
      if (transaction_history != null)
      {
        foreach (var entry in transaction_history)
        {
          transactionDict[entry.order_id] = entry.ToDictionary();
        }
      }

      return new Dictionary<string, object>
      {
        { "session_summary", session_summary.ToDictionary() },
        { "inventory_logs", inventory_logs.ToDictionary() },
        { "transaction_history", transactionDict }
      };
    }
  }

  [Serializable]
  public class AggregateStats
  {
    public int totalSessions;
    public int bestScore;
    public int recentScore;
    public string bestGrade;
    public int totalOrdersCompleted;
    public float foodAccuracyPercent;
    public float changeAccuracyPercent;
    public float highestBalance;
    public float totalPlaytimeSeconds;

    /// <summary>
    /// Formats playtime as "Xh Ym" or "Ym" for UI display
    /// </summary>
    public string GetFormattedPlaytime()
    {
      int totalMinutes = UnityEngine.Mathf.FloorToInt(totalPlaytimeSeconds / 60f);
      int hours = totalMinutes / 60;
      int minutes = totalMinutes % 60;

      if (hours > 0)
        return $"{hours}h {minutes}m";
      else
        return $"{minutes}m";
    }
  }
}
