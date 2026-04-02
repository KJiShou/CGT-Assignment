using UnityEngine;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{
    public NavMeshAgent agent;
    public float range = 10f;

    public Transform centerPoint; // NPC moving range

    public Animator anim;

    public float intervalTime = 3.0f;
    private float timer = 0f;

    private Vector3 destination = Vector3.zero;
    public float stopRadius = 0.5f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // if not specific centerPoint, use NPC transform as center point
        if (centerPoint == null)
        {
            centerPoint = this.transform;
        }
    }

    private void Update()
    {
        if (IsAgentDone())
        {
            // NPC reach destination, start count down
            anim.SetBool("isWalking", false);

            timer += Time.deltaTime;
            if (timer >= intervalTime)
            {
                // find next random position as destination
                if (RandomPoint(centerPoint.position, range, out Vector3 point))
                {
                    Debug.DrawRay(point, Vector3.up * 2f, Color.blue, 2.0f);
                    destination = point;
                    agent.SetDestination(point);
                    timer = 0f;
                }
            }
        }
        else
        {
            // NPC is walking
            anim.SetBool("isWalking", true);
            timer = 0f;
        }
    }

    /// <summary>
    /// Is NPC reached destination, or stuck at a point
    /// </summary>
    private bool IsAgentDone()
    {
        // 1. 如果路径还在计算中，肯定没到
        // if path still counting, then haven't reach destination
        if (agent.pathPending) return false;

        // ==========================================
        // If NPC enter destination stop range, count as reached destination
        // ==========================================
        float absoluteDistance = Vector3.Distance(transform.position, destination);
        if (absoluteDistance <= stopRadius)
        {
            return true;
        }

        // NavMesh distance count
        if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            return true;
        }

        // prevent stuck, if NPC velocity near to 0, return true
        if (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.velocity.sqrMagnitude < 0.01f)
        {
            return true;
        }

        return false;
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;

        // Eliminate y axis offset, pull y axis back to ground
        randomPoint.y = center.y;

        NavMeshHit hit;

        int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
        if (NavMesh.SamplePosition(randomPoint, out hit, range, walkableMask))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private void OnDrawGizmos()
    {
        if (centerPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(centerPoint.position, range);
        }

        if (destination != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(destination, stopRadius);
        }
    }
}