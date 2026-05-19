using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class AiPathing : MonoBehaviour
{
    public NavMeshAgent agent; // Reference to the NavMeshAgent component
    public Transform[] waypoints; // Array of waypoints the NPC can move to
    public Transform[] lingerPoints; // Array of points where the NPC will linger longer
    public float shortBreakDuration = 2f; // Duration of short breaks
    public float lingerDuration = 5f; // Duration of lingering at specific points
    public float minTimeBetweenMoves = 3f; // Minimum time between moves
    public float maxTimeBetweenMoves = 8f; // Maximum time between moves

    private float moveCooldown; // Time until the next move
    private bool isLingerPoint; // Whether the NPC is heading to a linger point
    private bool isWaiting; // Whether the NPC is currently waiting
    private Transform currentTarget; // The current target waypoint

    void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (waypoints.Length == 0)
        {
            Debug.LogError("No waypoints assigned to AiPathing script.");
            return;
        }

        // Start the NPC's movement
        SetNextDestination();
    }

    void Update()
    {
        if (isWaiting)
        {
            return; // Skip movement logic while waiting
        }

        // Check if the NPC has reached its destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Decide whether to take a short break or move to the next destination
            StartCoroutine(HandleBreak());
        }
    }

    private void SetNextDestination()
    {
        // Randomly decide whether to go to a linger point or a regular waypoint
        isLingerPoint = lingerPoints.Length > 0 && Random.value < 0.3f; // 30% chance to go to a linger point

        if (isLingerPoint)
        {
            currentTarget = lingerPoints[Random.Range(0, lingerPoints.Length)];
        }
        else
        {
            currentTarget = waypoints[Random.Range(0, waypoints.Length)];
        }

        // Set the agent's destination
        agent.SetDestination(currentTarget.position);
    }

    private IEnumerator HandleBreak()
    {
        isWaiting = true;

        // Determine the duration of the break
        float breakDuration = isLingerPoint ? lingerDuration : shortBreakDuration;

        // Wait for the break duration
        yield return new WaitForSeconds(breakDuration);

        // Wait for a random cooldown before moving again
        moveCooldown = Random.Range(minTimeBetweenMoves, maxTimeBetweenMoves);
        yield return new WaitForSeconds(moveCooldown);

        // Set the next destination
        SetNextDestination();

        isWaiting = false;
    }
}
