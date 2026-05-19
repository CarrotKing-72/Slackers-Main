using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    [Header("Leg Speed Adjustment")]
    [Tooltip("Multiplier to increase the speed fed to the SmoothStep scripts, making legs take faster/longer steps.")]
    public float legSpeedMultiplier = 2f;

    [Header("IK Tweak - Step Verticality")]
    [Tooltip("Artificial multiplier to increase the vertical separation between legs (step height/amplitude).")]
    public float legVerticalityMultiplier = 1.0f;

    [Header("NavMesh Agent Configuration")]
    [Tooltip("If true, the NavMeshAgent only handles horizontal movement (X/Z). Recommended when using IK for vertical grounding.")]
    public bool disableAgentYControl = true;

    [Header("Root Height Enforcement")]
    [Tooltip("Height of the character pivot above the ground when standing.")]
    public float characterStandingHeight = 1.0f;

    [Tooltip("The layer(s) the ground is on.")]
    public LayerMask groundLayer;

    [Tooltip("How quickly the character adjusts its height to the ground.")]
    public float heightSmoothSpeed = 15f;

    private NavMeshAgent agent;
    private SmoothStep[] legs;
    private Component[] legGroundingComponents;
    private float actualSpeed;
    private int finalGroundMask;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NPCController requires a NavMeshAgent component.");
            return;
        }

        // --- LEG SETUP ---
        legs = GetComponentsInChildren<SmoothStep>();
        legGroundingComponents = new Component[legs.Length];

        for (int i = 0; i < legs.Length; i++)
        {
            legGroundingComponents[i] = legs[i].GetComponent<LegGrounding>();
            if (legGroundingComponents[i] == null)
            {
                Debug.LogWarning($"Leg {i} missing LegGrounding component.");
            }
        }

        // --- GROUND LAYER FIX ---
        finalGroundMask = groundLayer.value;
        if (finalGroundMask != 0)
        {
            finalGroundMask &= ~(1 << gameObject.layer);
        }
        else
        {
            finalGroundMask = ~0;
            finalGroundMask &= ~(1 << gameObject.layer);
        }

        // Let OfficeNPCController still control movement logic
        // but optionally freeze Y control (safer for IK characters)
        if (disableAgentYControl)
            agent.updatePosition = false;
    }

    void Update()
    {
        if (agent == null) return;

        // --- ROOT HEIGHT ENFORCEMENT ---
        HandleRootHeight();

        // --- ROTATION LOCK (stay upright) ---
        Quaternion currentRotation = transform.rotation;
        transform.rotation = Quaternion.Euler(0f, currentRotation.eulerAngles.y, 0f);

        // --- LEG IK UPDATE ---
        UpdateLegs();
    }

    private void HandleRootHeight()
    {
        // OfficeNPCController handles horizontal movement
        // We only adjust the vertical position if enabled
        if (disableAgentYControl)
        {
            float targetY = transform.position.y;

            // Raycast downwards to find ground
            if (Physics.Raycast(transform.position + Vector3.up * characterStandingHeight,
                                Vector3.down,
                                out RaycastHit hit,
                                10f,
                                finalGroundMask))
            {
                targetY = hit.point.y + characterStandingHeight;
            }

            Vector3 finalPos = new Vector3(agent.nextPosition.x,
                                           Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * heightSmoothSpeed),
                                           agent.nextPosition.z);

            transform.position = finalPos;
            agent.nextPosition = transform.position;
        }
        else
        {
            transform.position = agent.nextPosition;
        }
    }

    private void UpdateLegs()
    {
        actualSpeed = agent.velocity.magnitude;
        Vector3 movementDirection = agent.velocity.normalized;
        float movementSpeedForLegs = actualSpeed * legSpeedMultiplier;

        for (int i = 0; i < legs.Length; i++)
        {
            SmoothStep leg = legs[i];
            var grounding = legGroundingComponents[i];

            // Disable grounding while stepping
            if (grounding != null)
            {
                bool isMoving = leg.legIsMoving;
                ((Behaviour)grounding).enabled = !isMoving;
            }

            // Provide movement data for IK
            leg.MovementDirection = movementDirection;
            leg.MovementSpeed = movementSpeedForLegs * legVerticalityMultiplier;
            leg.CharacterRotation = transform.rotation;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 2f);
    }
}
