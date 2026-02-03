using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

public class SocketStockManager : MonoBehaviour
{
    [Header("--- Stock Counting ---")]
    public List<XRSocketInteractor> mySockets; 
    public TextMeshProUGUI stockText;
    
    [Header("--- Spawning Logic ---")]
    public Button uiButton;
    public GameObject cratePrefab;
    public Transform spawnLocation;
    public float cooldownTime = 3f;            
    private bool isCoolingDown = false;
    
    [Header("--- Trash Disposal ---")]
    public TextMeshProUGUI trashCountText;
    private int trashDisposed = 0;

    // Cache the count to avoid unnecessary UI updates
    private int lastCount = -1;

    void Start()
    {
        if (uiButton != null)
        {
            uiButton.onClick.AddListener(TrySpawnCrate);
        }
        
        foreach (var socket in mySockets)
        {
            if (socket != null)
            {
                // Subscribe to events
                socket.selectEntered.AddListener(OnSocketChanged);
                socket.selectExited.AddListener(OnSocketChanged);
            }
        }
        
        UpdateStockDisplay(true); // Initial forced update
        UpdateTrashDisplay();
    }
    
    // Use OnDisable in addition to OnDestroy for better VR cleanup
    void OnDisable()
    {
        foreach (var socket in mySockets)
        {
            if (socket != null)
            {
                socket.selectEntered.RemoveListener(OnSocketChanged);
                socket.selectExited.RemoveListener(OnSocketChanged);
            }
        }
    }
    
    // --- FIX 2: Event-driven updates ---
    void OnSocketChanged(BaseInteractionEventArgs args)
    {
        UpdateStockDisplay();
    }
    
    void UpdateStockDisplay(bool forceUpdate = false)
    {
        int currentCount = 0;
        foreach (var socket in mySockets)
        {
            if (socket == null) continue;
            
            // Check if socket has an object
            if (socket.hasSelection || socket.interactablesSelected.Count > 0)
            {
                currentCount++;
            }
        }

        // Only update UI and allocate strings if the number actually changed
        if (forceUpdate || currentCount != lastCount)
        {
            lastCount = currentCount;
            if (stockText != null)
            {
                // Using string interpolation is slightly cleaner, but the key is only doing it once!
                stockText.text = $"Stock: {currentCount}";
                stockText.color = (currentCount == 0) ? Color.red : Color.black;
            }
        }
    }
    
    void UpdateTrashDisplay()
    {
        if (trashCountText != null)
        {
            trashCountText.text = $"Trash Disposed: {trashDisposed}";
        }
    }
    
    public void DisposeTrash(GameObject trashedObject)
    {
        if (trashedObject == null) return; // Safety check
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