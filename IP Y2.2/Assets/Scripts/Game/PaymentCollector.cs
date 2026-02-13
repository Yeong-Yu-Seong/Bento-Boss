/// <summary>
/// File: PaymentCollector.cs
/// Author: Jayden Wong
/// Description: Detects customer payment money entering the trigger zone and forwards the amount to PaymentHandler.
/// </summary>
using UnityEngine;
using System.Collections.Generic;

public class PaymentCollector : MonoBehaviour
{
  private float collectedAmount = 0f;

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

  private void OnTriggerEnter(Collider other)
  {
    if (other.gameObject.layer != LayerMask.NameToLayer("CustomerPayment")) return;

    float value = GetMoneyValue(other.tag);

    if (value > 0f)
    {
      collectedAmount += value;

      Debug.Log($"Collected ${value:F2}, Total: ${collectedAmount:F2}");

      if (PaymentHandler.Instance != null)
      {
        PaymentHandler.Instance.OnPaymentReceived(collectedAmount);
      }

      if (TrayValidator.Instance != null)
      {
        TrayValidator.Instance.OnPaymentCollected();
      }

      Destroy(other.gameObject);
      collectedAmount = 0f;
      gameObject.SetActive(false);
    }
  }

  private float GetMoneyValue(string tag)
  {
    return moneyValues.TryGetValue(tag, out float val) ? val : 0f;
  }
}
