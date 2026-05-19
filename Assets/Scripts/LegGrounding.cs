

using UnityEngine;

public class LegGrounding : MonoBehaviour
{
    [Header("Grounding Settings")]
    public Vector3 groundingOffset = new Vector3(0f, 0.75f, 0f);
    public GameObject raycastOrigin;

    // Private variables
    private int layerMask;

    void Start()
    {
        layerMask = LayerMask.GetMask("Ground");

        // Default to the parent object if raycastOrigin is not set
        if (raycastOrigin == null && transform.parent != null)
        {
            raycastOrigin = transform.parent.gameObject;
        }
    }

    void Update()
    {
        PerformGrounding();
    }

    private void PerformGrounding()
    {
        Vector3 origin = raycastOrigin != null ? raycastOrigin.transform.position : transform.position;

        if (Physics.Raycast(origin + groundingOffset, -transform.up, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            transform.position = hit.point + groundingOffset;
        }
    }
}
