using UnityEngine;
using TMPro;

public class ServeBentoButton : MonoBehaviour
{
    public GameObject successMessage;

    public void OnServeBentoPressed()
    {
        Debug.Log("Serve Bento pressed");

        ShowSuccessMessage();
    }

    void ShowSuccessMessage()
    {
        successMessage.SetActive(true);
        Invoke(nameof(HideSuccessMessage), 2f);
    }

    void HideSuccessMessage()
    {
        successMessage.SetActive(false);
    }
}

