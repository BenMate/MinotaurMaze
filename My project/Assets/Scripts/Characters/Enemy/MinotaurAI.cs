
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MinotaurAI : MonoBehaviour
{
    public enum State
    {
        Wander,
        Hunt,
        Search,
        Investigate,
        Confused
    }

    private enum ChargePhase
    {
        None,
        Windup,
        Charging,
        Recovery
    }

    private enum InvestigationPhase
    {
        ListenAtNoise,
        MoveToNoise,
        LookAround,
        MoveToNearby,
        PauseAtNearby,
        FinalListen
    }

    [Header("References")]
    public Transform player;
    public SceneLoader sceneManager;

    [Header("Vision")]
    public float sightRange = 12f;
    public float closeAwarenessRange = 4f;
    public float forgetTime = 5f;
    public LayerMask wallMask;

    [Header("Movement Speeds")]
    public float wanderSpeed = 2f;
    public float searchSpeed = 2f;
    public float investigateSpeed = 2f;
    public float huntSpeed = 4f;

    [Header("Wander")]
    public float wanderRadius = 10f;
    public float wanderWaitTime = 2f;
    public float wanderWaitVariation = 1f;
    public float wanderAreaDuration = 30f;
    public float wanderAreaRadius = 8f;
    public float wanderEscapeDistance = 20f;
    public float wanderEdgeClearance = 1.5f;

    [Header("Search")]
    public float searchDuration = 12f;
    public float searchRadius = 4f;
    public float searchPauseMin = 1f;
    public float searchPauseMax = 2.5f;

    [Header("Investigation")]
    public float investigationListenTime = 2f;
    public float investigationPauseMin = 1f;
    public float investigationPauseMax = 2.5f;
    public float investigationNearbyRadius = 4f;
    public int minInvestigationTurns = 2;
    public int maxInvestigationTurns = 5;
    public int minInvestigationLocations = 2;
    public int maxInvestigationLocations = 3;

    [Header("Charge Settings")]
    [SerializeField] private float chargeStartRange = 8f;
    [SerializeField] private float chargeWindupTime = 1.2f;
    [SerializeField] private float chargeSpeed = 7.5f;
    [SerializeField] private float chargeCollisionRadius = 0.25f;
    [SerializeField] private float chargeRecoveryTime = 1f;
    [SerializeField] private float chargeCooldownTime = 20f;
    private float chargeCooldownTimer;

    [Header("Confused Settings")]
    [SerializeField] private float confusedTurnSpeed = 120f;
    [SerializeField] private float confusedSearchDuration = 8f;
    private float confusedTimer;

    private ChargePhase chargePhase = ChargePhase.None;
    private Vector2 chargeDirection;
    private float chargeTimer;

    [Header("Grab")]
    public float grabRange = 1.2f;
    public float killRange = 1.5f;
    public float grabWindup = 1f;

    private State currentState;
    private bool hasEnteredState = false;

    private Vector3 lastKnownPlayerPos;
    private Vector3 investigationPos;

    private float lastSeenTime;
    private float stateTimer;
    private bool canSeePlayer;

    private bool isAttemptingGrab;
    private float grabTimer;

    private bool ambushHuntActive;

    private NavMeshAgent agent;
    private Animator animator;

    private Vector2 lastMoveDirection = Vector2.down;

    private readonly HashSet<SafeZone> activeSafeZones = new();

    private bool playerIsInSafeZone;

    // Wander information.
    private Vector3 wanderAreaOrigin;
    private float wanderAreaTimer;
    private float wanderWaitTimer;
    private bool wanderEscapingArea;

    // Search information.
    private bool searchPaused;
    private float searchPauseTimer;

    // Investigation information.
    private InvestigationPhase investigationPhase;
    private float investigationTimer;
    private int investigationTurnsRemaining;
    private int investigationLocationsRemaining;
    private bool investigationMoving;
    private Vector3 investigationTarget;

    private bool returningToNavMesh;
    private Vector3 navMeshReturnTarget;

    [SerializeField] private float navMeshReturnSpeed = 5f;
    [SerializeField] private float navMeshReturnRadius = 2f;


    private bool isDead;

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
        if (agent == null || animator == null || !agent.enabled)
            return;

        if (chargeCooldownTimer > 0f) chargeCooldownTimer -= Time.deltaTime;

        UpdateVision();

        if (isDead)
        {
            UpdateAnimation();
            return;
        }

        if (returningToNavMesh)
        {
            ReturnToNavMesh();
            UpdateAnimation();
            return;
        }

        if (playerIsInSafeZone)
        {
            if (currentState != State.Wander)
                ChangeState(State.Wander);

            agent.isStopped = false;
            WanderUpdate();
            UpdateAnimation();
            return;
        }

        StateTransitions();

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

            case State.Confused:
                ConfusedUpdate();
                break;
        }

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        Vector3 velocity = agent.velocity;
        velocity.z = 0f;

        bool isMoving = velocity.sqrMagnitude > 0.01f;

        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            lastMoveDirection = velocity.normalized;
        }

        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);

        animator.speed = isMoving && currentState == State.Hunt ? huntSpeed / Mathf.Max(wanderSpeed, 0.01f): 1f;
    }

    // --------------------------------------------------
    // VISION
    // --------------------------------------------------

    #region VISION

    void UpdateVision()
    {
        if (player == null || playerIsInSafeZone)
        {
            canSeePlayer = false;
            return;
        }

        PlayerController pc = player.GetComponent<PlayerController>();

        if (pc != null && pc.IsHidden)
        {
            canSeePlayer = false;
            return;
        }

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

        if (currentState == State.Confused)
        {
            float angle = Vector2.Angle(lastMoveDirection, direction);

            if (angle > 45f)
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

    #endregion

    // --------------------------------------------------
    // WANDER
    // --------------------------------------------------

    #region WANDER

    void WanderUpdate()
    {
        agent.speed = wanderSpeed;

        // Currently waiting at a destination.
        if (wanderWaitTimer > 0f)
        {
            agent.isStopped = true;
            wanderWaitTimer -= Time.deltaTime;

            if (wanderWaitTimer <= 0f)
            {
                Debug.Log("MINOTAUR: Wait finished. Choosing next destination.");
                ChooseWanderDestination();
            }

            return;
        }

        // Escaping the current area.
        if (wanderEscapingArea)
        {
            if (HasReachedDestination())
            {
                wanderEscapingArea = false;
                wanderAreaOrigin = transform.position;
                wanderAreaTimer = 0f;
                ChooseWanderDestination();
                return;
            }

            agent.isStopped = false;
            return;
        }

        wanderAreaTimer += Time.deltaTime;

        if (wanderAreaTimer >= wanderAreaDuration)
        {
            wanderEscapingArea = true;

            Vector3 escapeTarget = GetSafeWanderPoint(transform.position, wanderEscapeDistance);

            if (Vector3.Distance(escapeTarget, transform.position) < 0.5f || !SetSafeDestination(escapeTarget))
            {
                wanderEscapingArea = false;
                wanderAreaTimer = 0f;
                ChooseWanderDestination();
                return;
            }

            agent.isStopped = false;
            return;
        }

        // Check whether the current journey has finished.
        if (agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            Debug.Log("MINOTAUR: Destination reached. Starting wait.");

            agent.isStopped = true;

            wanderWaitTimer = Random.Range(
                Mathf.Max(0f, wanderWaitTime - wanderWaitVariation),
                wanderWaitTime + wanderWaitVariation
            );

            return;
        }

        agent.isStopped = false;
    }

    void ChooseWanderDestination()
    {
        Debug.Log("MINOTAUR: Attempting to choose wander destination.");

        agent.isStopped = false;

        Vector3 target = GetSafeWanderPoint(transform.position, wanderRadius);

        if (Vector3.Distance(target, transform.position) < 0.5f)
        {
            Debug.LogWarning("MINOTAUR: No suitable wander point found.");
            agent.ResetPath();
            agent.isStopped = true;
            wanderWaitTimer = 0.5f;
            return;
        }

        if (!SetSafeDestination(target))
        {
            Debug.LogWarning($"MINOTAUR: Failed to set destination: {target}");

            agent.ResetPath();
            agent.isStopped = true;
            wanderWaitTimer = 0.5f;
            return;
        }

        wanderWaitTimer = 0f;
        agent.isStopped = false;

        Debug.Log($"MINOTAUR: Destination successfully assigned: {agent.destination}");
    }

    #endregion

    // --------------------------------------------------
    // HUNT
    // --------------------------------------------------

    #region HUNT/Charge
 

    void HuntUpdate()
    {
        if (player == null)
            return;

        agent.speed = huntSpeed;

        PlayerController pc = player.GetComponent<PlayerController>();

        float distance = Vector2.Distance(transform.position, player.position);

        // Handle an attempted grab.
        if (isAttemptingGrab)
        {
            grabTimer -= Time.deltaTime;

            if (grabTimer <= 0f)
            {
                isAttemptingGrab = false;

                if (distance <= killRange)
                {
                    if (pc != null)
                        pc.PlayerDied();

                    if (sceneManager != null)
                        sceneManager.PlayerDiedScene();

                    animator.SetBool("playerDead", true);
                    isDead = true;

                    Debug.Log("PLAYER DIED");
                }
                else
                {
                    animator.SetBool("isGrabbing", false);
                    Debug.Log("PLAYER ESCAPED");

                    pc?.ClearGrabSlow();
                    agent.isStopped = false;
                }
            }

            return;
        }

        // Start a grab when close enough.
        if (distance <= grabRange)
        {
            // Cancel any active charge.
            chargePhase = ChargePhase.None;
            chargeTimer = 0f;

            agent.updatePosition = true;
            agent.Warp(transform.position);

            isAttemptingGrab = true;
            grabTimer = grabWindup;

            agent.ResetPath();
            agent.isStopped = true;

            FaceDirection(player.position - transform.position);

            pc?.ApplyGrabSlow();

            animator.SetBool("isGrabbing", true);

            Debug.Log("MINOTAUR STARTED GRAB");
            return;
        }

        // Handle an active charge.
        if (chargePhase != ChargePhase.None)
        {
            UpdateCharge();
            return;
        }

        // Begin charging when the player is within range.
        if (distance <= chargeStartRange && chargeCooldownTimer <= 0f)
        {
            chargePhase = ChargePhase.Windup;
            chargeTimer = chargeWindupTime;
            chargeCooldownTimer = chargeCooldownTime;

            agent.ResetPath();
            agent.isStopped = true;

            FaceDirection(player.position - transform.position);

            return;
        }

        // Normal pursuit when outside charge range.
        agent.isStopped = false;

        if (!agent.hasPath || Vector3.Distance(agent.destination, player.position) > 0.5f)
        {
            SetSafeDestination(player.position);
        }
    }

    void UpdateCharge()
    {
        switch (chargePhase)
        {
            case ChargePhase.Windup:

                agent.isStopped = true;

                // Keep facing the player during the wind-up.
                if (player != null)
                    FaceDirection(player.position - transform.position);

                chargeTimer -= Time.deltaTime;

                if (chargeTimer <= 0f)
                {
                    // Lock the direction when the charge begins.
                    if (player != null)
                    {
                        Vector2 direction = new Vector2(
                            player.position.x - transform.position.x,
                            player.position.y - transform.position.y);

                        chargeDirection = direction.normalized;
                    }
                    else
                    {
                        chargeDirection = lastMoveDirection;
                    }

                    agent.ResetPath();
                    agent.isStopped = false;
                    
                    agent.updatePosition = false;
                    chargePhase = ChargePhase.Charging;

                    Debug.Log("MINOTAUR: CHARGE!");
                }

                break;

            case ChargePhase.Charging:

                agent.isStopped = true;

                float moveDistance = chargeSpeed * Time.deltaTime;

                // Check for an obstacle directly ahead.
                RaycastHit2D hit = Physics2D.CircleCast(
                    transform.position,
                    chargeCollisionRadius,
                    chargeDirection,
                    moveDistance + 0.1f,
                    wallMask
                );

                if (hit.collider != null)
                {
                    // Move right up to the obstacle.
                    float safeDistance = Mathf.Max(0f, hit.distance - 0.02f);

                    transform.position += new Vector3(
                        chargeDirection.x * safeDistance,
                        chargeDirection.y * safeDistance,
                        0f
                    );

                    agent.nextPosition = transform.position;

                    StartChargeRecovery();
                    return;
                }

                // Move directly along the XY plane.
                transform.position += new Vector3(
                    chargeDirection.x * moveDistance,
                    chargeDirection.y * moveDistance,
                    0f
                );

                // Keep the NavMeshAgent synchronised with the Minotaur.
                agent.nextPosition = transform.position;

                break;

            case ChargePhase.Recovery:

                if (returningToNavMesh)
                    break;

                agent.isStopped = true;

                chargeTimer -= Time.deltaTime;

                if (chargeTimer <= 0f)
                {
                    Debug.Log("MINOTAUR: Charge recovery finished. Entering Search.");

                    agent.Warp(transform.position);
                    agent.updatePosition = true;

                    chargePhase = ChargePhase.None;

                    if (player != null)
                        lastKnownPlayerPos = player.position;

                    ChangeState(State.Confused);
                }

                break;
        }
    }

    void StartChargeRecovery()
    {
        chargePhase = ChargePhase.Recovery;
        chargeTimer = chargeRecoveryTime;

        agent.isStopped = true;
        agent.ResetPath();

        if (FindNavMeshReturnPosition())
        {
            returningToNavMesh = true;
        }
        else
        {
            Debug.LogError("MINOTAUR: Charge ended outside recoverable NavMesh range.");
        }
    }

    #endregion

    // --------------------------------------------------
    // SEARCH
    // --------------------------------------------------

    #region SEARCH

    void SearchUpdate()
    {
        agent.speed = searchSpeed;

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ChangeState(State.Wander);
            return;
        }

        if (searchPaused)
        {
            agent.isStopped = true;
            searchPauseTimer -= Time.deltaTime;

            if (searchPauseTimer <= 0f)
            {
                searchPaused = false;
                ChooseSearchDestination();
            }

            return;
        }

        if (HasReachedDestination())
        {
            agent.isStopped = true;
            FaceDirection(lastKnownPlayerPos - transform.position);

            searchPaused = true;
            searchPauseTimer = Random.Range(
                searchPauseMin,
                searchPauseMax
            );

            return;
        }

        agent.isStopped = false;
    }

    void ChooseSearchDestination()
    {
        NavMeshPath path = new NavMeshPath();

        for (int i = 0; i < 30; i++)
        {
            Vector2 offset = Random.insideUnitCircle * searchRadius;

            Vector3 candidate = lastKnownPlayerPos + new Vector3(
                offset.x,
                offset.y,
                0f
            );

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, agent.areaMask))
                continue;

            if (!agent.CalculatePath(hit.position, path))
                continue;

            if (path.status != NavMeshPathStatus.PathComplete)
                continue;

            if (!agent.SetDestination(hit.position))
                continue;

            agent.isStopped = false;
            return;
        }

        Debug.LogWarning("MINOTAUR: No reachable search destination. Returning to Wander.");

        ChangeState(State.Wander);
    }

    #endregion

    // --------------------------------------------------
    // INVESTIGATION
    // --------------------------------------------------

    #region INVESTIGATION

    void StartInvestigation()
    {
        agent.ResetPath();
        agent.isStopped = true;
        agent.speed = investigateSpeed;

        investigationPhase = InvestigationPhase.ListenAtNoise;
        investigationTimer = investigationListenTime;

        FaceDirection(investigationPos - transform.position);
    }

    void InvestigateUpdate()
    {
        agent.speed = investigateSpeed;

        switch (investigationPhase)
        {
            case InvestigationPhase.ListenAtNoise:
                InvestigationListen();
                break;

            case InvestigationPhase.MoveToNoise:
                InvestigationMoveToNoise();
                break;

            case InvestigationPhase.LookAround:
                InvestigationLookAround();
                break;

            case InvestigationPhase.MoveToNearby:
                InvestigationMoveToNearby();
                break;

            case InvestigationPhase.PauseAtNearby:
                InvestigationPauseAtNearby();
                break;

            case InvestigationPhase.FinalListen:
                InvestigationFinalListen();
                break;
        }
    }

    void InvestigationListen()
    {
        agent.isStopped = true;

        investigationTimer -= Time.deltaTime;

        if (investigationTimer <= 0f)
        {
            if (SetSafeDestination(investigationPos))
            {
                investigationPhase = InvestigationPhase.MoveToNoise;
                agent.isStopped = false;
            }
            else
            {
                Debug.LogWarning("MINOTAUR: Cannot reach noise location. Investigating nearby instead.");

                investigationPhase = InvestigationPhase.LookAround;
                investigationTurnsRemaining = Random.Range(minInvestigationTurns, maxInvestigationTurns + 1);
                investigationTimer = Random.Range(investigationPauseMin, investigationPauseMax);
                FaceRandomDirection();
            }
        }
    }

    void InvestigationMoveToNoise()
    {
        agent.isStopped = false;

        if (!HasReachedDestination())
            return;

        agent.isStopped = true;

        investigationTurnsRemaining = Random.Range(
            minInvestigationTurns,
            maxInvestigationTurns + 1
        );

        investigationPhase = InvestigationPhase.LookAround;
        investigationTimer = Random.Range(
            investigationPauseMin,
            investigationPauseMax
        );

        FaceRandomDirection();
    }

    void InvestigationLookAround()
    {
        agent.isStopped = true;

        investigationTimer -= Time.deltaTime;

        if (investigationTimer > 0f)
            return;

        investigationTurnsRemaining--;

        if (investigationTurnsRemaining > 0)
        {
            FaceRandomDirection();

            investigationTimer = Random.Range(
                investigationPauseMin,
                investigationPauseMax
            );

            return;
        }

        investigationLocationsRemaining = Random.Range(
            minInvestigationLocations,
            maxInvestigationLocations + 1
        );

        investigationPhase = InvestigationPhase.MoveToNearby;
        ChooseInvestigationLocation();
    }

    void InvestigationMoveToNearby()
    {
        agent.isStopped = false;

        if (!HasReachedDestination())
            return;

        agent.isStopped = true;

        investigationPhase = InvestigationPhase.PauseAtNearby;
        investigationTimer = Random.Range(
            investigationPauseMin,
            investigationPauseMax
        );

        FaceRandomDirection();
    }

    void InvestigationPauseAtNearby()
    {
        agent.isStopped = true;

        investigationTimer -= Time.deltaTime;

        if (investigationTimer > 0f)
            return;

        investigationLocationsRemaining--;

        if (investigationLocationsRemaining > 0)
        {
            investigationPhase = InvestigationPhase.MoveToNearby;
            ChooseInvestigationLocation();
        }
        else
        {
            investigationPhase = InvestigationPhase.FinalListen;
            investigationTimer = investigationListenTime;
            FaceDirection(investigationPos - transform.position);
        }
    }

    void InvestigationFinalListen()
    {
        agent.isStopped = true;

        investigationTimer -= Time.deltaTime;

        if (investigationTimer <= 0f)
        {
            lastKnownPlayerPos = investigationPos;
            ChangeState(State.Search);
        }
    }

    void ChooseInvestigationLocation()
    {
        for (int i = 0; i < 10; i++)
        {
            investigationTarget = GetSafeRandomPoint(transform.position, investigationNearbyRadius);

            if (Vector3.Distance(investigationTarget, transform.position) < 0.5f)
                continue;

            if (SetSafeDestination(investigationTarget))
            {
                agent.isStopped = false;
                return;
            }
        }

        Debug.LogWarning("MINOTAUR: Could not find another investigation location.");

        investigationPhase = InvestigationPhase.FinalListen;
        investigationTimer = investigationListenTime;
        agent.isStopped = true;
    }

    #endregion

    // --------------------------------------------------
    // Confused State
    // --------------------------------------------------

    #region Confused State

    void ConfusedUpdate()
    {
        agent.isStopped = true;

        confusedTimer -= Time.deltaTime;

        float currentAngle = Mathf.Atan2(
            lastMoveDirection.y,
            lastMoveDirection.x
        ) * Mathf.Rad2Deg;

        currentAngle += confusedTurnSpeed * Time.deltaTime;

        lastMoveDirection = new Vector2(
            Mathf.Cos(currentAngle * Mathf.Deg2Rad),
            Mathf.Sin(currentAngle * Mathf.Deg2Rad)
        ).normalized;

        if (confusedTimer <= 0f)
        {
            ChangeState(State.Search);
        }
    }
    #endregion

    // --------------------------------------------------
    // STATE TRANSITIONS
    // --------------------------------------------------

    #region STATE TRANSITIONS

    void StateTransitions()
    {

        if (ambushHuntActive)
        {
            PlayerController pc = player != null
                ? player.GetComponent<PlayerController>()
                : null;

            if (pc != null && pc.IsHidden)
            {
                ambushHuntActive = false;

                lastKnownPlayerPos = player.position;

                agent.isStopped = false;
                agent.ResetPath();

                pc.ClearGrabSlow();

                isAttemptingGrab = false;
                animator.SetBool("isGrabbing", false);

                ChangeState(State.Search);
                return;
            }

            if (canSeePlayer)
            {
                ambushHuntActive = false;
            }
            else
            {
                return;
            }
        }

        if (currentState == State.Hunt && chargePhase != ChargePhase.None)
            return;

        if (canSeePlayer)
        {
            if (currentState != State.Hunt)
                ChangeState(State.Hunt);

            return;
        }

        if (currentState == State.Hunt)
        {
            ChangeState(State.Search);

            if (player != null)
                player.GetComponent<PlayerController>()?.ClearGrabSlow();

            return;
        }
    }

    void ChangeState(State newState)
    {
        if (hasEnteredState && currentState == newState)
            return;

        hasEnteredState = true;
        currentState = newState;

        agent.isStopped = false;
        agent.ResetPath();

        switch (newState)
        {
            case State.Wander:
                wanderAreaOrigin = transform.position;
                wanderAreaTimer = 0f;
                wanderWaitTimer = 0f;
                wanderEscapingArea = false;
                agent.speed = wanderSpeed;
                ChooseWanderDestination();
                break;

            case State.Hunt:
                chargePhase = ChargePhase.None;
                agent.speed = huntSpeed;
                break;

            case State.Search:
                stateTimer = searchDuration;
                searchPaused = false;
                searchPauseTimer = 0f;
                agent.speed = searchSpeed;
                ChooseSearchDestination();
                break;

            case State.Investigate:
                StartInvestigation();
                break;

            case State.Confused:
                agent.speed = 0f;
                agent.isStopped = true;
                confusedTimer = confusedSearchDuration;
                break;
        }
    }

    #endregion

    // --------------------------------------------------
    // MOVEMENT HELPERS
    // --------------------------------------------------

    #region MOVEMENT HELPERS


    bool HasReachedDestination()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        if (agent.pathPending)
            return false;

        // No path does NOT mean we have reached our destination.
        if (!agent.hasPath)
            return false;

        // Only a complete path can count as a successful journey.
        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            return false;

        return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.2f);
    }

    bool SetSafeDestination(Vector3 target)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, agent.areaMask))
            return false;

        NavMeshPath path = new NavMeshPath();

        if (!agent.CalculatePath(hit.position, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        return agent.SetDestination(hit.position);
    }

    Vector3 GetSafeRandomPoint(Vector3 origin, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * radius;

            Vector3 randomPoint = new Vector3(
                origin.x + randomOffset.x,
                origin.y + randomOffset.y,
                origin.z
            );

            if (TryGetNavMeshPoint(randomPoint, out Vector3 hit))
                return hit;
        }

        return origin;
    }

    Vector3 GetSafeWanderPoint(Vector3 origin, float radius)
    {
        if (!agent.isOnNavMesh)
            return origin;

        NavMeshPath path = new NavMeshPath();

        for (int i = 0; i < 30; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;

            Vector3 candidate = new Vector3(
                origin.x + offset.x,
                origin.y + offset.y,
                origin.z
            );

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, agent.areaMask))
                continue;

            // Reject positions too close to the NavMesh edge.
            if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, agent.areaMask))
                continue;

            float edgeDistance = Vector3.Distance(hit.position, edgeHit.position);

            if (edgeDistance < wanderEdgeClearance)
                continue;

            // Only accept destinations with a complete path.
            if (!agent.CalculatePath(hit.position, path))
                continue;

            if (path.status != NavMeshPathStatus.PathComplete)
                continue;

            return hit.position;
        }

        // No suitable destination found.
        return origin;
    }

    bool TryGetNavMeshPoint(Vector3 source, out Vector3 result)
    {
        if (NavMesh.SamplePosition(
            source,
            out NavMeshHit hit,
            2f,
            NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = source;
        return false;
    }

    void ReturnToNavMesh()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            navMeshReturnTarget,
            navMeshReturnSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, navMeshReturnTarget) > 0.05f)
            return;

        transform.position = navMeshReturnTarget;

        if (agent.Warp(navMeshReturnTarget))
        {
            agent.nextPosition = navMeshReturnTarget;
            agent.updatePosition = true;
            returningToNavMesh = false;

            Debug.Log("MINOTAUR: Successfully reconnected to NavMesh.");
        }
    }

    bool FindNavMeshReturnPosition()
    {
        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit hit,
            navMeshReturnRadius,
            agent.areaMask))
        {
            navMeshReturnTarget = hit.position;
            return true;
        }

        Debug.LogWarning("MINOTAUR: Could not find nearby NavMesh position.");
        return false;
    }

    #endregion

    // --------------------------------------------------
    // DIRECTION AND ANIMATION
    // --------------------------------------------------

    #region DIRECTION AND ANIMATION

    void FaceRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        Vector2 direction = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );

        FaceDirection(direction);
    }

    void FaceDirection(Vector3 direction)
    {
        Vector2 dir = new Vector2(direction.x, direction.y);

        if (dir.sqrMagnitude < 0.001f)
            return;

        lastMoveDirection = dir.normalized;
    }

    #endregion

    
    // --------------------------------------------------
    // AMBUSH
    // --------------------------------------------------



    public bool TriggerAmbush(Transform targetPlayer,Vector3 spawnPosition)
    {
        if (targetPlayer == null)
        {
            Debug.LogError("[AMBUSH] No player was supplied.");
            return false;
        }

        if (agent == null || !agent.enabled)
        {
            Debug.LogError("[AMBUSH] NavMeshAgent is missing or disabled.");
            return false;
        }

        if (!TryGetNavMeshPoint(spawnPosition, out Vector3 safeSpawn))
        {
            Debug.LogError(
                $"[AMBUSH] No NavMesh found near spawn point {spawnPosition}."
            );
            return false;
        }

        PlayerController pc = targetPlayer.GetComponent<PlayerController>();

        isAttemptingGrab = false;
        grabTimer = 0f;

        animator.SetBool("isGrabbing", false);
        pc?.ClearGrabSlow();

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();

            if (!agent.Warp(safeSpawn))
            {
                agent.isStopped = false;
                Debug.LogError("[AMBUSH] NavMeshAgent.Warp failed.");
                return false;
            }
        }
        else
        {
            agent.enabled = false;
            transform.position = safeSpawn;
            agent.enabled = true;

            if (!agent.isOnNavMesh)
            {
                Debug.LogError("[AMBUSH] Agent could not attach to the NavMesh.");
                return false;
            }
        }

        player = targetPlayer;
        lastKnownPlayerPos = player.position;
        lastSeenTime = Time.time;
        investigationPos = player.position;

        ambushHuntActive = true;

        ChangeState(State.Hunt);

        agent.isStopped = false;
        SetSafeDestination(player.position);

        Debug.Log(
            $"[AMBUSH] SUCCESS! Minotaur spawned at {transform.position} " +
            $"and is hunting player at {player.position}."
        );

        return true;
    }

    // --------------------------------------------------
    // HIDING
    // --------------------------------------------------

    public void PlayerFinishedHiding()
    {
        if (currentState != State.Hunt || player == null)
            return;

        lastKnownPlayerPos = player.position;
        ambushHuntActive = false;

        agent.ResetPath();
        agent.isStopped = true;
        
        ChangeState(State.Search);
    }

    public bool CanPlayerNotHide()
    {
        if (currentState == State.Hunt && canSeePlayer)
        {
            Debug.Log("MINOTAUR Sees PLAYER hiding");
            return false;
        }

        return true;
    }

    // --------------------------------------------------
    // SAFE ZONES
    // --------------------------------------------------

    public void EnterSafeZone(SafeZone zone,PlayerController enteringPlayer)
    {
        if (zone == null || enteringPlayer == null)
            return;

        if (player != null && player != enteringPlayer.transform)
            return;

        player = enteringPlayer.transform;

        activeSafeZones.Add(zone);
        playerIsInSafeZone = activeSafeZones.Count > 0;

        if (!playerIsInSafeZone)
            return;

        isAttemptingGrab = false;
        grabTimer = 0f;

        animator.SetBool("isGrabbing", false);
        enteringPlayer.ClearGrabSlow();

        ambushHuntActive = false;

        agent.isStopped = true;
        agent.ResetPath();

        ChangeState(State.Wander);

        Debug.Log("MINOTAUR: Player entered a safe zone.");
    }

    public void ExitSafeZone(SafeZone zone,PlayerController exitingPlayer)
    {
        if (zone == null || exitingPlayer == null)
            return;

        if (player != exitingPlayer.transform)
            return;

        activeSafeZones.Remove(zone);
        playerIsInSafeZone = activeSafeZones.Count > 0;

        if (playerIsInSafeZone)
            return;

        agent.isStopped = false;
        agent.ResetPath();

        Debug.Log("MINOTAUR: Player left the safe zone.");
    }

   

    // --------------------------------------------------
    // NOISE
    // --------------------------------------------------

    public void TriggerNoise(Vector3 position)
    {
        if (playerIsInSafeZone || ambushHuntActive || isDead)
            return;

        if (canSeePlayer)
            return;

        investigationPos = position;

        // A new noise restarts an existing investigation.
        if (currentState == State.Investigate)
        {
            StartInvestigation();
            return;
        }

        ChangeState(State.Investigate);
    }

    // --------------------------------------------------
    // GIZMOS
    // --------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        // Grab range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRange);

        // Kill range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRange);

        // Wander radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // Investigation radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, investigationNearbyRadius);

        // Line to player when visible
        if (canSeePlayer && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Current NavMesh destination
        if (agent != null && agent.enabled && agent.hasPath)
        {
            Vector3 destination = agent.destination;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(destination, 0.5f);
            Gizmos.DrawLine(transform.position, destination);
        }
    }
}
