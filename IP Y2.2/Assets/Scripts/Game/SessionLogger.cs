using UnityEngine;
using System;
using System.Collections.Generic;
using BentoBoss.FirebaseManagers;

/// <summary>
/// Central session logging orchestrator.
/// Pushes inventory, transactions, balance, and timer to Firebase in real-time.
/// Does a final complete write on EndSession().
/// </summary>
public class SessionLogger : MonoBehaviour
{
  public static SessionLogger Instance { get; private set; }

  [Header("Dependencies")]
  [SerializeField] private InventoryStockDisplay inventoryDisplay;

  private float _sessionStartTime;
  private string _sessionTimestamp;
  private string _userId;
  private List<TransactionEntry> _transactions = new List<TransactionEntry>();
  private int _orderCounter = 0;
  private bool _sessionEnded = false;

  private float _lastTimerPush;
  private const float TIMER_PUSH_INTERVAL = 10f;

  // Cached inventory counts for diff-based pushes — only changed fields go to Firebase
  private Dictionary<string, int> _lastInventoryCounts = new Dictionary<string, int>();
  private int _lastTrashCount = -1;

  // Maps Unity tags to Firebase inventory_logs field names
  private static readonly Dictionary<string, string> TagToFirebaseKey = new Dictionary<string, string>
  {
    { "Apple", "apple_count" },
    { "Banana", "banana_count" },
    { "Orange", "orange_count" },
    { "Strawberry", "strawberry_count" },
    { "Bento1", "bento_set_1_count" },
    { "Bento2", "bento_set_2_count" },
    { "Blueberry", "blueberry_drink_count" },
    { "GreenTea", "green_tea_count" }
  };

