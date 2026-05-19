using UnityEngine;

public class TorsoSway : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;     // usually the AlienBody or main player controller object

    [Header("Sway Settings")]
    public float rotationAmount = 5f;     // how much the torso tilts side-to-side
    public float rotationSpeed = 6f;      // how fast it reacts to movement
    public float bobAmount = 0.03f;       // how much the torso moves up/down
    public float bobSpeed = 10f;          // speed of the bobbing motion

    private Vector3 defaultPos;
    private Quaternion defaultRot;
    private Vector3 lastPos;
    private float moveSpeed;

    void Start()
    {
        defaultPos = transform.localPosition;
        defaultRot = transform.localRotation;
        lastPos = playerRoot.position;
    }

    void Update()
    {
        // --- Calculate movement speed ---
        Vector3 velocity = (playerRoot.position - lastPos) / Time.deltaTime;
        velocity.y = 0;
        moveSpeed = velocity.magnitude;

        // --- Bobbing effect ---
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount * Mathf.Clamp01(moveSpeed);

        // --- Side sway based on movement direction ---
        float swayOffset = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount * Mathf.Clamp01(moveSpeed);

        // --- Apply transforms smoothly ---
        Quaternion targetRot = defaultRot * Quaternion.Euler(0, 0, -swayOffset);
        Vector3 targetPos = defaultPos + new Vector3(0, bobOffset, 0);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * 8f);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRot, Time.deltaTime * 8f);

        // --- Store last position for next frame ---
        lastPos = playerRoot.position;
    }
}
