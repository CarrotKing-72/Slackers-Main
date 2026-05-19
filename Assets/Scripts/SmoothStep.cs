using UnityEngine;
using UnityEngine.InputSystem;

public class SmoothStep : MonoBehaviour
{
    [Header("Leg Movement Settings")]
    public GameObject legAimPosition;
    public AnimationCurve stepCurve; // Arc of the step
    public float stepHeight = 0.2f; // Maximum height of the step arc
    public float oppositeLegBuffer = 0.15f; // Buffer time for opposite leg

    [Header("References")]
    public SmoothStep oppositeLeg;

    // Public properties for external movement data
    public Vector3 MovementDirection { get; set; } = Vector3.zero; // Movement direction (set externally)
    public float MovementSpeed { get; set; } = 0f; // Movement speed (set externally)
    public Quaternion CharacterRotation { get; set; } = Quaternion.identity; // Character rotation (set externally)
    public bool legIsMoving;
    // Private variables
    private Vector3 currentIKPosition;
    private Vector3 currentLead = Vector3.zero; // Smoothed forward lead
    private Vector3 startPos; // Starting position of the step
    private Vector3 endPos; // Target position of the step
    private float stepProgress = 0f; // Normalized time for the step arc
    private float lastStepTime;
    

    // Dynamically calculated parameters
    private float maxStepDistance = 0.4f; // Maximum distance before a step is triggered
    private float stepCooldown = 0.5f; // Minimum time between steps

    void Start()
    {
        currentIKPosition = transform.position;
        lastStepTime = Time.time;
    }

    void Update()
    {
        UpdateStepParameters();
        HandleLegMovement();
    }

    private void UpdateStepParameters()
    {
        // Dynamically calculate step parameters based on movement speed
        float moveSpeed = Mathf.Max(MovementSpeed, 0.1f); // Avoid division by zero
        maxStepDistance = Mathf.Lerp(0.1f, 0.4f, moveSpeed / 10f);

        // Increased step cooldown for slower steps
        stepCooldown = Mathf.Lerp(0.15f, 0.4f, 1f / moveSpeed);
    }

    private void HandleLegMovement()
    {
        Vector3 targetPos = CalculateTargetPosition();

        // Check if the leg needs to step
        float currentStepDistance = Vector3.Distance(currentIKPosition, targetPos);
        bool canStep = currentStepDistance > maxStepDistance &&
                       (oppositeLeg == null || !oppositeLeg.CheckIsMoving()) &&
                       (Time.time - lastStepTime >= stepCooldown);

        if (canStep && !legIsMoving)
        {
            StartStep(targetPos);
        }

        if (legIsMoving)
        {
            PerformStep();
        }
        else
        {
            // Keep the leg at its current position
            transform.position = currentIKPosition;
        }
    }

    private Vector3 CalculateTargetPosition()
    {
        
        Quaternion uprightRotation = Quaternion.Euler(0, CharacterRotation.eulerAngles.y, 0);

        

        //  Define the lead distance based on speed
        float leadDistance = Mathf.Lerp(0.4f, 1f, MovementSpeed / 10f);

        //  Calculate the local forward direction of the character
        Vector3 characterForward = uprightRotation * Vector3.forward;

        //  Calculate the world target position: Leg Origin + Character's Forward Lead
        Vector3 targetPos = legAimPosition.transform.position + (characterForward * leadDistance);

        return targetPos;
    }

    private void StartStep(Vector3 targetPos)
    {
        legIsMoving = true;
        lastStepTime = Time.time;
        stepProgress = 0f;
        startPos = transform.position;
        endPos = targetPos;
    }

    private void PerformStep()
    {
        stepProgress += Time.deltaTime / stepCooldown;
        stepProgress = Mathf.Clamp01(stepProgress);

        // Interpolate position and add arc height
        Vector3 flat = Vector3.Lerp(startPos, endPos, stepProgress);
        float arcHeight = stepCurve.Evaluate(stepProgress) * stepHeight;
        transform.position = flat + Vector3.up * arcHeight;

        // End the step when progress is complete
        if (stepProgress >= 1f)
        {
            transform.position = endPos;
            currentIKPosition = transform.position;
            legIsMoving = false;
        }
    }

    public bool CheckIsMoving()
    {
        return legIsMoving;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawSphere(endPos, 0.05f);
    }
}
