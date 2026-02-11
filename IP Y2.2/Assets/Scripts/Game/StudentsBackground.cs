using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class StudentsBackground : MonoBehaviour
{
  [Header("Wander Settings")]
  [Tooltip("How far the student can walk from their starting point")]
  [SerializeField] private float wanderRadius = 15f; // Increased slightly to encourage spreading

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

    // Anti-Jam Priority
    _agent.avoidancePriority = Random.Range(30, 70);
  }

  private void Update()
  {
    bool isMoving = _agent.velocity.sqrMagnitude > 0.1f;

    if (_animator != null)
      _animator.SetBool(WalkingParam, isMoving);

    // Stuck Detection
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

    // Arrival Logic
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

  private void SetNewDestination()
  {
    Vector3 bestCandidate = transform.position;
    int lowestNeighborCount = int.MaxValue;
    float furthestDistFromMe = 0f;
    bool foundValid = false;

    // --- THE SMART LOGIC ---
    // Instead of trying 5 times to find *any* valid spot,
    // We generate 'intelligenceSamples' spots and pick the BEST one.
    for (int i = 0; i < intelligenceSamples; i++)
    {
      Vector3 candidatePos = RandomNavSphere(transform.position, wanderRadius, -1);

      // 1. HARD CHECK: Is this spot physically blocked by someone standing there?
      if (IsCrowded(candidatePos)) continue;

      // 2. SOFT CHECK: How busy is the general area?
      int neighborCount = CountNeighbors(candidatePos, densityScanRadius);

      // 3. SCORING:
      // We prefer the spot with the FEWEST neighbors.
      // Tie-breaker: If two spots are equally empty, pick the one furthest from my current spot.
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

    // If we found a calculated best spot, go there.
    if (foundValid)
    {
      _agent.SetDestination(bestCandidate);
    }
    else
    {
      // Fallback: Just pick a random point if we are totally hemmed in
      _agent.SetDestination(RandomNavSphere(transform.position, wanderRadius, -1));
    }
  }

  // Checks if a specific spot is literally taken (Personal Space)
  private bool IsCrowded(Vector3 targetPos)
  {
    // Check smaller radius just for collision prevention
    Collider[] hitColliders = Physics.OverlapSphere(targetPos, personalSpace, studentLayer);
    foreach (var hit in hitColliders)
    {
      if (hit.gameObject != gameObject) return true; // It's crowded by someone else
    }
    return false;
  }

  // Counts how many agents are in the general area (Social Density)
  private int CountNeighbors(Vector3 targetPos, float radius)
  {
    int count = 0;
    // Check larger radius for social density
    Collider[] hitColliders = Physics.OverlapSphere(targetPos, radius, studentLayer);

    foreach (var hit in hitColliders)
    {
      // If it's a student and not ME, add to count
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
