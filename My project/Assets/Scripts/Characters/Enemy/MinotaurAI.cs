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
    public float sightRange = 10f;
    public float forgetTime = 5f;

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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

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
    }

    void LateUpdate()
    {
        // HARD LOCK 2D PLANE
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, p.y, 0f);
    }

    // ---------------- VISION ----------------

    void UpdateVision()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        canSeePlayer = dist <= sightRange;

        if (canSeePlayer)
        {
            lastKnownPlayerPos = player.position;
            lastSeenTime = Time.time;
        }
    }

    // ---------------- STATES ----------------

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
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            if (stateTimer <= 0)
            {
                ChangeState(State.Wander);
                return;
            }

            Vector3 target = GetSafeRandomPoint(lastKnownPlayerPos, searchRadius);
            SetSafeDestination(target);

            stateTimer -= Time.deltaTime;
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

    // ---------------- STATE LOGIC ----------------

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

        if (Time.time - lastSeenTime > forgetTime && currentState == State.Search)
        {
            ChangeState(State.Wander);
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        if (newState == State.Search)
            stateTimer = searchDuration;
    }

    // ---------------- SAFE NAV HELPERS ----------------

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
                return hit;
        }

        return origin; // fallback safety
    }

    bool TryGetNavMeshPoint(Vector3 source, out Vector3 result)
    {
        NavMeshHit hit;

        bool success = NavMesh.SamplePosition(source, out hit, 2f, NavMesh.AllAreas);

        if (success)
        {
            result = hit.position;
            return true;
        }

        result = source;
        return false;
    }

    // ---------------- NOISE SYSTEM ----------------

    public void TriggerNoise(Vector3 position)
    {
        investigationPos = position;
        ChangeState(State.Investigate);
    }
}