  private bool IsReady => !string.IsNullOrEmpty(_userId) && !string.IsNullOrEmpty(_sessionTimestamp)
      && DatabaseManager.Instance != null;

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
  }

  private void Start()
  {
    _sessionStartTime = Time.time;
    _lastTimerPush = Time.time;
    _sessionTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
    _userId = AuthManager.Instance?.CurrentUser?.UserId;

    // Push initial session_summary to create the node in Firebase immediately
    if (IsReady)
    {
      var initialFields = new Dictionary<string, object>
      {
        { "total_time_seconds", 0f },
        { "final_balance", 0f },
        { "is_bento_unlocked", false },
        { "trash_disposed", 0 },
        { "completed_at", "" }
      };
      DatabaseManager.Instance.PushSummaryFieldsLive(_userId, _sessionTimestamp, initialFields);
    }
  }

  private void Update()
  {
    if (_sessionEnded) return;

    // Retry userId cache if it wasn't available at Start
    if (string.IsNullOrEmpty(_userId))
    {
      _userId = AuthManager.Instance?.CurrentUser?.UserId;
    }

    // Push timer every 10 seconds
    if (IsReady && Time.time - _lastTimerPush >= TIMER_PUSH_INTERVAL)
    {
      _lastTimerPush = Time.time;
      float elapsed = (float)Math.Round(Time.time - _sessionStartTime, 2);
      DatabaseManager.Instance.PushTimerLive(_userId, _sessionTimestamp, elapsed);
    }
  }

  /// <summary>
  /// Log a completed transaction and push it to Firebase immediately.
  /// Called from PaymentHandler after each successful order.
  /// </summary>
  public void LogTransaction(string food, int foodQty, string drink, int drinkQty,
      bool correctItem, float orderCost, float amountPaid, float changeGiven, bool changeCorrect)
  {
    _orderCounter++;

    var entry = new TransactionEntry
    {
      order_id = $"order_{_orderCounter:D2}",
      requested_food = food,
      requested_food_qty = foodQty,
      requested_drink = drink,
      requested_drink_qty = drinkQty,
      is_correct_item = correctItem,
      order_cost = orderCost,
      amount_paid = amountPaid,
      change_given = changeGiven,
      is_change_correct = changeCorrect
    };

    _transactions.Add(entry);
    Debug.Log($"[SessionLogger] Transaction logged: {entry.order_id} - {food} x{foodQty}, {drink} x{drinkQty}, ${orderCost:F2}");

    // Push to Firebase immediately
    if (IsReady)
    {
      DatabaseManager.Instance.PushTransactionLive(_userId, _sessionTimestamp, entry.order_id, entry.ToDictionary());
    }
  }

  /// <summary>
  /// Diff-based inventory push — only sends changed fields to Firebase.
  /// Compares current counts against cached values, pushes individual fields via UpdateChildrenAsync.
  /// </summary>
  public void PushInventoryNow()
  {
    if (!IsReady || inventoryDisplay == null) return;

    var counts = inventoryDisplay.CountFoodByType();

    foreach (var kvp in TagToFirebaseKey)
    {
      string tag = kvp.Key;
      string firebaseKey = kvp.Value;

      int currentCount = counts.ContainsKey(tag) ? counts[tag] : 0;
      _lastInventoryCounts.TryGetValue(tag, out int lastCount);

      if (currentCount != lastCount)
      {
        _lastInventoryCounts[tag] = currentCount;
        DatabaseManager.Instance.PushInventoryFieldLive(_userId, _sessionTimestamp, firebaseKey, currentCount);
      }
    }

    // Push trash count only if changed
    int trashCount = inventoryDisplay.TrashDisposed;
    if (trashCount != _lastTrashCount)
    {
      _lastTrashCount = trashCount;
      var trashField = new Dictionary<string, object> { { "trash_disposed", trashCount } };
      DatabaseManager.Instance.PushSummaryFieldsLive(_userId, _sessionTimestamp, trashField);
    }
  }

  /// <summary>
  /// Push current balance and bento unlock status to Firebase.
  /// Called from EarningsTracker when profit changes.
  /// </summary>
  public void PushBalanceNow(float balance)
  {
    if (!IsReady) return;

    var fields = new Dictionary<string, object>
    {
      { "final_balance", (float)Math.Round(balance, 2) },
      { "is_bento_unlocked", balance >= 15f }
    };
    DatabaseManager.Instance.PushSummaryFieldsLive(_userId, _sessionTimestamp, fields);
  }

  /// <summary>
  /// End the session and push final complete snapshot to Firebase.
  /// Called from EarningsTracker.OnGoalReached().
  /// </summary>
  public async void EndSession()
  {
    if (_sessionEnded) return;
    _sessionEnded = true;

    float elapsed = Time.time - _sessionStartTime;
    float finalBalance = EarningsTracker.Instance != null ? EarningsTracker.Instance.CurrentProfit : 0f;
    bool bentoUnlocked = finalBalance >= 15f;

    // Gather inventory snapshot
    int trashCount = 0;
    var inventoryLog = new InventoryLog();

    if (inventoryDisplay != null)
    {
      trashCount = inventoryDisplay.TrashDisposed;

      var counts = inventoryDisplay.CountFoodByType();
      if (counts.TryGetValue("Apple", out int apple)) inventoryLog.apple_count = apple;
      if (counts.TryGetValue("Banana", out int banana)) inventoryLog.banana_count = banana;
      if (counts.TryGetValue("Orange", out int orange)) inventoryLog.orange_count = orange;
      if (counts.TryGetValue("Strawberry", out int strawberry)) inventoryLog.strawberry_count = strawberry;
      if (counts.TryGetValue("Bento1", out int bento1)) inventoryLog.bento_set_1_count = bento1;
      if (counts.TryGetValue("Bento2", out int bento2)) inventoryLog.bento_set_2_count = bento2;
      if (counts.TryGetValue("Blueberry", out int blueberry)) inventoryLog.blueberry_drink_count = blueberry;
      if (counts.TryGetValue("GreenTea", out int greenTea)) inventoryLog.green_tea_count = greenTea;
    }

    var sessionData = new FirebaseSessionData
    {
      session_summary = new SessionSummary
      {
        total_time_seconds = elapsed,
        final_balance = finalBalance,
        is_bento_unlocked = bentoUnlocked,
        trash_disposed = trashCount,
        completed_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
      },
      inventory_logs = inventoryLog,
      transaction_history = _transactions
    };

    // Get current user ID
    string userId = !string.IsNullOrEmpty(_userId) ? _userId : AuthManager.Instance?.CurrentUser?.UserId;
    if (string.IsNullOrEmpty(userId))
    {
      Debug.LogError("[SessionLogger] No authenticated user — session not saved");
      return;
    }

    var result = await DatabaseManager.Instance.SaveSessionData(userId, _sessionTimestamp, sessionData);

    if (result.Success)
    {
      Debug.Log($"[SessionLogger] Session saved — {_transactions.Count} transactions, {elapsed:F1}s, ${finalBalance:F2}");
    }
    else
    {
      Debug.LogError($"[SessionLogger] Failed to save session: {result.ErrorMessage}");
    }
  }
}
