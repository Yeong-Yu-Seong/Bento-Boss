using UnityEngine;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints; // Spot1, Spot2, Spot3, Spot4, ResetSpot
    [SerializeField] private GameObject[] students; // The 4 NPC models
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float testTimerLimit = 4.0f;

    private int[] studentTargetIndices;
    private float timer;

    private void Start()
    {
        // Guard clause: ensure arrays are populated
        if (students.Length == 0 || waypoints.Length < 5)
        {
            Debug.LogError("QueueManager: Assign all students and waypoints in inspector.");
            return;
        }

        // Initialize each student's starting target
        studentTargetIndices = new int[students.Length];
        for (int i = 0; i < students.Length; i++)
        {
            studentTargetIndices[i] = i; 
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleTestingTimer();
    }

    private void HandleMovement()
    {
        for (int i = 0; i < students.Length; i++)
        {
            Transform target = waypoints[studentTargetIndices[i]];
            Vector3 direction = target.position - students[i].transform.position;

            if (direction.magnitude > 0.1f)
            {
                // Move towards target
                students[i].transform.position = Vector3.MoveTowards(
                    students[i].transform.position, 
                    target.position, 
                    walkSpeed * Time.deltaTime
                );

                // Rotate to face target
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                students[i].transform.rotation = Quaternion.Slerp(
                    students[i].transform.rotation, 
                    targetRotation, 
                    Time.deltaTime * 5f
                );
            }
        }
    }

    private void HandleTestingTimer()
    {
        timer += Time.deltaTime;

        if (timer >= testTimerLimit)
        {
            ShiftQueue();
            timer = 0;
        }
    }

    public void ShiftQueue()
    {
        for (int i = 0; i < students.Length; i++)
        {
            // If student is at Spot1 (Index 0), send to ResetSpot (Index 4)
            if (studentTargetIndices[i] == 0)
            {
                studentTargetIndices[i] = 4;
            }
            // If student is at ResetSpot, send to Spot4 (Index 3)
            else if (studentTargetIndices[i] == 4)
            {
                studentTargetIndices[i] = 3;
            }
            // Otherwise, move them up one spot
            else
            {
                studentTargetIndices[i] -= 1;
            }
        }
    }
}