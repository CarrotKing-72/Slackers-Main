using UnityEngine;

public class SmashWindow : MonoBehaviour
{
    [SerializeField] GameObject windowGlass;
    public GameManager manager;

    public void SmashWindowStart()
    {
        manager.AddScore(20);
        manager.AddTime(10);

        Vector3 playerPos = FindFirstObjectByType<PlayerController>().transform.position;
        OfficeNPCController.AlertNPCs(playerPos);

        windowGlass.SetActive(false);
    }
}