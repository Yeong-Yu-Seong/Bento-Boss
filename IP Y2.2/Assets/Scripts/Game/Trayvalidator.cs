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
    
    // Track actual GameObjects on the tray so we can destroy them later
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
        Debug.Log("TrayValidator: Awake called");
        Instance = this;
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
            Debug.Log("TrayValidator: OrderBubbleController reference is assigned correctly");
        }
    }

    // Call this when a new customer arrives at Spot 1
    public void StartNewOrder()
    {
        Debug.Log("=== TrayValidator: StartNewOrder called ===");
        Debug.Log($"TrayValidator: orderActive BEFORE = {orderActive}");
        
        // CRASH FIX: Stop any pending cleanup from previous orders to prevent logic overlaps
        CancelInvoke();
        
        // 1. Generate the order
        if(orderBubbleController != null) 
            orderBubbleController.GenerateNewOrder();

        // 2. Read what we need to validate
        SetupOrderRequirements();

        // 3. Clear the tray tracking (Fresh start)
        // We clear the dictionary and list to ensure no ghost items exist from previous crashes/resets
        itemsOnTray.Clear();
        physicalItemsOnTray.Clear();

        orderActive = true;
        Debug.Log($"TrayValidator: orderActive AFTER = {orderActive}");

        Debug.Log($"Order active: Need {requiredFoodQuantity}x {requiredFoodTag} + {requiredDrinkQuantity}x {requiredDrinkTag}");
    }

    private void SetupOrderRequirements()
    {
        if (orderBubbleController == null) return;

        // CRASH FIX: Bounds checking to prevent IndexOutOfRangeException if IDs are wrong
        int foodID = orderBubbleController.requiredFoodID;
        if (foodID >= 0 && foodID < foodTags.Length)
        {
            requiredFoodTag = foodTags[foodID];
        }
        else
        {
            Debug.LogError($"TrayValidator: Food ID {foodID} is out of bounds!");
            requiredFoodTag = "INVALID";
        }
        
        requiredFoodQuantity = orderBubbleController.requiredFoodQuantity;

        int drinkID = orderBubbleController.requiredDrinkID;
        if (drinkID >= 0 && drinkID < drinkTags.Length)
        {
            requiredDrinkTag = drinkTags[drinkID];
        }
        else
        {
            Debug.LogError($"TrayValidator: Drink ID {drinkID} is out of bounds!");
            requiredDrinkTag = "INVALID";
        }
        
        requiredDrinkQuantity = orderBubbleController.requiredDrinkQuantity;
        
        Debug.Log($"TrayValidator: Setup requirements - Food: {requiredFoodTag} x{requiredFoodQuantity}, Drink: {requiredDrinkTag} x{requiredDrinkQuantity}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($">>> OnTriggerEnter FIRED! Object: {other.gameObject.name}, Tag: {other.tag}, IsTrigger: {other.isTrigger}");
        
        if (!orderActive)
        {
            Debug.Log(">>> OnTriggerEnter: orderActive is FALSE, exiting early");
            return;
        }

        if (other == null)
        {
            Debug.Log(">>> OnTriggerEnter: other is NULL, exiting early");
            return;
        }

        GameObject itemObj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        Debug.Log($">>> OnTriggerEnter: itemObj determined as {itemObj.name}");
        
        if (itemObj == null)
        {
            Debug.Log(">>> OnTriggerEnter: itemObj is NULL, exiting early");
            return;
        }

        string tag = itemObj.tag;
        Debug.Log($">>> OnTriggerEnter: Checking tag '{tag}' against food/drink tags");

        // CHILD COLLIDER FIX: Only process if the collider's GameObject matches the root object
        // This prevents child colliders from triggering duplicate detections
        if (other.gameObject != itemObj)
        {
            Debug.Log($">>> OnTriggerEnter: Ignoring child collider '{other.gameObject.name}' of parent '{itemObj.name}'");
            return;
        }

        if (IsFoodOrDrinkTag(tag))
        {
            Debug.Log($">>> OnTriggerEnter: '{tag}' IS a food or drink tag!");
            
            if (physicalItemsOnTray.Contains(itemObj))
            {
                Debug.Log($">>> OnTriggerEnter: {itemObj.name} is ALREADY tracked, ignoring");
                return;
            }

            physicalItemsOnTray.Add(itemObj);

            if (itemsOnTray.ContainsKey(tag))
            {
                itemsOnTray[tag]++;
            }
            else
            {
                itemsOnTray[tag] = 1;
            }

            Debug.Log($"TrayValidator: Added {itemObj.name}. Current {tag} count: {itemsOnTray[tag]}");
            ValidateOrder();
        }
        else
        {
            Debug.Log($">>> OnTriggerEnter: '{tag}' is NOT a food or drink tag, ignoring");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($">>> OnTriggerExit FIRED! Object: {other.gameObject.name}, Tag: {other.tag}, IsTrigger: {other.isTrigger}");
        
        if (!orderActive)
        {
            Debug.Log(">>> OnTriggerExit: orderActive is FALSE, exiting early");
            return;
        }
        
        if (other == null)
        {
            Debug.Log(">>> OnTriggerExit: other is NULL, exiting early");
            return;
        }

        GameObject itemObj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        Debug.Log($">>> OnTriggerExit: itemObj determined as {itemObj.name}");
        
        if (itemObj == null)
        {
            Debug.Log(">>> OnTriggerExit: itemObj is NULL, exiting early");
            return;
        }

        // CHILD COLLIDER FIX: Only process if the collider's GameObject matches the root object
        // This prevents child colliders from triggering duplicate detections
        if (other.gameObject != itemObj)
        {
            Debug.Log($">>> OnTriggerExit: Ignoring child collider '{other.gameObject.name}' of parent '{itemObj.name}'");
            return;
        }

        string tag = itemObj.tag;

        if (physicalItemsOnTray.Contains(itemObj))
        {
            Debug.Log($">>> OnTriggerExit: {itemObj.name} WAS being tracked, removing it");
            
            physicalItemsOnTray.Remove(itemObj);

            if (itemsOnTray.ContainsKey(tag))
            {
                itemsOnTray[tag]--;
                if (itemsOnTray[tag] <= 0)
                {
                    itemsOnTray.Remove(tag);
                }
            }
            
            Debug.Log($"TrayValidator: Removed {itemObj.name}. Current {tag} count: {(itemsOnTray.ContainsKey(tag) ? itemsOnTray[tag] : 0)}");

            ValidateOrder();
        }
        else
        {
            Debug.Log($">>> OnTriggerExit: {itemObj.name} was NOT being tracked, ignoring");
        }
    }

    private bool IsFoodOrDrinkTag(string tag)
    {
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
        int foodCount = itemsOnTray.ContainsKey(requiredFoodTag) ? itemsOnTray[requiredFoodTag] : 0;
        int drinkCount = itemsOnTray.ContainsKey(requiredDrinkTag) ? itemsOnTray[requiredDrinkTag] : 0;
        
        Debug.Log($"ValidateOrder: Current food={foodCount}/{requiredFoodQuantity}, drink={drinkCount}/{requiredDrinkQuantity}");
        
        if (foodCount == requiredFoodQuantity && drinkCount == requiredDrinkQuantity)
        {
            int totalItemsOnTray = 0;
            foreach (var count in itemsOnTray.Values)
            {
                totalItemsOnTray += count;
            }

            int expectedTotal = requiredFoodQuantity + requiredDrinkQuantity;
            
            if (totalItemsOnTray == expectedTotal)
            {
                Debug.Log("TrayValidator: Order requirements met! Triggering completion.");
                OnOrderComplete();
            }
            else
            {
                Debug.Log($"TrayValidator: Extra items on tray ({totalItemsOnTray} vs {expectedTotal}).");
            }
        }
    }

    private void OnOrderComplete()
    {
        orderActive = false;
        Debug.Log("=== ORDER COMPLETE! Correct items on tray. ===");

        if(orderBubbleController != null)
            orderBubbleController.HideOrder();

        Debug.Log($"TrayValidator: Waiting {orderCompleteDelay} seconds...");
        Invoke(nameof(CompleteOrderSequence), orderCompleteDelay);
    }

    private void CompleteOrderSequence()
    {
        Debug.Log("=== TrayValidator: CompleteOrderSequence started ===");
        
        ClearPhysicalItems();

        if (QueueManager.Instance != null)
        {
            QueueManager.Instance.ShiftQueue();
        }
        else
        {
            Debug.LogWarning("TrayValidator: QueueManager.Instance is null!");
        }
    }

    public void ClearPhysicalItems()
    {
        Debug.Log($"=== TrayValidator: Cleanup Items: {physicalItemsOnTray.Count} ===");
        
        List<GameObject> itemsToDestroy = new List<GameObject>(physicalItemsOnTray);
        
        foreach (GameObject item in itemsToDestroy)
        {
            if (item != null)
            {
                item.SetActive(false); 
                Destroy(item);
            }
        }
        
        physicalItemsOnTray.Clear();
        itemsOnTray.Clear();
    }
}