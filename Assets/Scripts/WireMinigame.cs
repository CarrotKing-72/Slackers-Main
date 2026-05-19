using UnityEngine;

public class WireMinigame : MonoBehaviour
{
    public WireConnector[] wireConnectors;
    public GameManager manager;
    public PlayerController playerController;

    int connectedCount = 0;
    bool alertSent = false;

    [Header("Panel Reference")]
    public GameObject panel;

    private void Update()
    {
        ExitPanel();
    }

    public void WireConnected(WireConnector connector)
    {
        if (connector == null || connector.isConnected) return;

        connector.isConnected = true;
        connectedCount++;

        if (connectedCount == wireConnectors.Length && !alertSent)
        {
            Debug.Log("Wire minigame completed! All wires connected.");

            Vector3 playerPos = FindFirstObjectByType<PlayerController>().transform.position;
            OfficeNPCController.AlertNPCs(playerPos);

            alertSent = true;

            manager?.AddScore(50);
            manager?.AddTime(10);
        }
    }

    public void WireDisconnected(WireConnector connector)
    {
        if (connector != null && connector.isConnected)
        {
            connector.isConnected = false;
            connectedCount--;
        }
    }

    public void ResetWires()
    {
        connectedCount = 0;
        alertSent = false;

        foreach (var connector in wireConnectors)
        {
            connector.isConnected = false;

            if (connector.connectedWire != null)
            {
                WireStart wireStart = connector.connectedWire.GetComponent<WireStart>();
                wireStart?.ResetWire();
                connector.connectedWire = null;
            }
        }

        WireStart[] allWires = FindObjectsOfType<WireStart>(true);
        foreach (var wire in allWires) wire.ResetWire();
    }

    public void ExitPanel()
    {
        if (Input.GetKeyDown(playerController.exitMenu))
        {
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        ResetWires();
        if (panel != null) panel.SetActive(false);
    }
}