
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

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
    public SceneLoader sceneManager;

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

    [Header("Grab")]
    public float grabRange = 1.2f;
    public float killRange = 1.5f;
    public float grabWindup = 1f;

    private State currentState;
    private Vector3 lastKnownPlayerPos;
    private Vector3 investigationPos;
    private float lastSeenTime;
    private float stateTimer;
    private bool canSeePlayer;

    private bool isAttemptingGrab;
    private float grabTimer;

    // Keeps the ambush hunt active until the Minotaur sees
    // the player, or the player hides.
    private bool ambushHuntActive;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector2 lastMoveDirection = Vector2.down;

    private readonly System.Collections.Generic.HashSet<SafeZone>
    activeSafeZones = new();

    private bool playerIsInSafeZone;

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

        if (playerIsInSafeZone)
        {
            // The player is safe, so the Minotaur abandons the hunt.
            if (currentState != State.Wander)
                ChangeState(State.Wander);

            // Continue wandering outside the protected area.
            agent.isStopped = false;

            WanderUpdate();
            UpdateAnimation();

            // Do not run vision-based state transitions while the
            // player remains inside a safe zone.
            return;
        }

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

        if (player == null)
        {
            canSeePlayer = false;
            return;
        }

        if (playerIsInSafeZone)
        {
            canSeePlayer = false;
            return;
        }

        PlayerController playerController =
            player.GetComponent<PlayerController>();

        if (playerController != null && playerController.IsHidden)
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
            Vector3 target = GetSafeRandomPoint(
                transform.position,
                wanderRadius
            );

            SetSafeDestination(target);
        }
    }

    void HuntUpdate()
    {
        if (player == null)
            return;

        PlayerController pc = player.GetComponent<PlayerController>();

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        // Handle an attempted grab.
        if (isAttemptingGrab)
        {
            grabTimer -= Time.deltaTime;

            if (grabTimer <= 0f)
            {
                isAttemptingGrab = false;

                if (distance <= killRange)
                {
                    pc.PlayerDied();

                    sceneManager.PlayerDiedScene();
                    animator.SetBool("playerDead", true);

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
            isAttemptingGrab = true;
            grabTimer = grabWindup;

            agent.ResetPath();
            agent.isStopped = true;

            pc?.ApplyGrabSlow();

            animator.SetBool("isGrabbing", true);

            Debug.Log("MINOTAUR STARTED GRAB");
            return;
        }

        agent.isStopped = false;

        // During an ambush, pursue the player's current position
        // even if the player is temporarily outside normal vision.
        SetSafeDestination(player.position);
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

            if (Mathf.Abs(dir.x) < 0.1f)
                dir.x = 0;

            if (Mathf.Abs(dir.y) < 0.1f)
                dir.y = 0;

            if (dir != Vector2.zero)
                lastMoveDirection = dir;
        }

        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);
    }

    void SearchUpdate()
    {
        stateTimer -= Time.deltaTime;

        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            if (stateTimer <= 0f)
            {
                ChangeState(State.Wander);
                return;
            }

            Vector3 target = GetSafeRandomPoint(
                lastKnownPlayerPos,
                searchRadius
            );

            SetSafeDestination(target);
        }
    }

    void InvestigateUpdate()
    {
        SetSafeDestination(investigationPos);

        if (!agent.pathPending &&
            agent.remainingDistance < 0.5f)
        {
            ChangeState(State.Search);
        }
    }

    void StateTransitions()
    {
        // Special handling for an ambush.
        if (ambushHuntActive)
        {
            PlayerController pc = player != null
                ? player.GetComponent<PlayerController>()
                : null;

            // If the player hides, stop the forced hunt and search.
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

            // Once normal vision sees the player, return to
            // the normal Hunt and vision transition rules.
            if (canSeePlayer)
            {
                ambushHuntActive = false;
            }
            else
            {
                // Stay in Hunt until the player is seen or hides.
                return;
            }
        }

        if (canSeePlayer)
        {
            ChangeState(State.Hunt);
            return;
        }

        if (!canSeePlayer && currentState == State.Hunt)
        {
            ChangeState(State.Search);
            stateTimer = searchDuration;

            if (player != null)
                player.GetComponent<PlayerController>()?.ClearGrabSlow();

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
            stateTimer = searchDuration;
    }

    void SetSafeDestination(Vector3 target)
    {
        if (TryGetNavMeshPoint(target, out Vector3 safe))
            agent.SetDestination(safe);
    }

    Vector3 GetSafeRandomPoint(Vector3 origin, float radius)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPoint =
                origin + Random.insideUnitSphere * radius;

            if (TryGetNavMeshPoint(randomPoint, out Vector3 hit))
                return hit;
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
            NavMesh.AllAreas
        );

        if (success)
        {
            result = hit.position;
            return true;
        }

        result = source;
        return false;
    }

    // Called by an EnemyChase trigger.
    public bool TriggerAmbush(
    Transform targetPlayer,
    Vector3 spawnPosition)
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
                $"[AMBUSH] No NavMesh found near spawn point {spawnPosition}. " +
                "Place the spawn point on the baked NavMesh."
            );
            return false;
        }

        PlayerController pc = targetPlayer.GetComponent<PlayerController>();

        // Cancel an existing grab.
        isAttemptingGrab = false;
        grabTimer = 0f;

        animator.SetBool("isGrabbing", false);
        pc?.ClearGrabSlow();

        // Stop existing movement.
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Warp works when the agent is already attached to a NavMesh.
        // If it isn't, re-enable it at the valid sampled position.
        if (agent.isOnNavMesh)
        {
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
                Debug.LogError(
                    "[AMBUSH] Agent could not attach to the NavMesh at the spawn point."
                );
                return false;
            }
        }

        // Start the chase.
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

    public void PlayerFinishedHiding()
    {
        // Ignore if we weren't chasing the player.
        if (currentState != State.Hunt)
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
    public void EnterSafeZone(SafeZone zone, PlayerController enteringPlayer)
    {
        if (zone == null || enteringPlayer == null)
            return;

        // Only protect the player this Minotaur is assigned to.
        if (player != null && player != enteringPlayer.transform)
            return;

        player = enteringPlayer.transform;

        activeSafeZones.Add(zone);
        playerIsInSafeZone = activeSafeZones.Count > 0;

        if (!playerIsInSafeZone)
            return;

        // Cancel a grab and release the player's movement slow.
        isAttemptingGrab = false;
        grabTimer = 0f;

        animator.SetBool("isGrabbing", false);
        enteringPlayer.ClearGrabSlow();

        // Stop the current chase and forget the forced ambush.
        ambushHuntActive = false;

        agent.isStopped = true;
        agent.ResetPath();

        // The Minotaur loses interest.
        ChangeState(State.Wander);

        Debug.Log("MINOTAUR: Player entered a safe zone.");
    }

    public void ExitSafeZone(SafeZone zone, PlayerController exitingPlayer)
    {
        if (zone == null || exitingPlayer == null)
            return;

        if (player != exitingPlayer.transform)
            return;

        activeSafeZones.Remove(zone);
        playerIsInSafeZone = activeSafeZones.Count > 0;

        // If the player is still inside another safe zone,
        // keep them protected.
        if (playerIsInSafeZone)
            return;

        agent.isStopped = false;
        agent.ResetPath();

        // Do not immediately restart the hunt.
        // Normal vision can detect the player again.
        Debug.Log("MINOTAUR: Player left the safe zone.");
    }

    public void TriggerNoise(Vector3 position)
    {
        investigationPos = position;
        ChangeState(State.Investigate);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRange);
    }
}
