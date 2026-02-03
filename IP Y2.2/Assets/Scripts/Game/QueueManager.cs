using UnityEngine;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    [Header("Queue Settings")]
    [SerializeField] private Transform[] waypoints; // Spot1, Spot2, Spot3, Spot4, ResetSpot
    [SerializeField] private GameObject[] students; // The 4 NPC models
    [SerializeField] private float walkSpeed = 2.0f;

    [Header("Dependencies")]
    [SerializeField] private OrderBubbleController orderBubble; 
    [SerializeField] private TrayValidator trayValidator; // Reference to TrayValidator

    private int[] studentTargetIndices;
    private bool hasOrdered = false;

    // SINGLETON PATTERN: Allows other scripts to find this manager easily
    public static QueueManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (students.Length == 0 || waypoints.Length < 5)
        {
            Debug.LogError("QueueManager: Assign all students and waypoints in inspector.");
            return;
        }

        if (trayValidator == null)
        {
            Debug.LogError("QueueManager: TrayValidator reference is missing! Please assign it in the Inspector.");
        }

        // Initialize targets
        studentTargetIndices = new int[students.Length];
        for (int i = 0; i < students.Length; i++)
        {
            studentTargetIndices[i] = i; 
        }
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        for (int i = 0; i < students.Length; i++)
        {
            Transform target = waypoints[studentTargetIndices[i]];
            Vector3 direction = target.position - students[i].transform.position;
            float distance = direction.magnitude;

            if (distance > 0.1f)
            {
                // Move
                students[i].transform.position = Vector3.MoveTowards(
                    students[i].transform.position, 
                    target.position, 
                    walkSpeed * Time.deltaTime
                );

                // Rotate
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                students[i].transform.rotation = Quaternion.Slerp(
                    students[i].transform.rotation, 
                    targetRotation, 
                    Time.deltaTime * 5f
                );
            }
            
            // Trigger Order Logic when arriving at Spot 1
            if (studentTargetIndices[i] == 0 && distance < 0.1f)
            {
                if (!hasOrdered)
                {
                    // Call TrayValidator to start the new order
                    if (trayValidator != null)
                    {
                        trayValidator.StartNewOrder();
                    }
                    else
                    {
                        // Fallback to old behavior if TrayValidator isn't set
                        if (orderBubble != null) orderBubble.GenerateNewOrder();
                    }
                    hasOrdered = true;
                }
            }
        }
    }

    // CALL THIS FUNCTION when the order is complete
    // TrayValidator will call this automatically when player places correct items on tray
    public void ShiftQueue()
    {
        hasOrdered = false; 

        // Cycle spots
        for (int i = 0; i < students.Length; i++)
        {
            if (studentTargetIndices[i] == 0) studentTargetIndices[i] = 4;      // Spot 1 -> Reset
            else if (studentTargetIndices[i] == 4) studentTargetIndices[i] = 3; // Reset -> Spot 4
            else studentTargetIndices[i] -= 1;                                  // Move Up
        }

        Debug.Log("Queue shifted to next customer.");
    }
}