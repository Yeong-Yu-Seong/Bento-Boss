using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrayValidator : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the OrderBubbleController script")]
    [SerializeField] private OrderBubbleController orderBubbleController;

    [Header("Order Complete Settings")]
    [Tooltip("Delay before moving to next customer after order complete (seconds)")]
    [SerializeField] private float orderCompleteDelay = 2f;

    [Header("Socket References")]
    [Header("Drink Sockets")]
    public Transform drinkSocket1;
    public Transform drinkSocket2;

    [Header("Bento Sockets")]
    public Transform bento1Socket;
    public Transform bento2Socket;

    [Header("Apple Sockets")]
    public Transform appleSocket1;
    public Transform appleSocket2;
    public Transform appleSocket3;

    [Header("Banana Sockets")]
    public Transform bananaSocket1;
    public Transform bananaSocket2;
    public Transform bananaSocket3;

    [Header("Orange Sockets")]
    public Transform orangeSocket1;
    public Transform orangeSocket2;
    public Transform orangeSocket3;

    [Header("Strawberry Sockets")]
    public Transform strawberrySocket1;
    public Transform strawberrySocket2;
    public Transform strawberrySocket3;

    [Header("Snap Settings")]
    public float snapDelay = 0.3f;
    public float snapSpeed = 8f;

    private Dictionary<string, int> itemsOnTray = new Dictionary<string, int>();
    private List<GameObject> physicalItemsOnTray = new List<GameObject>();
    
    private string requiredFoodTag;
    private int requiredFoodQuantity;
    private string requiredDrinkTag;
    private int requiredDrinkQuantity;

    private bool orderActive = false;

    private readonly string[] foodTags = { "Apple", "Banana", "Orange", "Strawberry", "Bento1", "Bento2" };
    private readonly string[] drinkTags = { "Blueberry", "GreenTea" };

    private Dictionary<GameObject, Coroutine> pendingSnaps = new Dictionary<GameObject, Coroutine>();
    private HashSet<GameObject> snappedItems = new HashSet<GameObject>();
    
    private Dictionary<Transform, GameObject> socketOccupancy = new Dictionary<Transform, GameObject>();
    private Dictionary<GameObject, Transform> itemToSocket = new Dictionary<GameObject, Transform>();

    public static TrayValidator Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (orderBubbleController == null)
        {
            Debug.LogError("TrayValidator: OrderBubbleController reference is missing!");
        }
    }

    public void StartNewOrder()
    {
        CancelInvoke();
        
        foreach (var kvp in pendingSnaps)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        pendingSnaps.Clear();
        snappedItems.Clear();
        socketOccupancy.Clear();
        itemToSocket.Clear();
        
        if(orderBubbleController != null) 
            orderBubbleController.GenerateNewOrder();

        SetupOrderRequirements();

        itemsOnTray.Clear();
        physicalItemsOnTray.Clear();

        orderActive = true;

        Debug.Log($"New Order: {requiredFoodQuantity}x {requiredFoodTag} + {requiredDrinkQuantity}x {requiredDrinkTag}");
    }

    private void SetupOrderRequirements()
    {
        if (orderBubbleController == null) return;

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!orderActive) return;
        if (other == null) return;

        GameObject itemObj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        
        if (itemObj == null) return;
        if (other.gameObject != itemObj) return;

        string tag = itemObj.tag;

        if (IsFoodOrDrinkTag(tag))
        {
            if (physicalItemsOnTray.Contains(itemObj)) return;

            physicalItemsOnTray.Add(itemObj);

            if (itemsOnTray.ContainsKey(tag))
            {
                itemsOnTray[tag]++;
            }
            else
            {
                itemsOnTray[tag] = 1;
            }

            Debug.Log($"Added {tag} ({itemsOnTray[tag]}/{(tag == requiredFoodTag ? requiredFoodQuantity : requiredDrinkQuantity)})");
            
            if (IsCorrectItemForOrder(tag))
            {
                Transform targetSocket = GetSocketForItem(tag, itemObj);
                if (targetSocket != null)
                {
                    socketOccupancy[targetSocket] = itemObj;
                    itemToSocket[itemObj] = targetSocket;
                    
                    Rigidbody rb = itemObj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.useGravity = false;
                        
                        if (!rb.isKinematic)
                        {
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                        
                        rb.isKinematic = true;
                    }
                    
                    Coroutine snapCoroutine = StartCoroutine(SnapToSocketAfterDelay(itemObj, targetSocket));
                    pendingSnaps[itemObj] = snapCoroutine;
                }
            }
            
            ValidateOrder();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!orderActive) return;
        if (other == null) return;

        GameObject itemObj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        
        if (itemObj == null) return;
        if (other.gameObject != itemObj) return;

        string tag = itemObj.tag;

        if (physicalItemsOnTray.Contains(itemObj))
        {
            if (pendingSnaps.ContainsKey(itemObj))
            {
                StopCoroutine(pendingSnaps[itemObj]);
                pendingSnaps.Remove(itemObj);
            }
            
            if (snappedItems.Contains(itemObj))
            {
                snappedItems.Remove(itemObj);
            }
            
            if (itemToSocket.ContainsKey(itemObj))
            {
                Transform socket = itemToSocket[itemObj];
                socketOccupancy.Remove(socket);
                itemToSocket.Remove(itemObj);
            }
            
            Rigidbody rb = itemObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            
            physicalItemsOnTray.Remove(itemObj);

            if (itemsOnTray.ContainsKey(tag))
            {
                itemsOnTray[tag]--;
                if (itemsOnTray[tag] <= 0)
                {
                    itemsOnTray.Remove(tag);
                }
            }
            
            Debug.Log($"Removed {tag} (remaining: {(itemsOnTray.ContainsKey(tag) ? itemsOnTray[tag] : 0)})");

            ValidateOrder();
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

    private bool IsCorrectItemForOrder(string itemTag)
    {
        if (!orderActive) return false;
        return itemTag == requiredFoodTag || itemTag == requiredDrinkTag;
    }

    private Transform GetSocketForItem(string itemTag, GameObject item)
    {
        if (itemTag == "Blueberry" || itemTag == "GreenTea")
        {
            if (!socketOccupancy.ContainsKey(drinkSocket1)) return drinkSocket1;
            if (!socketOccupancy.ContainsKey(drinkSocket2)) return drinkSocket2;
            return null;
        }
        
        if (itemTag == "Bento1") 
        {
            if (!socketOccupancy.ContainsKey(bento1Socket)) return bento1Socket;
            return null;
        }
        
        if (itemTag == "Bento2") 
        {
            if (!socketOccupancy.ContainsKey(bento2Socket)) return bento2Socket;
            return null;
        }
        
        if (itemTag == "Apple")
        {
            if (!socketOccupancy.ContainsKey(appleSocket1)) return appleSocket1;
            if (!socketOccupancy.ContainsKey(appleSocket2)) return appleSocket2;
            if (!socketOccupancy.ContainsKey(appleSocket3)) return appleSocket3;
            return null;
        }
        
        if (itemTag == "Banana")
        {
            if (!socketOccupancy.ContainsKey(bananaSocket1)) return bananaSocket1;
            if (!socketOccupancy.ContainsKey(bananaSocket2)) return bananaSocket2;
            if (!socketOccupancy.ContainsKey(bananaSocket3)) return bananaSocket3;
            return null;
        }
        
        if (itemTag == "Orange")
        {
            if (!socketOccupancy.ContainsKey(orangeSocket1)) return orangeSocket1;
            if (!socketOccupancy.ContainsKey(orangeSocket2)) return orangeSocket2;
            if (!socketOccupancy.ContainsKey(orangeSocket3)) return orangeSocket3;
            return null;
        }
        
        if (itemTag == "Strawberry")
        {
            if (!socketOccupancy.ContainsKey(strawberrySocket1)) return strawberrySocket1;
            if (!socketOccupancy.ContainsKey(strawberrySocket2)) return strawberrySocket2;
            if (!socketOccupancy.ContainsKey(strawberrySocket3)) return strawberrySocket3;
            return null;
        }
        
        return null;
    }

    private IEnumerator SnapToSocketAfterDelay(GameObject item, Transform socket)
    {
        float elapsedTime = 0f;
        Vector3 startPos = item.transform.position;
        Quaternion startRot = item.transform.rotation;
        
        while (elapsedTime < snapDelay)
        {
            if (item == null || !physicalItemsOnTray.Contains(item))
            {
                yield break;
            }
            
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / snapDelay);
            
            item.transform.position = Vector3.Lerp(startPos, socket.position, t);
            item.transform.rotation = Quaternion.Lerp(startRot, socket.rotation, t);
            
            yield return null;
        }
        
        if (item != null)
        {
            item.transform.position = socket.position;
            item.transform.rotation = socket.rotation;
            snappedItems.Add(item);
        }
        
        if (pendingSnaps.ContainsKey(item))
        {
            pendingSnaps.Remove(item);
        }
    }

    private void ValidateOrder()
    {
        int foodCount = itemsOnTray.ContainsKey(requiredFoodTag) ? itemsOnTray[requiredFoodTag] : 0;
        int drinkCount = itemsOnTray.ContainsKey(requiredDrinkTag) ? itemsOnTray[requiredDrinkTag] : 0;
        
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
                Debug.Log("✓ ORDER COMPLETE!");
                OnOrderComplete();
            }
        }
    }

    private void OnOrderComplete()
    {
        orderActive = false;

        if(orderBubbleController != null)
            orderBubbleController.HideOrder();

        Invoke(nameof(CompleteOrderSequence), orderCompleteDelay);
    }

    private void CompleteOrderSequence()
    {
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
        foreach (var kvp in pendingSnaps)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        pendingSnaps.Clear();
        snappedItems.Clear();
        socketOccupancy.Clear();
        itemToSocket.Clear();
        
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