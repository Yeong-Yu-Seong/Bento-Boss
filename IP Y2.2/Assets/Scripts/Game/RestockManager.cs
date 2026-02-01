using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit; 
using System.Collections.Generic;

public class SocketStockManager : MonoBehaviour
{
    [Header("--- Stock Counting ---")]
    public List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor> mySockets; 
    public TextMeshProUGUI stockText;
    
    [Header("--- Spawning Logic ---")]
    public Button uiButton;
    public GameObject cratePrefab;
    public Transform spawnLocation;
    public float cooldownTime = 3f;            
    private bool isCoolingDown = false;
    
    [Header("--- Trash Disposal ---")]
    public TextMeshProUGUI trashCountText;  // Drag a UI Text element here
    private int trashDisposed = 0;
    
    void Start()
    {
        if (uiButton != null)
        {
            uiButton.onClick.AddListener(TrySpawnCrate);
        }
        
        // Subscribe to socket events for immediate updates
        foreach (var socket in mySockets)
        {
            if (socket != null)
            {
                socket.selectEntered.AddListener(OnSocketFilled);
                socket.selectExited.AddListener(OnSocketEmptied);
            }
        }
        
        UpdateStockDisplay();
        UpdateTrashDisplay();
    }
    
    void OnDestroy()
    {
        // Clean up listeners
        foreach (var socket in mySockets)
        {
            if (socket != null)
            {
                socket.selectEntered.RemoveListener(OnSocketFilled);
                socket.selectExited.RemoveListener(OnSocketEmptied);
            }
        }
    }
    
    void OnSocketFilled(SelectEnterEventArgs args)
    {
        UpdateStockDisplay();
    }
    
    void OnSocketEmptied(SelectExitEventArgs args)
    {
        UpdateStockDisplay();
    }
    
    void Update()
    {
        // Still update every frame as backup
        UpdateStockDisplay();
    }
    
    void UpdateStockDisplay()
    {
        int currentCount = 0;
        
        foreach (var socket in mySockets)
        {
            if (socket == null) continue;
            
            // Try multiple methods to check if socket has an object
            if (socket.hasSelection || 
                socket.interactablesSelected.Count > 0 ||
                socket.firstInteractableSelected != null)
            {
                currentCount++;
            }
        }
        
        if (stockText != null)
        {
            stockText.text = "Stock: " + currentCount.ToString();
            stockText.color = (currentCount == 0) ? Color.red : Color.black;
        }
    }
    
    void UpdateTrashDisplay()
    {
        if (trashCountText != null)
        {
            trashCountText.text = "Trash Disposed: " + trashDisposed.ToString();
        }
    }
    
    // Call this function from the trash can trigger
    public void DisposeTrash(GameObject trashedObject)
    {
        trashDisposed++;
        UpdateTrashDisplay();
        Destroy(trashedObject);
    }
    
    public void TrySpawnCrate()
    {
        if (isCoolingDown) return;
        
        if (cratePrefab != null && spawnLocation != null)
        {
            Instantiate(cratePrefab, spawnLocation.position, spawnLocation.rotation);
        }
        
        isCoolingDown = true;
        if(uiButton != null) uiButton.interactable = false;
        
        Invoke("ResetCooldown", cooldownTime);
    }
    
    void ResetCooldown()
    {
        isCoolingDown = false;
        if(uiButton != null) uiButton.interactable = true;
    }
}