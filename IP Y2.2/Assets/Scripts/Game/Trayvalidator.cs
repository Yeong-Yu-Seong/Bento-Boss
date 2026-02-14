/// <summary>
/// File: Trayvalidator.cs
/// Author: Jayden Wong
/// Description: Validates tray contents against the current order, handles item snapping to sockets, and manages change collection during payment.
/// </summary>
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using System.Collections.Generic;

public class TrayValidator : MonoBehaviour
{
  [Header("Dependencies")]
  [Tooltip("Reference to the OrderBubbleController script")]
  [SerializeField] private OrderProgressUI orderProgressUI;
  [SerializeField] private OrderBubbleController orderBubbleController;

  [Header("Order Complete Settings")]
  [Tooltip("Delay before moving to next customer after order complete (seconds)")]

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

  [Header("Feedback VFX/SFX")]
  [Tooltip("AudioSource on the tray for playing feedback SFX")]
  [SerializeField] private AudioSource feedbackAudioSource;
  [Tooltip("SFX played when a correct item is placed")]
  [SerializeField] private AudioClip successSFX;
  [Tooltip("SFX played when a wrong item is placed")]
  [SerializeField] private AudioClip failSFX;
  [Tooltip("Particle system played when a correct item is placed")]
  [SerializeField] private ParticleSystem successVFX;
  [Tooltip("Particle system played when a wrong item is placed")]
  [SerializeField] private ParticleSystem failVFX;
  [Tooltip("How long the VFX plays before being stopped (seconds)")]
  [SerializeField] private float feedbackDuration = 2f;
  [Tooltip("Minimum time between VFX plays to prevent duplicate particles (seconds)")]
  [SerializeField] private float vfxCooldown = 0.5f;

  private Dictionary<string, int> itemsOnTray = new Dictionary<string, int>();
  private List<GameObject> physicalItemsOnTray = new List<GameObject>();
  private HashSet<GameObject> physicalItemsSet = new HashSet<GameObject>();

  private string requiredFoodTag;
  private int requiredFoodQuantity;
  private string requiredDrinkTag;
  private int requiredDrinkQuantity;

  private bool orderActive = false;

  private bool paymentPhase = false;
  private float requiredChange = 0f;
  private float collectedChange = 0f;

  private bool hasGivenChange = false;
  private bool hasCollectedPayment = false;

  private readonly string[] foodTags = { "Apple", "Banana", "Orange", "Strawberry", "Bento1", "Bento2" };
  private readonly string[] drinkTags = { "Blueberry", "GreenTea" };

  private Dictionary<GameObject, Coroutine> pendingSnaps = new Dictionary<GameObject, Coroutine>();
  private HashSet<GameObject> snappedItems = new HashSet<GameObject>();

  private int correctFoodPlacements = 0;
  private int wrongFoodPlacements = 0;
  private bool wasChangeExact = true;

  public bool WasFoodOrderCorrect => wrongFoodPlacements == 0;
  public bool WasChangeCorrect => wasChangeExact;
  public int CorrectFoodPlacements => correctFoodPlacements;
  public int WrongFoodPlacements => wrongFoodPlacements;

  private Dictionary<Transform, GameObject> socketOccupancy = new Dictionary<Transform, GameObject>();
  private Dictionary<GameObject, Transform> itemToSocket = new Dictionary<GameObject, Transform>();
  private List<GameObject> collectedMoney = new List<GameObject>();
  private HashSet<GameObject> collectedMoneySet = new HashSet<GameObject>();

  private Dictionary<GameObject, Rigidbody> rbCache = new Dictionary<GameObject, Rigidbody>();
  private float lastVFXTime = -1f;

  private static readonly Dictionary<string, float> moneyValues = new Dictionary<string, float>
    {
        { "Money_10Cent", 0.10f },
        { "Money_20Cent", 0.20f },
        { "Money_50Cent", 0.50f },
        { "Money_1Dollar", 1.00f },
        { "Money_2Dollar", 2.00f },
        { "Money_5Dollar", 5.00f },
        { "Money_10Dollar", 10.00f }
    };

  public static TrayValidator Instance;

  /// <summary>
  /// Returns the current dictionary of item tags to their counts on the tray
  /// </summary>
  public Dictionary<string, int> GetItemsOnTray()
  {
    return itemsOnTray;
  }

  /// <summary>
  /// Returns the total value of change coins currently on the tray
  /// </summary>
  public float GetCollectedChange()
  {
    return collectedChange;
  }

