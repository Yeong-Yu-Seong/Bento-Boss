using UnityEngine;

public class ItemPrices : MonoBehaviour
{
    public static ItemPrices Instance;

    private readonly float[] foodPrices = { 0.80f, 0.60f, 1.00f, 1.20f, 4.50f, 5.50f };
    private readonly float[] drinkPrices = { 1.50f, 1.30f };

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

    public float CalculateOrderTotal(int foodID, int foodQuantity, int drinkID, int drinkQuantity)
    {
        if (foodID < 0 || foodID >= foodPrices.Length)
        {
            Debug.LogError($"ItemPrices: Invalid food ID {foodID}");
            return 0f;
        }

        if (drinkID < 0 || drinkID >= drinkPrices.Length)
        {
            Debug.LogError($"ItemPrices: Invalid drink ID {drinkID}");
            return 0f;
        }

        float foodCost = foodPrices[foodID] * foodQuantity;
        float drinkCost = drinkPrices[drinkID] * drinkQuantity;
        float total = foodCost + drinkCost;

        return total;
    }

    public float GetFoodPrice(int foodID)
    {
        if (foodID < 0 || foodID >= foodPrices.Length) return 0f;
        return foodPrices[foodID];
    }

    public float GetDrinkPrice(int drinkID)
    {
        if (drinkID < 0 || drinkID >= drinkPrices.Length) return 0f;
        return drinkPrices[drinkID];
    }
}