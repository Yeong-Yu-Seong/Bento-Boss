/// <summary>
/// File: OrderBubbleController.cs
/// Author: Jayden Wong
/// Description: Generates randomized food and drink orders using a shuffled bag system with anti-repeat guardrails.
/// </summary>
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
  [SerializeField] private string[] foodNames = { "Apple", "Banana", "Orange", "Strawberry", "Bento Set 1", "Bento Set 2" };
  [SerializeField] private string[] drinkNames = { "Blueberry Tea", "Green Tea" };

  [Header("Payment Phrases")]
  [SerializeField]
  private string[] paymentPhrases =
  {
    "Thank you so much!\nHere's $",
    "This looks great!\nHere's $",
    "Perfect, thank you!\nHere's $",
    "Appreciate it!\nHere's $",
    "You're the best!\nHere's $"
  };

  [Header("Settings")]
  [SerializeField] private float typingSpeed = 0.05f;

  [HideInInspector] public int requiredFoodID;
  [HideInInspector] public int requiredFoodQuantity;
  [HideInInspector] public int requiredDrinkID;
  [HideInInspector] public int requiredDrinkQuantity;

  private Coroutine typingCoroutine;

  private StringBuilder typingBuilder = new StringBuilder(64);
  private WaitForSeconds typingWait;

  private List<int> foodDeck = new List<int>();
  private List<int> drinkDeck = new List<int>();
  private List<int> paymentDeck = new List<int>();

  private int lastFoodID1 = -1;
  private int lastFoodID2 = -1;
  private int lastFoodCat1 = -1;
  private int lastFoodCat2 = -1;

  private int lastDrink1 = -1;
  private int lastDrink2 = -1;

  // IDs at or above this are Bento items, below are Fruits
  private const int BENTO_START_ID = 4;

  private bool _bentoIntroduced = false;

  private void Start()
  {
    if (mainCamera == null)
    {
      mainCamera = Camera.main;
    }

    if (mainCamera == null)
    {
      Debug.LogError("CRITICAL ERROR: OrderBubbleController cannot find the VR Camera! Please drag your Main Camera into the 'Main Camera' slot in the Inspector.");
    }

    if (bubbleVisuals != null) bubbleVisuals.SetActive(false);

    typingWait = new WaitForSeconds(typingSpeed);

    RebuildFoodDeck();
    RebuildAndShuffle(drinkDeck, drinkNames.Length);
    RebuildAndShuffle(paymentDeck, paymentPhrases.Length);
  }

  private void Update()
  {
    if (!bubbleVisuals.activeSelf || mainCamera == null) return;
    transform.rotation = Quaternion.LookRotation(mainCamera.transform.position - transform.position);
  }

  /// <summary>
  /// Generates a new randomized food and drink order and displays it with a typewriter effect
  /// </summary>
  public void GenerateNewOrder()
  {
    int foodIndex = DrawFood();
    int drinkIndex = DrawFromDeck(drinkDeck, drinkNames.Length, ref lastDrink1, ref lastDrink2);

    switch (foodIndex)
    {
      case 0:
      case 1:
        requiredFoodQuantity = Random.Range(1, 4);
        break;
      case 2:
      case 3:
        requiredFoodQuantity = Random.Range(2, 4);
        break;
      case 4:
      case 5:
      default:
        requiredFoodQuantity = 1;
        break;
    }
    requiredDrinkQuantity = Random.Range(1, 3);

    requiredFoodID = foodIndex;
    requiredDrinkID = drinkIndex;

    string fullSentence = $"I would like:\n{requiredFoodQuantity} {foodNames[foodIndex]}\n{requiredDrinkQuantity} {drinkNames[drinkIndex]}";

    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    typingCoroutine = StartCoroutine(TypewriterEffect(fullSentence));
  }

  /// <summary>
  /// Displays a payment phrase with the given amount using a typewriter effect
  /// </summary>
  public void ShowPayment(float amount)
  {
    if (paymentDeck.Count == 0)
    {
      RebuildAndShuffle(paymentDeck, paymentPhrases.Length);
    }

    int phraseIndex = paymentDeck[0];
    paymentDeck.RemoveAt(0);

    string paymentText = paymentPhrases[phraseIndex] + amount.ToString("F2") + ".";

    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    typingCoroutine = StartCoroutine(TypewriterEffect(paymentText));
  }

  /// <summary>
  /// Rebuilds the food deck, weighting bento items with 2 copies each once earnings reach $15
  /// </summary>
  private void RebuildFoodDeck()
  {
    foodDeck.Clear();
    bool bentoUnlocked = EarningsTracker.Instance != null && EarningsTracker.Instance.CurrentProfit >= 15f;

    for (int i = 0; i < BENTO_START_ID; i++) foodDeck.Add(i);

    if (bentoUnlocked)
    {
      // Bento IDs get 2 copies each for 50/50 weighting against fruits
      for (int i = BENTO_START_ID; i < foodNames.Length; i++)
      {
        foodDeck.Add(i);
        foodDeck.Add(i);
      }
    }

    FisherYatesShuffle(foodDeck);
  }

  private void RebuildAndShuffle(List<int> deck, int itemCount)
  {
    deck.Clear();
    for (int i = 0; i < itemCount; i++)
    {
      deck.Add(i);
    }
    FisherYatesShuffle(deck);
  }

  private void FisherYatesShuffle(List<int> deck)
  {
    for (int i = deck.Count - 1; i > 0; i--)
    {
      int j = Random.Range(0, i + 1);
      int temp = deck[i];
      deck[i] = deck[j];
      deck[j] = temp;
    }
  }

  private int GetFoodCategory(int foodID)
  {
    return foodID >= BENTO_START_ID ? 1 : 0;
  }

  /// <summary>
  /// Draws a food ID with two-layer guardrails preventing same item or same category three times in a row
  /// </summary>
  private int DrawFood()
  {
    // Guaranteed bento on the first order after milestone unlock
    bool bentoUnlocked = EarningsTracker.Instance != null && EarningsTracker.Instance.CurrentProfit >= 15f;
    if (bentoUnlocked && !_bentoIntroduced)
    {
      _bentoIntroduced = true;
      int introID = Random.Range(BENTO_START_ID, foodNames.Length);
      lastFoodID2 = lastFoodID1;
      lastFoodID1 = introID;
      lastFoodCat2 = lastFoodCat1;
      lastFoodCat1 = GetFoodCategory(introID);
      RebuildFoodDeck();
      return introID;
    }

    if (foodDeck.Count == 0)
    {
      RebuildFoodDeck();
    }

    int attempts = 0;
    int maxAttempts = foodDeck.Count + foodNames.Length;

    while (attempts < maxAttempts)
    {
      if (foodDeck.Count == 0)
      {
        RebuildFoodDeck();
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
      RebuildFoodDeck();
    }
    int forced = foodDeck[0];
    foodDeck.RemoveAt(0);

    lastFoodID2 = lastFoodID1;
    lastFoodID1 = forced;
    lastFoodCat2 = lastFoodCat1;
    lastFoodCat1 = GetFoodCategory(forced);

    return forced;
  }

  /// <summary>
  /// Draws from a shuffled deck with a single guardrail preventing the same ID three times in a row
  /// </summary>
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

  /// <summary>
  /// Displays text character by character using a StringBuilder to avoid string allocations
  /// </summary>
  private IEnumerator TypewriterEffect(string sentence)
  {
    bubbleVisuals.SetActive(true);
    typingBuilder.Clear();
    textDisplay.text = "";

    foreach (char letter in sentence)
    {
      typingBuilder.Append(letter);
      textDisplay.text = typingBuilder.ToString();
      yield return typingWait;
    }
  }

  /// <summary>
  /// Hides the order bubble and clears the displayed text
  /// </summary>
  public void HideOrder()
  {
    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    bubbleVisuals.SetActive(false);
    textDisplay.text = "";
  }
}
