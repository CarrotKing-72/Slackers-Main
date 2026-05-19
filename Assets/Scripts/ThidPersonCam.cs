using UnityEngine;

public class ThidPersonCam : MonoBehaviour
{
    public Transform player; // Reference to the player's Transform
    public Vector3 offset; // Offset of the camera relative to the player

    private void Update()
    {
        gameObject.transform.position = player.position + offset;
    }
}
