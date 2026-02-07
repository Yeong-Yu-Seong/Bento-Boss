using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class OrderProgressUI : MonoBehaviour
{
  [Header("UI References")]
  [SerializeField] private GameObject progressPanel;
  [SerializeField] private TextMeshProUGUI foodProgressText;
  [SerializeField] private TextMeshProUGUI drinkProgressText;
  [SerializeField] private TextMeshProUGUI changeGivenText;

  [Header("Dependencies")]
  [SerializeField] private Camera mainCamera;
  [SerializeField] private TrayValidator trayValidator;
  [SerializeField] private OrderBubbleController orderBubbleController;

  [Header("Color Settings")]
  [SerializeField] private Color incompleteColor = new Color(1f, 0.71f, 0.39f);
  [SerializeField] private Color completeColor = new Color(0.39f, 1f, 0.39f);
  [SerializeField] private Color excessColor = new Color(1f, 0.39f, 0.39f);

  private readonly string[] foodTags = { "Apple", "Banana", "Orange", "Strawberry", "Bento1", "Bento2" };
  private readonly string[] drinkTags = { "Blueberry", "GreenTea" };

  private string requiredFoodTag;
  private int requiredFoodQuantity;
  private string requiredDrinkTag;
  private int requiredDrinkQuantity;

  private bool orderActive = false;
  private bool paymentMode = false;

  // Dirty tracking to avoid rebuilding UI every frame
  private int lastFoodCount = -1;
  private int lastDrinkCount = -1;

  // Cached display names (computed once per order)
  private string cachedFoodDisplayName;
  private string cachedDrinkDisplayName;

  private void Start()
  {
    if (mainCamera == null) mainCamera = Camera.main;

    if (progressPanel != null) progressPanel.SetActive(false);
  }

  private void Update()
  {
    if (!orderActive || mainCamera == null) return;

    Vector3 direction = mainCamera.transform.position - transform.position;
    direction.y = 0;
    transform.rotation = Quaternion.LookRotation(direction);

    if (!paymentMode)
    {
      UpdateProgressDisplay();
    }
    else
    {
      UpdateChangeDisplay();
    }
  }

  public void ShowOrderProgress()
  {
    if (orderBubbleController == null) return;

    int foodID = orderBubbleController.requiredFoodID;
    int drinkID = orderBubbleController.requiredDrinkID;

    if (foodID < 0 || foodID >= foodTags.Length || drinkID < 0 || drinkID >= drinkTags.Length) return;

    requiredFoodTag = foodTags[foodID];
    requiredFoodQuantity = orderBubbleController.requiredFoodQuantity;
    requiredDrinkTag = drinkTags[drinkID];
    requiredDrinkQuantity = orderBubbleController.requiredDrinkQuantity;

    // Cache display names once per order
    cachedFoodDisplayName = requiredFoodTag == "Bento1" ? "Bento Set 1" :
                            requiredFoodTag == "Bento2" ? "Bento Set 2" : requiredFoodTag;
    cachedDrinkDisplayName = requiredDrinkTag == "GreenTea" ? "Green Tea" :
                             requiredDrinkTag == "Blueberry" ? "Blueberry Tea" : requiredDrinkTag;

    // Reset dirty tracking so first UpdateProgressDisplay runs
    lastFoodCount = -1;
    lastDrinkCount = -1;

    if (progressPanel != null) progressPanel.SetActive(true);
    orderActive = true;
    paymentMode = false;

    if (changeGivenText != null)
    {
      changeGivenText.gameObject.SetActive(false);
    }

    UpdateProgressDisplay();
  }

  public void ShowPaymentMessage(string message, float totalCost, float paymentReceived)
  {
    if (progressPanel != null) progressPanel.SetActive(true);
    orderActive = true;
    paymentMode = true;

    if (foodProgressText != null)
    {
      foodProgressText.text = $"Total Cost: ${totalCost:F2}";
      foodProgressText.color = Color.white;
    }

    if (drinkProgressText != null)
    {
      drinkProgressText.text = $"Payment Received: ${paymentReceived:F2}";
      drinkProgressText.color = Color.white;
    }

    if (changeGivenText != null)
    {
      changeGivenText.gameObject.SetActive(true);
      changeGivenText.text = "Change Given: $0.00";
      changeGivenText.color = incompleteColor;
    }
  }

  public void HideUI()
  {
    if (progressPanel != null) progressPanel.SetActive(false);
    orderActive = false;
    paymentMode = false;

    if (changeGivenText != null)
    {
      changeGivenText.gameObject.SetActive(false);
    }
  }

  private void UpdateProgressDisplay()
  {
    if (trayValidator == null) return;

    var items = trayValidator.GetItemsOnTray();

    int foodCount = items.ContainsKey(requiredFoodTag) ? items[requiredFoodTag] : 0;
    int drinkCount = items.ContainsKey(requiredDrinkTag) ? items[requiredDrinkTag] : 0;

    if (foodCount == lastFoodCount && drinkCount == lastDrinkCount) return;
    lastFoodCount = foodCount;
    lastDrinkCount = drinkCount;

    if (foodProgressText != null)
    {
      foodProgressText.text = $"{foodCount}/{requiredFoodQuantity} {cachedFoodDisplayName}";
      foodProgressText.color = foodCount > requiredFoodQuantity ? excessColor :
                               foodCount >= requiredFoodQuantity ? completeColor : incompleteColor;
    }

    if (drinkProgressText != null)
    {
      drinkProgressText.text = $"{drinkCount}/{requiredDrinkQuantity} {cachedDrinkDisplayName}";
      drinkProgressText.color = drinkCount > requiredDrinkQuantity ? excessColor :
                                drinkCount >= requiredDrinkQuantity ? completeColor : incompleteColor;
    }
  }

  private void UpdateChangeDisplay()
  {
    if (changeGivenText != null && TrayValidator.Instance != null)
    {
      float currentChange = TrayValidator.Instance.GetCollectedChange();
      float required = PaymentHandler.Instance != null ? PaymentHandler.Instance.GetRequiredChange() : 0f;
      changeGivenText.text = $"Change Given: ${currentChange:F2}";

      if (Mathf.Abs(currentChange - required) < 0.01f)
        changeGivenText.color = completeColor;
      else if (currentChange > required)
        changeGivenText.color = excessColor;
      else
        changeGivenText.color = incompleteColor;
    }
  }
}
