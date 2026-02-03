using UnityEngine;
using System.Collections.Generic;

public class TrayValidator : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the OrderBubbleController script")]
    [SerializeField] private OrderBubbleController orderBubbleController;

    [Header("Order Complete Settings")]
    [Tooltip("Delay before moving to next customer after order complete (seconds)")]
    [SerializeField] private float orderCompleteDelay = 2f;

    // Maps tag names to counts (e.g., "Apple" -> 3, "GreenTea" -> 2)
    private Dictionary<string, int> itemsOnTray = new Dictionary<string, int>();
    
    // NEW: Track actual GameObjects on the tray so we can destroy them later
    private List<GameObject> physicalItemsOnTray = new List<GameObject>();
    
    // What we're looking for
    private string requiredFoodTag;
    private int requiredFoodQuantity;
    private string requiredDrinkTag;
    private int requiredDrinkQuantity;

    private bool orderActive = false;

    // Tag name mappings (based on your food/drink arrays in OrderBubbleController)
    private readonly string[] foodTags = { "Apple", "Banana", "Orange", "Strawberry", "Bento1", "Bento2" };
    private readonly string[] drinkTags = { "Blueberry", "GreenTea" };

    // SINGLETON PATTERN
    public static TrayValidator Instance;

    private void Awake()
    {
        Instance = this;
        Debug.Log("TrayValidator: Awake called");
    }

    private void Start()
    {
        Debug.Log("TrayValidator: Start called");
        
        // Validation checks
        if (orderBubbleController == null)
        {
            Debug.LogError("TrayValidator: OrderBubbleController reference is missing! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log("TrayValidator: OrderBubbleController reference found");
        }

        // Make sure this GameObject has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("TrayValidator: This GameObject needs a Collider component set to 'Is Trigger'!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("TrayValidator: Collider is not set to 'Is Trigger'. Setting it now.");
            col.isTrigger = true;
        }
        else
        {
            Debug.Log($"TrayValidator: Trigger collider found and active. Bounds: {col.bounds}");
        }
    }

    // Call this when a new customer arrives at Spot 1
    public void StartNewOrder()
    {
        Debug.Log("=== TrayValidator: StartNewOrder called ===");
        
        // 1. Generate the order (this sets the public variables on OrderBubbleController)
        orderBubbleController.GenerateNewOrder();

        // 2. Read what we need to validate
        SetupOrderRequirements();

        // 3. Clear the tray tracking
        ClearTray();

        orderActive = true;

        Debug.Log($"Order active: Need {requiredFoodQuantity}x {requiredFoodTag} + {requiredDrinkQuantity}x {requiredDrinkTag}");
    }

    private void SetupOrderRequirements()
    {
        // Map the food ID to its tag name
        int foodID = orderBubbleController.requiredFoodID;
        requiredFoodTag = foodTags[foodID];
        requiredFoodQuantity = orderBubbleController.requiredFoodQuantity;

        // Map the drink ID to its tag name
        int drinkID = orderBubbleController.requiredDrinkID;
        requiredDrinkTag = drinkTags[drinkID];
        requiredDrinkQuantity = orderBubbleController.requiredDrinkQuantity;
        
        Debug.Log($"TrayValidator: Setup requirements - Food: {requiredFoodTag} x{requiredFoodQuantity}, Drink: {requiredDrinkTag} x{requiredDrinkQuantity}");
    }

    // When an item enters the tray area
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"TrayValidator: OnTriggerEnter detected - Object: {other.gameObject.name}, Tag: {other.tag}, OrderActive: {orderActive}");
        
        if (!orderActive)
        {
            Debug.Log("TrayValidator: Ignoring trigger - order not active");
            return;
        }

        string tag = other.tag;

        // Only track food and drink tags
        if (IsFoodOrDrinkTag(tag))
        {
            Debug.Log($"TrayValidator: Valid food/drink tag detected: {tag}");
            
            // Add to count
            if (itemsOnTray.ContainsKey(tag))
            {
                itemsOnTray[tag]++;
            }
            else
            {
                itemsOnTray[tag] = 1;
            }
            
            // Track the physical GameObject
            if (!physicalItemsOnTray.Contains(other.gameObject))
            {
                physicalItemsOnTray.Add(other.gameObject);
                Debug.Log($"TrayValidator: Added {other.gameObject.name} to physical tracking list. Total tracked: {physicalItemsOnTray.Count}");
            }

            Debug.Log($"TrayValidator: Item added to tray: {tag}. Count: {itemsOnTray[tag]}");

            // Check if order is complete
            ValidateOrder();
        }
        else
        {
            Debug.Log($"TrayValidator: Tag '{tag}' is not a valid food/drink tag. Ignoring.");
        }
    }

    // When an item leaves the tray area
    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"TrayValidator: OnTriggerExit - Object: {other.gameObject.name}, Tag: {other.tag}");
        
        if (!orderActive) return;

        string tag = other.tag;

        if (IsFoodOrDrinkTag(tag) && itemsOnTray.ContainsKey(tag))
        {
            itemsOnTray[tag]--;
            
            if (itemsOnTray[tag] <= 0)
            {
                itemsOnTray.Remove(tag);
            }
            
            // Remove from physical tracking
            if (physicalItemsOnTray.Contains(other.gameObject))
            {
                physicalItemsOnTray.Remove(other.gameObject);
                Debug.Log($"TrayValidator: Removed {other.gameObject.name} from physical tracking. Total tracked: {physicalItemsOnTray.Count}");
            }

            Debug.Log($"TrayValidator: Item removed from tray: {tag}. Remaining: {(itemsOnTray.ContainsKey(tag) ? itemsOnTray[tag] : 0)}");
        }
    }

    private bool IsFoodOrDrinkTag(string tag)
    {
        // Check if tag is in either array
        foreach (string foodTag in foodTags)
        {
            if (tag == foodTag) return true;
        }
        foreach (string drinkTag in drinkTags)
        {
            if (tag == drinkTag) return true;
        }
        return false;
    }

    private void ValidateOrder()
    {
        Debug.Log("=== TrayValidator: ValidateOrder called ===");
        
        // Check if we have the exact food quantity and tag
        int foodCount = itemsOnTray.ContainsKey(requiredFoodTag) ? itemsOnTray[requiredFoodTag] : 0;
        
        // Check if we have the exact drink quantity and tag
        int drinkCount = itemsOnTray.ContainsKey(requiredDrinkTag) ? itemsOnTray[requiredDrinkTag] : 0;
        
        Debug.Log($"TrayValidator: Current counts - {requiredFoodTag}: {foodCount}/{requiredFoodQuantity}, {requiredDrinkTag}: {drinkCount}/{requiredDrinkQuantity}");

        // Order is complete if BOTH match exactly
        if (foodCount == requiredFoodQuantity && drinkCount == requiredDrinkQuantity)
        {
            // Make sure there are NO extra items on the tray
            int totalItemsOnTray = 0;
            foreach (var count in itemsOnTray.Values)
            {
                totalItemsOnTray += count;
            }

            int expectedTotal = requiredFoodQuantity + requiredDrinkQuantity;
            
            Debug.Log($"TrayValidator: Total items on tray: {totalItemsOnTray}, Expected: {expectedTotal}");

            if (totalItemsOnTray == expectedTotal)
            {
                Debug.Log("TrayValidator: Order requirements met! Triggering completion.");
                OnOrderComplete();
            }
            else
            {
                Debug.Log($"TrayValidator: Extra items on tray ({totalItemsOnTray} vs {expectedTotal}). Not completing order yet.");
            }
        }
        else
        {
            Debug.Log("TrayValidator: Order not complete yet - quantities don't match.");
        }
    }

    private void OnOrderComplete()
    {
        orderActive = false;

        Debug.Log("=== ORDER COMPLETE! Correct items on tray. ===");

        // Hide the order bubble
        orderBubbleController.HideOrder();

        // Move to next customer after delay
        Debug.Log($"TrayValidator: Waiting {orderCompleteDelay} seconds before clearing tray and moving to next customer...");
        Invoke(nameof(CompleteOrderSequence), orderCompleteDelay);
    }

    private void CompleteOrderSequence()
    {
        Debug.Log("=== TrayValidator: CompleteOrderSequence started ===");
        
        // Clear physical items from the tray
        ClearPhysicalItems();

        // Tell QueueManager to move to the next customer
        if (QueueManager.Instance != null)
        {
            Debug.Log("TrayValidator: Calling QueueManager.ShiftQueue()");
            QueueManager.Instance.ShiftQueue();
        }
        else
        {
            Debug.LogWarning("TrayValidator: QueueManager.Instance is null! Cannot shift queue.");
        }
    }

    private void ClearTray()
    {
        itemsOnTray.Clear();
        Debug.Log("TrayValidator: Tray tracking dictionary cleared.");
    }

    // Clear all physical items from the tray
    public void ClearPhysicalItems()
    {
        Debug.Log($"=== TrayValidator: ClearPhysicalItems called. Items to destroy: {physicalItemsOnTray.Count} ===");
        
        int destroyedCount = 0;
        
        // Create a copy of the list to avoid modification during iteration
        List<GameObject> itemsToDestroy = new List<GameObject>(physicalItemsOnTray);
        
        foreach (GameObject item in itemsToDestroy)
        {
            if (item != null)
            {
                Debug.Log($"TrayValidator: Destroying {item.name} (Tag: {item.tag})");
                Destroy(item);
                destroyedCount++;
            }
            else
            {
                Debug.LogWarning("TrayValidator: Found null item in tracking list (already destroyed?)");
            }
        }
        
        Debug.Log($"TrayValidator: Destroyed {destroyedCount} items from tray.");
        
        // Clear both tracking systems
        physicalItemsOnTray.Clear();
        ClearTray();
        
        Debug.Log("TrayValidator: Physical items list and tracking dictionary both cleared.");
    }
}