  /// <summary>
  /// Returns whether the given object is a collected money item during payment phase
  /// </summary>
  public bool IsMoneyCollected(GameObject obj)
  {
    return paymentPhase && collectedMoneySet.Contains(obj);
  }

  /// <summary>
  /// Resets all payment phase state including collected money tracking
  /// </summary>
  public void ClearCollectedChange()
  {
    collectedMoney.Clear();
    collectedMoneySet.Clear();
    collectedChange = 0f;
    paymentPhase = false;
    hasGivenChange = false;
    hasCollectedPayment = false;
  }

  private Rigidbody GetCachedRigidbody(GameObject obj)
  {
    if (!rbCache.TryGetValue(obj, out Rigidbody rb))
    {
      rb = obj.GetComponent<Rigidbody>();
      if (rb != null) rbCache[obj] = rb;
    }
    return rb;
  }

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

  /// <summary>
  /// Clears all state from the previous order and generates a new one
  /// </summary>
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

    if (orderBubbleController != null)
      orderBubbleController.GenerateNewOrder();

    SetupOrderRequirements();

    itemsOnTray.Clear();
    physicalItemsOnTray.Clear();
    physicalItemsSet.Clear();

    correctFoodPlacements = 0;
    wrongFoodPlacements = 0;

    orderActive = true;

    if (orderProgressUI != null)
    {
      orderProgressUI.ShowOrderProgress();
    }

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
    if (!orderActive && !paymentPhase) return;
    if (other == null) return;

    GameObject itemObj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

    if (itemObj == null) return;
    if (other.gameObject != itemObj) return;

    string tag = itemObj.tag;

    if (paymentPhase)
    {
      float moneyValue = GetMoneyValue(tag);
      if (moneyValue > 0f)
      {
        if (!collectedMoneySet.Contains(itemObj))
        {
          collectedMoney.Add(itemObj);
          collectedMoneySet.Add(itemObj);
          collectedChange += moneyValue;

          Debug.Log($"Change collected: ${moneyValue:F2}, Total: ${collectedChange:F2}");
          ValidateChange();
        }
      }
      return;
    }

