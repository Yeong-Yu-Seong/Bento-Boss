/// <summary>
/// File: StudentsBackground.cs
/// Author: Jayden Wong
/// Description: Controls background student NPC wandering using NavMesh with smart crowd-avoidance destination selection.
/// </summary>
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class StudentsBackground : MonoBehaviour
{
  [Header("Wander Settings")]
  [Tooltip("How far the student can walk from their starting point")]
  [SerializeField] private float wanderRadius = 15f;

  [Tooltip("How long to wait at destination before walking again")]
  [SerializeField] private float waitTime = 1.5f;

  [Tooltip("If student is stuck/stopped for this long, pick a new spot")]
  [SerializeField] private float stuckTimeout = 2.0f;

  [Header("Social Settings")]
  [Tooltip("How much personal space they need (Hard Constraint)")]
  [SerializeField] private float personalSpace = 1.0f;

  [Header("Smart Spreading")]
  [Tooltip("How many random points to test before picking the best one. Higher = Smarter but costs CPU.")]
  [Range(1, 15)]
  [SerializeField] private int intelligenceSamples = 8;

  [Tooltip("The radius to check for 'crowds' when scoring a potential destination.")]
  [SerializeField] private float densityScanRadius = 5.0f;

  [Tooltip("Optional: Layer mask for other students so we don't detect walls as people")]
  [SerializeField] private LayerMask studentLayer;

  private NavMeshAgent _agent;
  private Animator _animator;
  private float _waitTimer;
  private float _stuckTimer;

  private static readonly int WalkingParam = Animator.StringToHash("isWalking");

  private void Awake()
  {
    _agent = GetComponent<NavMeshAgent>();
    _animator = GetComponent<Animator>();
    _waitTimer = waitTime;

    _agent.radius = 0.25f;
    _agent.speed = 2.0f;

    // Randomize priority so agents don't deadlock when avoiding each other
    _agent.avoidancePriority = Random.Range(30, 70);
  }

  private void Update()
  {
    bool isMoving = _agent.velocity.sqrMagnitude > 0.1f;

    if (_animator != null)
      _animator.SetBool(WalkingParam, isMoving);

    if (!isMoving && _agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance)
    {
      _stuckTimer += Time.deltaTime;
      if (_stuckTimer >= stuckTimeout)
      {
        SetNewDestination();
        _stuckTimer = 0;
      }
    }
    else
    {
      _stuckTimer = 0;
    }

    if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
    {
      _waitTimer += Time.deltaTime;
      if (_waitTimer >= waitTime)
      {
        SetNewDestination();
        _waitTimer = 0;
      }
    }
  }

  /// <summary>
  /// Samples multiple random NavMesh positions and picks the one with the fewest nearby students
  /// </summary>
  private void SetNewDestination()
  {
    Vector3 bestCandidate = transform.position;
    int lowestNeighborCount = int.MaxValue;
    float furthestDistFromMe = 0f;
    bool foundValid = false;

    for (int i = 0; i < intelligenceSamples; i++)
    {
      Vector3 candidatePos = RandomNavSphere(transform.position, wanderRadius, -1);

      if (IsCrowded(candidatePos)) continue;

      int neighborCount = CountNeighbors(candidatePos, densityScanRadius);

      // Prefer fewest neighbors; tie-break by furthest distance for better spreading
      if (neighborCount < lowestNeighborCount)
      {
        lowestNeighborCount = neighborCount;
        bestCandidate = candidatePos;
        furthestDistFromMe = Vector3.Distance(transform.position, candidatePos);
        foundValid = true;
      }
      else if (neighborCount == lowestNeighborCount)
      {
        float dist = Vector3.Distance(transform.position, candidatePos);
        if (dist > furthestDistFromMe)
        {
          bestCandidate = candidatePos;
          furthestDistFromMe = dist;
        }
      }
    }

    if (foundValid)
    {
      _agent.SetDestination(bestCandidate);
    }
    else
    {
      _agent.SetDestination(RandomNavSphere(transform.position, wanderRadius, -1));
    }
  }

  private bool IsCrowded(Vector3 targetPos)
  {
    Collider[] hitColliders = Physics.OverlapSphere(targetPos, personalSpace, studentLayer);
    foreach (var hit in hitColliders)
    {
      if (hit.gameObject != gameObject) return true;
    }
    return false;
  }

  private int CountNeighbors(Vector3 targetPos, float radius)
  {
    int count = 0;
    Collider[] hitColliders = Physics.OverlapSphere(targetPos, radius, studentLayer);

    foreach (var hit in hitColliders)
    {
      if (hit.gameObject != gameObject)
      {
        count++;
      }
    }
    return count;
  }

  private static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
  {
    Vector3 randDirection = Random.insideUnitSphere * dist;
    randDirection += origin;
    NavMeshHit navHit;
    NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
    return navHit.position;
  }
}
