using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class OrderBubbleController : MonoBehaviour
{
  [Header("Dependencies")]
  [Tooltip("Drag your Main Camera (from XR Origin) here")]
  [SerializeField] private Camera mainCamera;

  [Header("UI Components")]
  [SerializeField] private GameObject bubbleVisuals;
  [SerializeField] private TextMeshProUGUI textDisplay;

  [Header("Menu Items")]
  // Unified food pool: IDs 0-3 = Fruits, IDs 4-5 = Bentos
  [SerializeField] private string[] foodNames = { "Apple", "Banana", "Orange", "Strawberry", "Bento Set 1", "Bento Set 2" };
  [SerializeField] private string[] drinkNames = { "Blueberry Tea", "Green Tea" };

  [Header("Settings")]
  [SerializeField] private float typingSpeed = 0.05f; // Lower is faster

  // PUBLIC VARIABLES (Your Stock Script will read these later!)
  [HideInInspector] public int requiredFoodID;
  [HideInInspector] public int requiredFoodQuantity;  // Fruit: 1-3, Bento: always 1
  [HideInInspector] public int requiredDrinkID;
  [HideInInspector] public int requiredDrinkQuantity; // Always 1-2

  private Coroutine typingCoroutine;

  // Cached allocations for typewriter effect
  private StringBuilder typingBuilder = new StringBuilder(64);
  private WaitForSeconds typingWait;

  // --- SHUFFLED BAG SYSTEM ---
  private List<int> foodDeck = new List<int>();
  private List<int> drinkDeck = new List<int>();

  // --- FOOD HISTORY (tracks last 2 draws) ---
  private int lastFoodID1 = -1;   // Most recent food ID drawn
  private int lastFoodID2 = -1;   // Second most recent food ID drawn
  private int lastFoodCat1 = -1;  // Most recent food category (0 = Fruit, 1 = Bento)
  private int lastFoodCat2 = -1;  // Second most recent food category

  // --- DRINK HISTORY (tracks last 2 draws) ---
  private int lastDrink1 = -1;
  private int lastDrink2 = -1;
  // --- END BAG SYSTEM VARS ---

  // The boundary ID: anything below this is a Fruit, anything at or above is a Bento
  private const int BENTO_START_ID = 4;

  private void Start()
  {
    // Fallback: If you forgot to drag it in, try to find it automatically
    if (mainCamera == null)
    {
      mainCamera = Camera.main;
    }

    // CRITICAL ERROR CHECK
    if (mainCamera == null)
    {
      Debug.LogError("CRITICAL ERROR: OrderBubbleController cannot find the VR Camera! Please drag your Main Camera into the 'Main Camera' slot in the Inspector.");
    }

    // Hide bubble immediately on start
    if (bubbleVisuals != null) bubbleVisuals.SetActive(false);

    // Cache the WaitForSeconds used in typewriter effect
    typingWait = new WaitForSeconds(typingSpeed);

    // --- Initialize and shuffle both decks on Start ---
    RebuildAndShuffle(foodDeck, foodNames.Length);   // 6 cards: [0,1,2,3,4,5]
    RebuildAndShuffle(drinkDeck, drinkNames.Length); // 2 cards: [0,1]
  }

  private void Update()
  {
    if (!bubbleVisuals.activeSelf || mainCamera == null) return;
    transform.rotation = Quaternion.LookRotation(mainCamera.transform.position - transform.position);
  }

  // Call this from QueueManager when student arrives at Spot 1
  public void GenerateNewOrder()
  {
    // 1. Draw food and drink IDs using shuffled bag guardrails
    int foodIndex = DrawFood();
    int drinkIndex = DrawFromDeck(drinkDeck, drinkNames.Length, ref lastDrink1, ref lastDrink2);

    // 2. Roll quantities based on category
    //    Fruit  -> 1 to 3
    //    Bento  -> always 1
    //    Drink  -> 1 to 2
    int foodCategory = GetFoodCategory(foodIndex);
    requiredFoodQuantity = (foodCategory == 0) ? Random.Range(1, 4) : 1; // Fruit: 1-3, Bento: 1
    requiredDrinkQuantity = Random.Range(1, 3);                            // Drink: 1-2

    // 3. Save IDs for Stock System
    requiredFoodID = foodIndex;
    requiredDrinkID = drinkIndex;

    // 4. Construct the Sentence
    string fullSentence = $"I would like:\n{requiredFoodQuantity} {foodNames[foodIndex]}\n{requiredDrinkQuantity} {drinkNames[drinkIndex]}";

    // 5. Start Visuals
    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    typingCoroutine = StartCoroutine(TypewriterEffect(fullSentence));
  }

  public void ShowPayment(float amount)
  {
    string paymentText = $"Here's ${amount:F2}";

    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    typingCoroutine = StartCoroutine(TypewriterEffect(paymentText));
  }

  // --- SHUFFLED BAG METHODS ---

  // Fills a deck with one copy of each ID (0 to count-1), then shuffles it
  private void RebuildAndShuffle(List<int> deck, int itemCount)
  {
    deck.Clear();
    for (int i = 0; i < itemCount; i++)
    {
      deck.Add(i);
    }
    FisherYatesShuffle(deck);
  }

  // Fisher-Yates shuffle: the standard unbiased shuffle algorithm
  private void FisherYatesShuffle(List<int> deck)
  {
    for (int i = deck.Count - 1; i > 0; i--)
    {
      int j = Random.Range(0, i + 1); // inclusive of i
      int temp = deck[i];
      deck[i] = deck[j];
      deck[j] = temp;
    }
  }

  // Returns 0 if the ID is a Fruit, 1 if it is a Bento
  private int GetFoodCategory(int foodID)
  {
    return foodID >= BENTO_START_ID ? 1 : 0;
  }

  // --- FOOD DRAW: Two-layer guardrail ---
  private int DrawFood()
  {
    if (foodDeck.Count == 0)
    {
      RebuildAndShuffle(foodDeck, foodNames.Length);
    }

    int attempts = 0;
    int maxAttempts = foodDeck.Count + foodNames.Length;

    while (attempts < maxAttempts)
    {
      if (foodDeck.Count == 0)
      {
        RebuildAndShuffle(foodDeck, foodNames.Length);
      }

      int candidate = foodDeck[0];
      foodDeck.RemoveAt(0);
      int candidateCategory = GetFoodCategory(candidate);

      bool itemBlocked = (candidate == lastFoodID1 && candidate == lastFoodID2);
      bool categoryBlocked = (candidateCategory == lastFoodCat1 && candidateCategory == lastFoodCat2);

      if (itemBlocked || categoryBlocked)
      {
        foodDeck.Add(candidate);
        attempts++;
        continue;
      }

      lastFoodID2 = lastFoodID1;
      lastFoodID1 = candidate;
      lastFoodCat2 = lastFoodCat1;
      lastFoodCat1 = candidateCategory;

      return candidate;
    }

    Debug.LogWarning("DrawFood: safety cap hit. Forcing next card.");
    if (foodDeck.Count == 0)
    {
      RebuildAndShuffle(foodDeck, foodNames.Length);
    }
    int forced = foodDeck[0];
    foodDeck.RemoveAt(0);

    lastFoodID2 = lastFoodID1;
    lastFoodID1 = forced;
    lastFoodCat2 = lastFoodCat1;
    lastFoodCat1 = GetFoodCategory(forced);

    return forced;
  }

  // --- DRINK DRAW: Single guardrail (exact ID only) ---
  private int DrawFromDeck(List<int> deck, int itemCount, ref int last1, ref int last2)
  {
    if (deck.Count == 0)
    {
      RebuildAndShuffle(deck, itemCount);
    }

    int drawn = deck[0];
    deck.RemoveAt(0);

    if (drawn == last1 && drawn == last2)
    {
      deck.Add(drawn);

      if (deck.Count == 0)
      {
        RebuildAndShuffle(deck, itemCount);
      }

      drawn = deck[0];
      deck.RemoveAt(0);
    }

    last2 = last1;
    last1 = drawn;

    return drawn;
  }

  // --- END SHUFFLED BAG METHODS ---

  // The "Cool Typewriter" Logic — uses StringBuilder to avoid string allocations
  private IEnumerator TypewriterEffect(string sentence)
  {
    bubbleVisuals.SetActive(true);
    typingBuilder.Clear();
    textDisplay.text = "";

    foreach (char letter in sentence)
    {
      typingBuilder.Append(letter);
      textDisplay.SetText(typingBuilder);
      yield return typingWait;
    }
  }

  // Call this when you successfully serve them
  public void HideOrder()
  {
    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    bubbleVisuals.SetActive(false);
    textDisplay.text = "";
  }
}