    if (IsFoodOrDrinkTag(tag))
    {
      if (physicalItemsSet.Contains(itemObj)) return;

      physicalItemsOnTray.Add(itemObj);
      physicalItemsSet.Add(itemObj);

      if (itemsOnTray.TryGetValue(tag, out int count))
        itemsOnTray[tag] = count + 1;
      else
        itemsOnTray[tag] = 1;

      Debug.Log($"Added {tag} ({itemsOnTray[tag]}/{(tag == requiredFoodTag ? requiredFoodQuantity : requiredDrinkQuantity)})");

      bool isCorrect = IsCorrectItemForOrder(tag);
      PlayFeedback(isCorrect);

      if (isCorrect)
        correctFoodPlacements++;
      else
        wrongFoodPlacements++;

      if (isCorrect)
      {
        Transform targetSocket = GetSocketForItem(tag, itemObj);
        if (targetSocket != null)
        {
          if (!itemToSocket.ContainsKey(itemObj))
          {
            socketOccupancy[targetSocket] = itemObj;
            itemToSocket[itemObj] = targetSocket;

            Rigidbody rb = GetCachedRigidbody(itemObj);
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
      }

      ValidateOrder();
    }
  }

  private void OnTriggerExit(Collider other)
  {
    if (!orderActive && !paymentPhase) return;
    if (other == null) return;
    GameObject itemObj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

    if (itemObj == null) return;
    if (other.gameObject != itemObj) return;
    string tag = itemObj.tag;

    if (paymentPhase && collectedMoneySet.Contains(itemObj))
    {
      XRGrabInteractable grab = itemObj.GetComponent<XRGrabInteractable>();
      bool isBeingHeld = grab != null && grab.isSelected;

      Rigidbody moneyRb = GetCachedRigidbody(itemObj);
      bool isSettled = moneyRb != null && moneyRb.isKinematic;

      // If money is settled on tray and not being held, ignore the physics exit event
      if (!isBeingHeld && isSettled)
      {
        return;
      }

      float moneyValue = GetMoneyValue(tag);
      if (moneyValue > 0f)
      {
        collectedMoney.Remove(itemObj);
        collectedMoneySet.Remove(itemObj);
        collectedChange -= moneyValue;

        if (moneyRb != null)
        {
          moneyRb.isKinematic = false;
          moneyRb.useGravity = true;
        }

        Debug.Log($"Money removed: ${moneyValue:F2}, Remaining: ${collectedChange:F2}");
      }
      return;
    }

    if (physicalItemsSet.Contains(itemObj))
    {
      XRGrabInteractable grab = itemObj.GetComponent<XRGrabInteractable>();
      bool isBeingHeld = grab != null && grab.isSelected;

      // If item is mid-snap and not being held, ignore the exit — it will settle at the socket
      if (pendingSnaps.ContainsKey(itemObj) && !isBeingHeld)
      {
        return;
      }

      if (itemToSocket.TryGetValue(itemObj, out Transform assignedSocket))
      {
        Rigidbody checkRb = GetCachedRigidbody(itemObj);

        if (checkRb != null && checkRb.isKinematic &&
            Vector3.Distance(itemObj.transform.position, assignedSocket.position) < 0.2f
            && !isBeingHeld)
        {
          return;
        }
      }

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

      Rigidbody rb = GetCachedRigidbody(itemObj);
      if (rb != null)
      {
        rb.isKinematic = false;
        rb.useGravity = true;
      }

      physicalItemsOnTray.Remove(itemObj);
      physicalItemsSet.Remove(itemObj);

      if (itemsOnTray.TryGetValue(tag, out int count2))
      {
        count2--;
        if (count2 <= 0) itemsOnTray.Remove(tag);
        else itemsOnTray[tag] = count2;
      }

      Debug.Log($"Removed {tag} (remaining: {(itemsOnTray.TryGetValue(tag, out int rem) ? rem : 0)})");

      ValidateOrder();
    }
  }

  private void PlayFeedback(bool success)
  {
    AudioClip clip = success ? successSFX : failSFX;
    ParticleSystem vfx = success ? successVFX : failVFX;

    if (clip != null && feedbackAudioSource != null)
    {
      feedbackAudioSource.PlayOneShot(clip);
    }

    if (vfx != null && Time.time >= lastVFXTime + vfxCooldown)
    {
      lastVFXTime = Time.time;
      vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
      vfx.Play();
      StartCoroutine(StopVFXAfterDuration(vfx, feedbackDuration));
    }
  }

  private IEnumerator StopVFXAfterDuration(ParticleSystem vfx, float duration)
  {
    yield return new WaitForSeconds(duration);
    if (vfx != null)
    {
      vfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

  private bool IsSocketPhysicallyOccupied(Transform socket)
  {
    if (socketOccupancy.ContainsKey(socket)) return true;

    foreach (GameObject item in physicalItemsSet)
    {
      if (item != null && Vector3.SqrMagnitude(item.transform.position - socket.position) < 0.05f)
      {
        socketOccupancy[socket] = item;
        itemToSocket[item] = socket;
        return true;
      }
    }
    return false;
  }

  private Transform GetSocketForItem(string itemTag, GameObject item)
  {
    if (itemTag == "Blueberry" || itemTag == "GreenTea")
    {
      if (!IsSocketPhysicallyOccupied(drinkSocket1)) return drinkSocket1;
      if (!IsSocketPhysicallyOccupied(drinkSocket2)) return drinkSocket2;
      return null;
    }

    if (itemTag == "Bento1")
    {
      if (!IsSocketPhysicallyOccupied(bento1Socket)) return bento1Socket;
      return null;
    }

    if (itemTag == "Bento2")
    {
      if (!IsSocketPhysicallyOccupied(bento2Socket)) return bento2Socket;
      return null;
    }

    if (itemTag == "Apple")
    {
      if (!IsSocketPhysicallyOccupied(appleSocket1)) return appleSocket1;
      if (!IsSocketPhysicallyOccupied(appleSocket2)) return appleSocket2;
      if (!IsSocketPhysicallyOccupied(appleSocket3)) return appleSocket3;
      return null;
    }

    if (itemTag == "Banana")
    {
      if (!IsSocketPhysicallyOccupied(bananaSocket1)) return bananaSocket1;
      if (!IsSocketPhysicallyOccupied(bananaSocket2)) return bananaSocket2;
      if (!IsSocketPhysicallyOccupied(bananaSocket3)) return bananaSocket3;
      return null;
    }

    if (itemTag == "Orange")
    {
      if (!IsSocketPhysicallyOccupied(orangeSocket1)) return orangeSocket1;
      if (!IsSocketPhysicallyOccupied(orangeSocket2)) return orangeSocket2;
      if (!IsSocketPhysicallyOccupied(orangeSocket3)) return orangeSocket3;
      return null;
    }

    if (itemTag == "Strawberry")
    {
      if (!IsSocketPhysicallyOccupied(strawberrySocket1)) return strawberrySocket1;
      if (!IsSocketPhysicallyOccupied(strawberrySocket2)) return strawberrySocket2;
      if (!IsSocketPhysicallyOccupied(strawberrySocket3)) return strawberrySocket3;
      return null;
    }

    return null;
  }

  private IEnumerator ResetMoneyPhysics(Rigidbody rb)
  {
    yield return new WaitForFixedUpdate();
    if (rb != null)
    {
      rb.isKinematic = false;
      rb.useGravity = true;
    }
  }

  /// <summary>
  /// Lerps an item to its assigned socket position over snapDelay, verifying ownership throughout
  /// </summary>
  private IEnumerator SnapToSocketAfterDelay(GameObject item, Transform socket)
  {
    float elapsedTime = 0f;
    Vector3 startPos = item.transform.position;
    Quaternion startRot = item.transform.rotation;

    while (elapsedTime < snapDelay)
    {
      if (item == null || !physicalItemsSet.Contains(item))
      {
        yield break;
      }

      // Abort if socket was reassigned to another item during animation
      if (!socketOccupancy.TryGetValue(socket, out GameObject owner) || owner != item)
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
      if (socketOccupancy.TryGetValue(socket, out GameObject finalOwner) && finalOwner == item)
      {
        item.transform.position = socket.position;
        item.transform.rotation = socket.rotation;
        snappedItems.Add(item);
      }
      else
      {
        Rigidbody rb = GetCachedRigidbody(item);
        if (rb != null)
        {
          rb.isKinematic = false;
          rb.useGravity = true;
        }
      }
    }

    if (pendingSnaps.ContainsKey(item))
    {
      pendingSnaps.Remove(item);
    }
  }

  private void ValidateOrder()
  {
    itemsOnTray.TryGetValue(requiredFoodTag, out int foodCount);
    itemsOnTray.TryGetValue(requiredDrinkTag, out int drinkCount);

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
        Debug.Log("ORDER COMPLETE!");
        OnOrderComplete();
      }
    }
  }

  private void OnOrderComplete()
  {
    orderActive = false;
    StartCoroutine(ClearItemsBeforePayment());
  }

  private IEnumerator ClearItemsBeforePayment()
  {
    yield return new WaitForSeconds(1.5f);

    ClearPhysicalItems();

    if (orderProgressUI != null)
    {
      orderProgressUI.HideUI();
    }

    if (PaymentHandler.Instance != null)
    {
      PaymentHandler.Instance.StartPaymentPhase();
    }
  }

  /// <summary>
  /// Destroys all physical items on the tray and resets all socket/snap tracking state
  /// </summary>
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
    rbCache.Clear();

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
    physicalItemsSet.Clear();
    itemsOnTray.Clear();
  }

