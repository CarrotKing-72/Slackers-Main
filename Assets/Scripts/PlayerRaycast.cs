using UnityEngine;
using UnityEngine.UI;

public class PlayerRaycast : MonoBehaviour
{
    [Header("Raycasting References")]
    [SerializeField] Transform raycastOrigin;
    [SerializeField] float raycastDistance = 10f;
    private RaycastHit hit;

    [Header("Minigame References")]
    public GameObject computerMinigame1;
    public GameObject computerMinigame2;
    // public GameObject computerMinigame3;
    public bool computerMinigameIsActive = false;
    public GameObject sinkMinigame;
    public GameObject windowMinigame;

    [Header("UI References")]
    public GameObject interactPromptObject;
    public Text interactPromptText;
    public GameObject pickupPromptObject;
    public Text pickupPromptText;

    [Header("Player References")]
    public PlayerController playerController;

    private GameObject currentlyLookingAt;
    private bool interactedThisPress = false;

    private void Update()
    {
        // Draw the ray in the Scene view for debugging
        Debug.DrawRay(raycastOrigin.position, raycastOrigin.forward * raycastDistance, Color.red);

        // Reset UI prompts
        interactPromptObject.SetActive(false);
        pickupPromptObject.SetActive(false);
        currentlyLookingAt = null;

        // Reset key press trigger
        if (!Input.GetKey(playerController.interactKey))
            interactedThisPress = false;

        if (computerMinigame1.activeInHierarchy == true || computerMinigame2.activeInHierarchy == true)
        {
            playerController.allowMovement = false;
        }
        else
        {
            playerController.allowMovement = true;
        }

        // Cast the ray
        int layerMask = ~LayerMask.GetMask("IgnoreRaycastInteract"); // ignore this layer if needed
        if (Physics.Raycast(raycastOrigin.position, raycastOrigin.forward, out hit, raycastDistance, layerMask))
        {
            currentlyLookingAt = hit.collider.gameObject;

            // Handle pickups
            if (hit.collider.CompareTag("Pickup"))
            {
                pickupPromptText.text = "Press E to pick up " + currentlyLookingAt.name;
                pickupPromptObject.SetActive(true);

                if (Input.GetKey(playerController.interactKey) && !interactedThisPress)
                {
                    interactedThisPress = true;

                    if (currentlyLookingAt.name == "Stapler")
                    {
                        playerController.staplerObtained = true;
                        Debug.Log("Stapler Obtained!");
                    }

                    if (currentlyLookingAt.name == "FoodWaste")
                    {
                        playerController.foodWasteObtained = true;
                        Debug.Log("Food Waste Obtained!");
                    }

                    Destroy(currentlyLookingAt);
                }

                return; // prevent interacting with other objects this frame
            }

            // Handle interactive objects
            if (hit.collider.CompareTag("Sab"))
            {
                interactPromptObject.SetActive(true);
                interactPromptText.text = "Press E to interact";

                if (Input.GetKey(playerController.interactKey) && !interactedThisPress)
                {
                    interactedThisPress = true;

                    switch (currentlyLookingAt.name)
                    {
                        case "Window":
                            if (playerController.staplerObtained)
                            {
                                currentlyLookingAt.GetComponent<SmashWindow>().SmashWindowStart();
                                Debug.Log("Window smashed!");
                            }
                            break;

                        case "Computer":
                            OpenComputerMinigame();
                            break;

                        case "Sink":
                            sinkMinigame.SetActive(true);
                            interactPromptObject.SetActive(false);
                            break;
                    }
                }
            }
        }
    }

    private void OpenComputerMinigame()
    {
        computerMinigameIsActive = true;

        // Disable all minigames first
        computerMinigame1.SetActive(false);
        computerMinigame2.SetActive(false);
        // computerMinigame3?.SetActive(false);

        // Pick one randomly
        int random = Random.Range(1, 3); // 1 or 2
        if (random == 1) computerMinigame1.SetActive(true);
        else computerMinigame2.SetActive(true);

        interactPromptObject.SetActive(false);
    }
}