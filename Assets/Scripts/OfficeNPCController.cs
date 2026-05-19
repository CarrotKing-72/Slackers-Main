using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class OfficeNPCController : MonoBehaviour
{
    // ─── Detection / Alert ───────────────────────────────────────────────────
    [Header("Detection Settings")]
    public float alertRadius = 5f;
    public float maxSuspicion = 20f;
    public float currentSuspicion = 0f;

    // ─── Wander / Patrol ─────────────────────────────────────────────────────
    [Header("Wander Settings")]
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _wanderRadius = 8f;
    [SerializeField] private float _wanderWaitMin = 1f;
    [SerializeField] private float _wanderWaitMax = 3f;
    [Tooltip("Optional fixed patrol route. Leave empty for random wander.")]
    [SerializeField] private Transform[] _patrolPoints;

    // ─── Alert ───────────────────────────────────────────────────────────────
    [Header("Alert Settings")]
    public float idleResumeDelay = 5f;

    // ─── IK Leg Settings ─────────────────────────────────────────────────────
    [Header("IK Leg Settings")]
    [Tooltip("Multiplier fed to SmoothStep — increase to make legs step faster.")]
    public float legSpeedMultiplier = 2f;
    [Tooltip("Multiplier for vertical step amplitude.")]
    public float legVerticalityMultiplier = 1f;

    [Header("Root Height (IK Grounding)")]
    [Tooltip("Disable NavMeshAgent Y so IK controls vertical position.")]
    public bool disableAgentYControl = true;
    [Tooltip("Height of the character pivot above the ground.")]
    public float characterStandingHeight = 1f;
    public LayerMask groundLayer;
    public float heightSmoothSpeed = 15f;

    // ─── State ───────────────────────────────────────────────────────────────
    public enum NPCState { Wander, Alert }
    private NPCState _currentState = NPCState.Wander;
    public NPCState CurrentState => _currentState;

    [HideInInspector] public Transform manualTarget;

    // ─── Private ─────────────────────────────────────────────────────────────
    private NavMeshAgent _agent;
    private Coroutine _resumeWanderCoroutine;

    private bool _isWaiting;
    private float _wanderWaitTimer;
    private float _wanderWaitDuration;
    private int _currentPatrolIndex;

    private SmoothStep[] _legs;
    private Component[] _legGroundingComponents;
    private int _finalGroundMask;

    // ─── Init ─────────────────────────────────────────────────────────────────
    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;

        SetupLegs();
        SetupGroundMask();

        if (disableAgentYControl)
            _agent.updatePosition = false;

        Debug.Log($"[OfficeNPCController] {gameObject.name}: found {_legs.Length} legs. disableAgentYControl={disableAgentYControl}");

        SetNewWanderDestination();
    }

    // ─── Main Loop ────────────────────────────────────────────────────────────
    private void Update()
    {
        switch (_currentState)
        {
            case NPCState.Wander: UpdateWander(); break;
            case NPCState.Alert:  UpdateAlert();  break;
        }

        HandleRootHeight();
        KeepUpright();
        UpdateLegs();
    }

    // ─── Wander ───────────────────────────────────────────────────────────────
    private void UpdateWander()
    {
        if (_patrolPoints != null && _patrolPoints.Length > 0)
            UpdatePatrolPoints();
        else
            UpdateRandomWander();
    }

    private void UpdatePatrolPoints()
    {
        if (!_isWaiting && HasReachedDestination())
        {
            _isWaiting = true;
            _wanderWaitDuration = Random.Range(_wanderWaitMin, _wanderWaitMax);
            _wanderWaitTimer = 0f;
        }

        if (_isWaiting)
        {
            _wanderWaitTimer += Time.deltaTime;
            if (_wanderWaitTimer >= _wanderWaitDuration)
            {
                _isWaiting = false;
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
                _agent.SetDestination(_patrolPoints[_currentPatrolIndex].position);
            }
        }
    }

    private void UpdateRandomWander()
    {
        if (!_isWaiting && HasReachedDestination())
        {
            _isWaiting = true;
            _wanderWaitDuration = Random.Range(_wanderWaitMin, _wanderWaitMax);
            _wanderWaitTimer = 0f;
        }

        if (_isWaiting)
        {
            _wanderWaitTimer += Time.deltaTime;
            if (_wanderWaitTimer >= _wanderWaitDuration)
            {
                _isWaiting = false;
                SetNewWanderDestination();
            }
        }
    }

    private bool HasReachedDestination()
    {
        return _agent.hasPath && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;
    }

    private void SetNewWanderDestination()
    {
        Vector3 randomDir = Random.insideUnitSphere * _wanderRadius + transform.position;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, _wanderRadius, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    // ─── Alert ────────────────────────────────────────────────────────────────
    private void UpdateAlert()
    {
        if (manualTarget == null)
        {
            TransitionTo(NPCState.Wander);
            SetNewWanderDestination();
            return;
        }

        _agent.isStopped = false;
        _agent.SetDestination(manualTarget.position);
    }

    public void SetTarget(Vector3 position)
    {
        if (manualTarget == null)
        {
            GameObject temp = new GameObject("NPCAlertTarget");
            temp.transform.position = position;
            manualTarget = temp.transform;
        }
        else
        {
            manualTarget.position = position;
        }

        TransitionTo(NPCState.Alert);

        if (_resumeWanderCoroutine != null)
            StopCoroutine(_resumeWanderCoroutine);
        _resumeWanderCoroutine = StartCoroutine(ResumeWanderAfterDelay());
    }

    private IEnumerator ResumeWanderAfterDelay()
    {
        yield return new WaitForSeconds(idleResumeDelay);

        if (manualTarget != null)
        {
            Destroy(manualTarget.gameObject);
            manualTarget = null;
        }

        TransitionTo(NPCState.Wander);
        SetNewWanderDestination();
    }

    private void TransitionTo(NPCState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
    }

    // ─── Static Alert (called by minigame events) ─────────────────────────────
    public static void AlertNPCs(Vector3 playerPosition)
    {
        OfficeNPCController[] npcs = FindObjectsOfType<OfficeNPCController>(true);
        GameManager gm = FindFirstObjectByType<GameManager>();

        if (gm == null)
        {
            Debug.LogWarning("No GameManager found for score updates!");
            return;
        }

        float totalSuspicion = 0f;
        List<OfficeNPCController> alertedNPCs = new List<OfficeNPCController>();

        foreach (var npc in npcs)
        {
            float distance = Vector3.Distance(npc.transform.position, playerPosition);
            if (distance <= npc.alertRadius)
            {
                float suspicion = npc.maxSuspicion * (1f - (distance / npc.alertRadius));
                npc.currentSuspicion += suspicion;
                totalSuspicion += suspicion;
                npc.SetTarget(playerPosition);
                alertedNPCs.Add(npc);
            }
        }

        if (alertedNPCs.Count > 0)
        {
            float scorePerSuspicion = 0.5f;
            int rawDecrement = Mathf.RoundToInt(totalSuspicion * scorePerSuspicion / 5f) * 5;
            int decrement = Mathf.Max(rawDecrement, 5);
            gm.RemoveScore(decrement);
            Debug.Log($"Alerted {alertedNPCs.Count} NPCs. Total suspicion: {totalSuspicion:F1} → Score -{decrement}. Score: {gm.currentScore}");
        }
        else
        {
            Debug.Log("No NPCs close enough to detect the player.");
        }
    }

    // ─── IK ───────────────────────────────────────────────────────────────────
    private void SetupLegs()
    {
        _legs = GetComponentsInChildren<SmoothStep>();
        _legGroundingComponents = new Component[_legs.Length];

        for (int i = 0; i < _legs.Length; i++)
        {
            _legGroundingComponents[i] = _legs[i].GetComponent<LegGrounding>();
        }
    }

    private void SetupGroundMask()
    {
        _finalGroundMask = groundLayer.value != 0
            ? groundLayer.value & ~(1 << gameObject.layer)
            : ~0 & ~(1 << gameObject.layer);
    }

    private void HandleRootHeight()
    {
        if (!disableAgentYControl)
        {
            transform.position = _agent.nextPosition;
            return;
        }

        // Use NavMesh surface as the authoritative ground Y — _agent.nextPosition.y can't be
        // trusted here because we sync it back from transform.position every frame, so it
        // inherits any upward displacement the physics engine applied.
        NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 10f, NavMesh.AllAreas);
        float navGroundY = navHit.position.y + characterStandingHeight;

        // Physics raycast refines Y for uneven geometry, but only if it agrees with the NavMesh
        float castOriginHeight = characterStandingHeight + 2f;
        float targetY = navGroundY;
        if (Physics.Raycast(transform.position + Vector3.up * castOriginHeight,
                            Vector3.down, out RaycastHit hit, castOriginHeight + 5f, _finalGroundMask))
        {
            float rayY = hit.point.y + characterStandingHeight;
            if (Mathf.Abs(rayY - navGroundY) < 0.5f)
                targetY = rayY;
        }

        // Snap immediately when significantly above ground, smooth otherwise
        float newY = Mathf.Abs(transform.position.y - targetY) > 0.8f
            ? targetY
            : Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * heightSmoothSpeed);

        transform.position = new Vector3(
            _agent.nextPosition.x,
            newY,
            _agent.nextPosition.z);

        _agent.nextPosition = transform.position;
    }

    private void KeepUpright()
    {
        Quaternion r = transform.rotation;
        transform.rotation = Quaternion.Euler(0f, r.eulerAngles.y, 0f);
    }

    private void UpdateLegs()
    {
        if (_legs == null || _legs.Length == 0) return;

        float speed = _agent.velocity.magnitude;
        Vector3 dir = _agent.velocity.normalized;
        float legSpeed = speed * legSpeedMultiplier;

        for (int i = 0; i < _legs.Length; i++)
        {
            SmoothStep leg = _legs[i];
            var grounding = _legGroundingComponents[i];

            if (grounding != null)
                ((Behaviour)grounding).enabled = !leg.legIsMoving;

            leg.MovementDirection = dir;
            leg.MovementSpeed = legSpeed * legVerticalityMultiplier;
            leg.CharacterRotation = transform.rotation;
        }
    }

    // ─── Gizmos ───────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _wanderRadius);
    }
}