  /// <summary>
  /// Enters payment phase where the tray collects change coins from the player
  /// </summary>
  public void StartPaymentPhase(float changeAmount)
  {
    paymentPhase = true;
    requiredChange = changeAmount;
    collectedChange = 0f;
    wasChangeExact = true;
    Debug.Log($"Payment phase started. Required change: ${requiredChange:F2}");
  }

  private float GetMoneyValue(string tag)
  {
    return moneyValues.TryGetValue(tag, out float val) ? val : 0f;
  }

  private void ValidateChange()
  {
    if (collectedChange >= requiredChange - 0.01f)
    {
      paymentPhase = false;
      hasGivenChange = true;

      bool isOverpaid = collectedChange > requiredChange + 0.01f;
      wasChangeExact = !isOverpaid;
      PlayFeedback(!isOverpaid);

      if (PaymentHandler.Instance != null)
      {
        PaymentHandler.Instance.OnChangeValidated(true, collectedChange);
      }

      CheckTransactionComplete();
    }
  }

  /// <summary>
  /// Called by PaymentCollector when the customer's payment has been picked up
  /// </summary>
  public void OnPaymentCollected()
  {
    hasCollectedPayment = true;
    CheckTransactionComplete();
  }

  private void CheckTransactionComplete()
  {
    if (hasGivenChange && hasCollectedPayment)
    {
      StartCoroutine(DeleteMoneyAfterDelay());
    }
  }

  private IEnumerator DeleteMoneyAfterDelay()
  {
    yield return new WaitForSeconds(1.5f);

    foreach (GameObject money in collectedMoney)
    {
      if (money != null)
      {
        Destroy(money);
      }
    }

    ClearCollectedChange();
  }
}
