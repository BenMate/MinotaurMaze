using UnityEngine;
using UnityEngine.AI;

public class MinotaurAI : MonoBehaviour
{
    public enum State
    {
        Wander,
        Hunt,
        Search,
        Investigate
    }

    [Header("References")]
    public Transform player;

    [Header("Vision")]
    public float sightRange = 12f;
    public float closeAwarenessRange = 4f;
    public float forgetTime = 5f;
    public LayerMask wallMask;

    [Header("Wander")]
    public float wanderRadius = 10f;
    public float wanderWaitTime = 2f;

    [Header("Search")]
    public float searchDuration = 6f;
    public float searchRadius = 4f;

    private State currentState;
    private Vector3 lastKnownPlayerPos;
    private Vector3 investigationPos;
    private float lastSeenTime;
    private float stateTimer;
    private bool canSeePlayer;

    private NavMeshAgent agent;
    private Animator animator;

    // NEW: stores last valid facing direction
    private Vector2 lastMoveDirection = Vector2.down;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Start()
    {
        ChangeState(State.Wander);
    }

    void Update()
    {
        UpdateVision();

        switch (currentState)
        {
            case State.Wander:
                WanderUpdate();
                break;

            case State.Hunt:
                HuntUpdate();
                break;

            case State.Search:
                SearchUpdate();
                break;

            case State.Investigate:
                InvestigateUpdate();
                break;
        }

        StateTransitions();
        UpdateAnimation();
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    void UpdateVision()
    {
        if (player == null) return;

        canSeePlayer = false;

        Vector2 direction = player.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= closeAwarenessRange)
        {
            canSeePlayer = true;
            lastKnownPlayerPos = player.position;
            lastSeenTime = Time.time;
            return;
        }

        if (distance > sightRange)
            return;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            distance,
            wallMask
        );

        if (hit.collider == null)
        {
            canSeePlayer = true;
            lastKnownPlayerPos = player.position;
            lastSeenTime = Time.time;
        }
    }

    void WanderUpdate()
    {
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            Vector3 target = GetSafeRandomPoint(transform.position, wanderRadius);
            SetSafeDestination(target);
            stateTimer = wanderWaitTime;
        }
    }

    void HuntUpdate()
    {
        if (player == null) return;
        SetSafeDestination(player.position);
    }

    void SearchUpdate()
    {
        stateTimer -= Time.deltaTime;

        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            if (stateTimer <= 0)
            {
                ChangeState(State.Wander);
                return;
            }

            Vector3 target = GetSafeRandomPoint(lastKnownPlayerPos, searchRadius);
            SetSafeDestination(target);
        }
    }

    void InvestigateUpdate()
    {
        SetSafeDestination(investigationPos);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            ChangeState(State.Search);
        }
    }

    void StateTransitions()
    {
        if (canSeePlayer)
        {
            ChangeState(State.Hunt);
            return;
        }

        if (!canSeePlayer && currentState == State.Hunt)
        {
            ChangeState(State.Search);
            stateTimer = searchDuration;
            return;
        }

        if (Time.time - lastSeenTime > forgetTime &&
            currentState == State.Search)
        {
            ChangeState(State.Wander);
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        if (newState == State.Search)
        {
            stateTimer = searchDuration;
        }
    }

    void SetSafeDestination(Vector3 target)
    {
        if (TryGetNavMeshPoint(target, out Vector3 safe))
        {
            agent.SetDestination(safe);
        }
    }

    Vector3 GetSafeRandomPoint(Vector3 origin, float radius)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPoint = origin + Random.insideUnitSphere * radius;

            if (TryGetNavMeshPoint(randomPoint, out Vector3 hit))
            {
                return hit;
            }
        }

        return origin;
    }

    bool TryGetNavMeshPoint(Vector3 source, out Vector3 result)
    {
        NavMeshHit hit;

        bool success = NavMesh.SamplePosition(
            source,
            out hit,
            2f,
            NavMesh.AllAreas);

        if (success)
        {
            result = hit.position;
            return true;
        }

        result = source;
        return false;
    }

    void UpdateAnimation()
    {
        Vector2 velocity = agent.velocity;
        float speed = velocity.magnitude;

        bool isMoving = speed > 0.05f;
        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            Vector2 dir = velocity.normalized;

            if (Mathf.Abs(dir.x) < 0.1f) dir.x = 0;
            if (Mathf.Abs(dir.y) < 0.1f) dir.y = 0;

            if (dir != Vector2.zero)
                lastMoveDirection = dir;
        }

        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);
    }

    public void TriggerNoise(Vector3 position)
    {
        investigationPos = position;
        ChangeState(State.Investigate);
    }
}