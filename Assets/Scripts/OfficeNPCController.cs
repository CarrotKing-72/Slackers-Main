using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class OfficeNPCController : MonoBehaviour
{
    [Header("Detection Settings")]
    public float alertRadius = 5f;
    public float maxSuspicion = 20f;
    public float currentSuspicion = 0f;

    [Header("Random Idle Settings")]
    public float idleRadius = 5f;              // Maximum distance for random idle movement
    public float idleDelay = 2f;               // Time between picking new idle positions
    public float idleSpeed = 2f;               // Movement speed while idle

    [Header("Alert Settings")]
    public float idleResumeDelay = 5f;         // Time after alert to resume idle

    [HideInInspector] public Transform manualTarget;  // Temporary alert target

    private UnityEngine.AI.NavMeshAgent agent;
    private Coroutine idleCoroutine;
    private Coroutine resumeIdleCoroutine;

    private void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = idleSpeed;

        StartIdle();
    }

    private void Update()
    {
        // Move toward alert target if exists
        if (manualTarget != null)
        {
            agent.isStopped = false;
            agent.SetDestination(manualTarget.position);
        }
    }

    /// <summary>
    /// Pick a random idle position within a radius
    /// </summary>
    private IEnumerator IdleRoutine()
    {
        while (true)
        {
            if (manualTarget == null)
            {
                Vector3 randomPos = transform.position + Random.insideUnitSphere * idleRadius;
                randomPos.y = transform.position.y;

                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out hit, idleRadius, UnityEngine.AI.NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }

            yield return new WaitForSeconds(idleDelay);
        }
    }

    private void StartIdle()
    {
        if (idleCoroutine != null)
            StopCoroutine(idleCoroutine);

        idleCoroutine = StartCoroutine(IdleRoutine());
    }

    /// <summary>
    /// Assigns a temporary alert target for the NPC
    /// </summary>
    public void SetTarget(Vector3 position)
    {
        if (manualTarget == null)
        {
            GameObject temp = new GameObject("NPCAlertTarget");
            temp.transform.position = position;
            manualTarget = temp.transform;
        }
        else
        {
            manualTarget.position = position;
        }

        // Stop any existing resume coroutine
        if (resumeIdleCoroutine != null)
            StopCoroutine(resumeIdleCoroutine);

        resumeIdleCoroutine = StartCoroutine(ResumeIdleAfterDelay());
    }

    private IEnumerator ResumeIdleAfterDelay()
    {
        yield return new WaitForSeconds(idleResumeDelay);

        // Clear alert target to resume idle movement
        if (manualTarget != null)
        {
            Destroy(manualTarget.gameObject);
            manualTarget = null;
        }
    }

    /// <summary>
    /// Alerts all NPCs to the player's position when a minigame is completed
    /// </summary>
    public static void AlertNPCs(Vector3 playerPosition)
    {
        OfficeNPCController[] npcs = Object.FindObjectsOfType<OfficeNPCController>(true);
        GameManager gm = FindFirstObjectByType<GameManager>();

        if (gm == null)
        {
            Debug.LogWarning("No GameManager found for score updates!");
            return;
        }

        float totalSuspicion = 0f;
        List<OfficeNPCController> alertedNPCs = new List<OfficeNPCController>();

        foreach (var npc in npcs)
        {
            float distance = Vector3.Distance(npc.transform.position, playerPosition);

            if (distance <= npc.alertRadius)
            {
                float suspicion = npc.maxSuspicion * (1f - (distance / npc.alertRadius));
                npc.currentSuspicion += suspicion;
                totalSuspicion += suspicion;

                npc.SetTarget(playerPosition);
                alertedNPCs.Add(npc);
            }
        }

        if (alertedNPCs.Count > 0)
        {
            float scorePerSuspicion = 0.5f;
            int rawDecrement = Mathf.RoundToInt(totalSuspicion * scorePerSuspicion / 5f) * 5;
            int decrement = Mathf.Max(rawDecrement, 5);

            gm.RemoveScore(decrement);
            Debug.Log($"Alerted {alertedNPCs.Count} NPCs. Total suspicion: {totalSuspicion:F1} → Score decreased by {decrement}. Current score: {gm.currentScore}");
        }
        else
        {
            Debug.Log("No NPCs were close enough to see the player. No score lost.");
        }
    }
}