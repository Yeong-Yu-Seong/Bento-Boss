using UnityEngine;
using System.Collections;

public class PaymentHandler : MonoBehaviour
{
  [Header("Dependencies")]
  [SerializeField] private OrderBubbleController orderBubbleController;
  [SerializeField] private OrderProgressUI orderProgressUI;
  [SerializeField] private TrayValidator trayValidator;
  [SerializeField] private GameObject paymentCollectorObject;

  [Header("Money Spawning")]
  [SerializeField] private Transform moneySpawnPoint;
  [SerializeField] private GameObject[] moneyPrefabs;

  [Header("Settings")]
  [SerializeField] private float paymentDisplayDelay = 1f;

  private float currentOrderTotal;
  private float customerPayment;
  private float requiredChange;
  private bool waitingForPayment = false;
  private bool waitingForChange = false;
  private GameObject spawnedPaymentObject;

  public static PaymentHandler Instance;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
    }
  }

  public void StartPaymentPhase()
  {
    if (orderBubbleController == null || ItemPrices.Instance == null)
    {
      Debug.LogError("PaymentHandler: Missing dependencies!");
      return;
    }

    if (paymentCollectorObject != null)
    {
      paymentCollectorObject.SetActive(true);
    }

    int foodID = orderBubbleController.requiredFoodID;
    int foodQty = orderBubbleController.requiredFoodQuantity;
    int drinkID = orderBubbleController.requiredDrinkID;
    int drinkQty = orderBubbleController.requiredDrinkQuantity;
    currentOrderTotal = ItemPrices.Instance.CalculateOrderTotal(foodID, foodQty, drinkID, drinkQty);
    customerPayment = DetermineCustomerPayment(currentOrderTotal);
    requiredChange = customerPayment - currentOrderTotal;

    Debug.Log($"Order Total: ${currentOrderTotal:F2}, Customer Pays: ${customerPayment:F2}, Change Needed: ${requiredChange:F2}");

    if (TrayValidator.Instance != null)
    {
      TrayValidator.Instance.StartPaymentPhase(requiredChange);
    }

    StartCoroutine(ShowPaymentSequence());
  }

  private IEnumerator ShowPaymentSequence()
  {
    yield return new WaitForSeconds(paymentDisplayDelay);

    if (orderBubbleController != null)
    {
      orderBubbleController.ShowPayment(customerPayment);
    }

    if (orderProgressUI != null)
    {
      orderProgressUI.ShowPaymentMessage($"Payment: ${customerPayment:F2} received", currentOrderTotal, customerPayment);
    }

    SpawnCustomerPayment();

    waitingForPayment = true;
  }

  private float DetermineCustomerPayment(float orderTotal)
  {
    if (orderTotal < 5f)
    {
      return Random.value < 0.7f ? 5f : 10f;
    }
    else
    {
      return 10f;
    }
  }

  private void SpawnCustomerPayment()
  {
    if (moneySpawnPoint == null)
    {
      Debug.LogError("PaymentHandler: Money spawn point not assigned!");
      return;
    }

    GameObject moneyToSpawn = GetMoneyPrefab(customerPayment);
    if (moneyToSpawn != null)
    {
      spawnedPaymentObject = Instantiate(moneyToSpawn, moneySpawnPoint.position, moneySpawnPoint.rotation);
      spawnedPaymentObject.layer = LayerMask.NameToLayer("CustomerPayment");
      Debug.Log($"Spawned ${customerPayment:F2} at counter");
    }
  }

  private GameObject GetMoneyPrefab(float amount)
  {
    if (moneyPrefabs == null || moneyPrefabs.Length == 0) return null;

    if (amount == 10f) return moneyPrefabs[0];
    if (amount == 5f) return moneyPrefabs[1];


    return moneyPrefabs[0];
  }

  public void OnPaymentReceived(float amount)
  {
    if (!waitingForPayment) return;

    if (Mathf.Abs(amount - customerPayment) < 0.01f)
    {
      Debug.Log("Payment received correctly!");
      waitingForPayment = false;
      waitingForChange = true;

      if (orderProgressUI != null)
      {
        orderProgressUI.ShowPaymentMessage("Give change", currentOrderTotal, customerPayment);
        orderProgressUI.OnPaymentCollected();
      }

      if (trayValidator != null)
      {
        trayValidator.StartPaymentPhase(requiredChange);
      }
    }
  }

  public void OnChangeValidated(bool correct, float givenAmount)
  {
    if (!waitingForChange) return;

    if (correct)
    {
      Debug.Log("✓ Correct change given!");

      if (orderProgressUI != null)
      {
        orderProgressUI.ShowPaymentMessage("Correct change!", currentOrderTotal, customerPayment);
        orderProgressUI.OnPaymentCollected();
      }

      if (EarningsTracker.Instance != null)
      {
        EarningsTracker.Instance.AddProfit(currentOrderTotal);
      }

      StartCoroutine(CompleteTransaction());
    }
    else
    {
      if (givenAmount < requiredChange)
      {
        if (orderProgressUI != null)
        {
          orderProgressUI.ShowPaymentMessage("Not enough change!", currentOrderTotal, customerPayment);
        }
        Debug.Log("✗ Not enough change given!");
      }
      else
      {
        if (orderProgressUI != null)
        {
          orderProgressUI.ShowPaymentMessage("Too much change!", currentOrderTotal, customerPayment);
        }
        Debug.Log("✗ Too much change given!");
      }
    }
  }

  private static readonly string[] foodTagNames = { "Apple", "Banana", "Orange", "Strawberry", "Bento1", "Bento2" };
  private static readonly string[] drinkTagNames = { "Blueberry", "GreenTea" };

  private IEnumerator CompleteTransaction()
  {
    yield return new WaitForSeconds(2f);

    waitingForChange = false;

    // Log transaction before clearing order
    if (SessionLogger.Instance != null && orderBubbleController != null)
    {
      int foodID = orderBubbleController.requiredFoodID;
      int drinkID = orderBubbleController.requiredDrinkID;
      string foodName = (foodID >= 0 && foodID < foodTagNames.Length) ? foodTagNames[foodID] : "Unknown";
      string drinkName = (drinkID >= 0 && drinkID < drinkTagNames.Length) ? drinkTagNames[drinkID] : "Unknown";

      SessionLogger.Instance.LogTransaction(
          foodName,
          orderBubbleController.requiredFoodQuantity,
          drinkName,
          orderBubbleController.requiredDrinkQuantity,
          true,
          currentOrderTotal,
          customerPayment,
          requiredChange,
          true
      );
    }

    if (orderBubbleController != null)
    {
      orderBubbleController.HideOrder();
    }

    if (orderProgressUI != null)
    {
      orderProgressUI.HideUI();
    }

    yield return new WaitForSeconds(1f);

    if (QueueManager.Instance != null)
    {
      QueueManager.Instance.ShiftQueue();
    }

    Debug.Log("Transaction complete - customer leaving");
  }

  public float GetRequiredChange()
  {
    return requiredChange;
  }
}
