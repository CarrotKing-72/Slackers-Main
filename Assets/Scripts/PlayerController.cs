using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSmoothTime = 0.1f;
    public bool allowMovement = true;

    [Header("Interact Settings")]
    public bool interactKeyPressed = false;
    public bool staplerObtained = false;
    public bool foodWasteObtained = false;
    public int windowsSmashed = 0;
    public int staplerUses = 4;

    [Header("Key Bindings")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode moveForward = KeyCode.W;
    public KeyCode moveBackward = KeyCode.S;
    public KeyCode moveLeft = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;
    public KeyCode exitMenu = KeyCode.Escape;

    // Private variables
    private Vector3 moveDirection;
    private float currentRotationVelocity;
    private float targetAngle;

    private Rigidbody rb;
    private SmoothStep[] legs;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        legs = GetComponentsInChildren<SmoothStep>();
    }

    private void Update()
    {
        // Read input
        ReadMovementInput();

        // Interact input
        interactKeyPressed = Input.GetKey(interactKey);

        // Stapler logic
        if (staplerObtained && staplerUses <= 0)
            staplerObtained = false;

        // Update leg animation
        UpdateLegs();
    }

    private void FixedUpdate()
    {
        // Denies Movement In Certain Conditions
        if (allowMovement)
        {
            // Move player in FixedUpdate for proper physics
            MovePlayer();
        }
    }

    private void ReadMovementInput()
    {
        Vector3 input = Vector3.zero;

        if (Input.GetKey(moveForward)) input.z += 1;
        if (Input.GetKey(moveBackward)) input.z -= 1;
        if (Input.GetKey(moveRight)) input.x += 1;
        if (Input.GetKey(moveLeft)) input.x -= 1;

        input = input.normalized;

        if (input.magnitude > 0f)
        {
            targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg;
            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        else
        {
            moveDirection = Vector3.zero;
        }
    }

    private void MovePlayer()
    {
        if (moveDirection.magnitude > 0f)
        {
            Vector3 movement = moveDirection * moveSpeed * Time.fixedDeltaTime;

            // Rigidbody movement ensures collisions
            rb.MovePosition(rb.position + movement);

            // Smooth rotation
            float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentRotationVelocity, rotationSmoothTime);
            rb.MoveRotation(Quaternion.Euler(0f, smoothedAngle, 0f));
        }
    }

    private void UpdateLegs()
    {
        float movementSpeed = moveDirection.magnitude * moveSpeed;

        foreach (var leg in legs)
        {
            leg.MovementDirection = moveDirection.normalized;
            leg.MovementSpeed = movementSpeed;
            leg.CharacterRotation = transform.rotation;
        }
    }
}