using UnityEngine;
using System.Collections;

public class QueueManager : MonoBehaviour
{
    [Header("Queue Settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private GameObject[] students;
    [SerializeField] private float walkSpeed = 2.0f;

    [Header("Dependencies")]
    [SerializeField] private OrderBubbleController orderBubble; 
    [SerializeField] private TrayValidator trayValidator;

    private int[] studentTargetIndices;
    private bool hasOrdered = false;
    
    private float movementCheckInterval = 0.02f;
    private float lastMovementCheck = 0f;

    public static QueueManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (students.Length == 0 || waypoints.Length < 4)
        {
            Debug.LogError("QueueManager: Assign all students and waypoints in inspector.");
            return;
        }

        if (trayValidator == null)
        {
            Debug.LogError("QueueManager: TrayValidator reference is missing! Please assign it in the Inspector.");
        }

        studentTargetIndices = new int[students.Length];
        for (int i = 0; i < students.Length; i++)
        {
            studentTargetIndices[i] = i; 
        }
    }

    private void Update()
    {
        if (Time.time - lastMovementCheck >= movementCheckInterval)
        {
            HandleMovement();
            lastMovementCheck = Time.time;
        }
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
                students[i].transform.position = Vector3.MoveTowards(
                    students[i].transform.position, 
                    target.position, 
                    walkSpeed * Time.deltaTime
                );

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                students[i].transform.rotation = Quaternion.Slerp(
                    students[i].transform.rotation, 
                    targetRotation, 
                    Time.deltaTime * 5f
                );
            }
            
            if (studentTargetIndices[i] == 0 && distance < 0.1f && !hasOrdered)
            {
                if (trayValidator != null)
                {
                    trayValidator.StartNewOrder();
                }
                else
                {
                    if (orderBubble != null) orderBubble.GenerateNewOrder();
                }
                hasOrdered = true;
            }
        }
    }

    public void ShiftQueue()
    {
        for (int i = 0; i < students.Length; i++)
        {
            if (studentTargetIndices[i] == 0) studentTargetIndices[i] = 3;
            else if (studentTargetIndices[i] == 3) studentTargetIndices[i] = 2;
            else studentTargetIndices[i] -= 1;
        }

        StartCoroutine(ResetOrderFlag());
        Debug.Log("Queue shifted to next customer.");
    }

    private IEnumerator ResetOrderFlag()
    {
        yield return new WaitForSeconds(0.5f);
        hasOrdered = false;
    }
}