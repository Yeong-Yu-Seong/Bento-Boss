using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class OrderProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private TextMeshProUGUI foodProgressText;
    [SerializeField] private TextMeshProUGUI drinkProgressText;
    
    [Header("Dependencies")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TrayValidator trayValidator;
    [SerializeField] private OrderBubbleController orderBubbleController;
    
    [Header("Color Settings")]
    [SerializeField] private Color incompleteColor = new Color(1f, 0.71f, 0.39f);
    [SerializeField] private Color completeColor = new Color(0.39f, 1f, 0.39f);
    
    private readonly string[] foodTags = { "Apple", "Banana", "Orange", "Strawberry", "Bento1", "Bento2" };
    private readonly string[] drinkTags = { "Blueberry", "GreenTea" };
    
    private string requiredFoodTag;
    private int requiredFoodQuantity;
    private string requiredDrinkTag;
    private int requiredDrinkQuantity;
    
    private bool orderActive = false;

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
        
        UpdateProgressDisplay();
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
        
        if (progressPanel != null) progressPanel.SetActive(true);
        orderActive = true;
        
        UpdateProgressDisplay();
    }

    public void HideUI()
    {
        if (progressPanel != null) progressPanel.SetActive(false);
        orderActive = false;
    }

    private void UpdateProgressDisplay()
    {
        if (trayValidator == null) return;
        
        var items = trayValidator.GetItemsOnTray();
        
        int foodCount = items.ContainsKey(requiredFoodTag) ? items[requiredFoodTag] : 0;
        int drinkCount = items.ContainsKey(requiredDrinkTag) ? items[requiredDrinkTag] : 0;
        
        if (foodProgressText != null)
        {
            string foodDisplayName = requiredFoodTag == "Bento1" ? "Bento Set 1" :
                                    requiredFoodTag == "Bento2" ? "Bento Set 2" : requiredFoodTag;
            
            foodProgressText.text = $"{foodCount}/{requiredFoodQuantity} {foodDisplayName}";
            foodProgressText.color = (foodCount >= requiredFoodQuantity) ? completeColor : incompleteColor;
        }
        
        if (drinkProgressText != null)
        {
            string drinkDisplayName = requiredDrinkTag == "GreenTea" ? "Green Tea" : 
                                    requiredDrinkTag == "Blueberry" ? "Blueberry Tea" : requiredDrinkTag;
            
            drinkProgressText.text = $"{drinkCount}/{requiredDrinkQuantity} {drinkDisplayName}";
            drinkProgressText.color = (drinkCount >= requiredDrinkQuantity) ? completeColor : incompleteColor;
        }
    }
}