using UnityEngine;

public class ServeBentoManager : MonoBehaviour
{
    [Header("UI Messages")]
    public GameObject serveMessage;
    public GameObject takeMessage;
    public GameObject discardMessage;

    public void ServeBento()
    {
        Debug.Log("Bento served");
        ShowMessage(serveMessage);
    }

    public void TakeBento()
    {
        Debug.Log("Bento taken");
        ShowMessage(takeMessage);
    }

    public void DiscardBento()
    {
        Debug.Log("Bento discarded");
        ShowMessage(discardMessage);
    }

    void ShowMessage(GameObject message)
    {
        if (message == null) return;

        message.SetActive(true);
        Invoke(nameof(HideAllMessages), 2f);
    }

    void HideAllMessages()
    {
        if (serveMessage != null) serveMessage.SetActive(false);
        if (takeMessage != null) takeMessage.SetActive(false);
        if (discardMessage != null) discardMessage.SetActive(false);
    }
}
