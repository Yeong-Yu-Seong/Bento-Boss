/// <summary>
/// File: QueueManager.cs
/// Author: Jayden Wong
/// Description: Manages a circular queue of student NPCs walking between waypoints and triggering orders at the front.
/// </summary>
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
  private Animator[] studentAnimators;

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
    studentAnimators = new Animator[students.Length];
    for (int i = 0; i < students.Length; i++)
    {
      studentTargetIndices[i] = i;
      studentAnimators[i] = students[i].GetComponentInChildren<Animator>();
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
      direction.y = 0;
      float sqrDistance = direction.sqrMagnitude;
      bool isMoving = sqrDistance > 0.01f;

      if (studentAnimators[i] != null)
        studentAnimators[i].SetBool("isWalking", isMoving);

      if (isMoving)
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

      if (studentTargetIndices[i] == 0 && !isMoving && !hasOrdered)
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

  /// <summary>
  /// Rotates all students to their next waypoint in the circular queue
  /// </summary>
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
