using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoneConstraintController : MonoBehaviour
{
    [Header("Leg Movement Settings")]
    public GameObject legAimPosition;
    public float maxStepDistance = 0.1f;
    public float oppositeLegBuffer = 0.15f;

    [Header("References")]
    public BoneConstraintController oppositeLeg;

    // Private variables
    private Vector3 currentIKPosition;
    private Vector3 currentLead = Vector3.zero; // Smoothed forward lead
    private float currentStepDistance;
    private float lastStepTime;
    private bool legIsMoving;

    // Dynamically calculated parameters
    private float stepCooldown = 0.3f;
    private float stepSpeed = 18f;

    // Cached reference to the player controller
    private PlayerController playerController;

    void Start()
    {
        currentIKPosition = transform.position;
        lastStepTime = Time.time;

        // Find the player controller in the hierarchy
        playerController = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        if (playerController == null) return; // Early return if no player controller

        HandleLegMovement();
    }

    private void HandleLegMovement()
    {
        Vector3 targetPos = CalculateTargetPosition();

        // Check if the leg needs to step
        currentStepDistance = Vector3.Distance(currentIKPosition, targetPos);
        bool canStep = currentStepDistance > maxStepDistance &&
                       (oppositeLeg == null || !oppositeLeg.CheckIsMoving()) &&
                       (Time.time - lastStepTime >= stepCooldown);

        if (canStep && !legIsMoving)
        {
            StartStep(targetPos);
        }

        if (legIsMoving)
        {
            PerformStep(targetPos);
        }
        else
        {
            // Keep the leg at its current position
            transform.position = currentIKPosition;
        }
    }

    private Vector3 CalculateTargetPosition()
    {
        // Calculate the desired lead based on movement input
        Vector3 moveInput = playerController.GetComponent<PlayerInput>().actions["Move"].ReadValue<Vector2>();
        float moveAmount = moveInput.magnitude;

        Vector3 desiredLead = playerController.transform.forward * Mathf.Lerp(0f, 0.8f, moveAmount);
        currentLead = Vector3.Lerp(currentLead, desiredLead, Time.deltaTime * 4f);

        // Calculate the target position for the leg
        return legAimPosition.transform.position + currentLead;
    }

    private void StartStep(Vector3 targetPos)
    {
        legIsMoving = true;
        lastStepTime = Time.time;
    }

    private void PerformStep(Vector3 targetPos)
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, stepSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            transform.position = targetPos;
            currentIKPosition = transform.position;
            legIsMoving = false;
        }
    }

    public bool CheckIsMoving()
    {
        return legIsMoving;
    }
}
