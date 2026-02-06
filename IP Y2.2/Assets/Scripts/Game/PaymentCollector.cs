using UnityEngine;

public class PaymentCollector : MonoBehaviour
{
    private float collectedAmount = 0f;

    private void OnTriggerEnter(Collider other)
    {
        float value = GetMoneyValue(other.tag);
        
        if (value > 0f)
        {
            collectedAmount += value;
            
            Debug.Log($"Collected ${value:F2}, Total: ${collectedAmount:F2}");
            
            if (PaymentHandler.Instance != null)
            {
                PaymentHandler.Instance.OnPaymentReceived(collectedAmount);
            }
            
            Destroy(other.gameObject);
            collectedAmount = 0f;
            gameObject.SetActive(false);
        }
    }

    private float GetMoneyValue(string tag)
    {
        if (tag == "Money_10Cent") return 0.10f;
        if (tag == "Money_20Cent") return 0.20f;
        if (tag == "Money_50Cent") return 0.50f;
        if (tag == "Money_1Dollar") return 1.00f;
        if (tag == "Money_2Dollar") return 2.00f;
        if (tag == "Money_5Dollar") return 5.00f;
        if (tag == "Money_10Dollar") return 10.00f;
        return 0f;
    }
}