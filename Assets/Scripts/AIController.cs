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

    public bool isSelecting = false;
    public bool resetAnim = false;
    public string animTrigger = "Idle";

    private int hashWalking;
    private int hashIdle;
    private int walkableMask;
    private float sqrStopRadius;

    private string lastAnimTrigger = "Neutral";
    private int activeTriggerHash = 0;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // if not specific centerPoint, use NPC transform as center point
        if (centerPoint == null)
        {
            centerPoint = this.transform;
        }

        hashWalking = Animator.StringToHash("Walking");
        hashIdle = Animator.StringToHash("Idle");
        walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");

        sqrStopRadius = stopRadius * stopRadius;
    }

    private void Update()
    {
        if (isSelecting)
        {
            HandleSelectionState();
        }
        else
        {
            HandleRoamingState();
        }
    }

    private void HandleSelectionState()
    {
        agent.isStopped = true;
        timer = 0f;

        //Debug.Log($"{animTrigger}");
        if (anim.GetBool(hashWalking)) anim.SetBool(hashWalking, false);

        if (lastAnimTrigger != animTrigger)
        {
            // Pause last animation
            if (activeTriggerHash != 0 && activeTriggerHash != hashIdle && activeTriggerHash != hashWalking)
            {
                anim.SetBool(activeTriggerHash, false);
            }

            lastAnimTrigger = animTrigger;

            if (animTrigger == "Neutral" || animTrigger == "Idle")
            {
                if (!anim.GetBool(hashIdle)) anim.SetBool(hashIdle, true);
                activeTriggerHash = hashIdle;
            }
            else
            {
                anim.SetBool(hashIdle, false);
                activeTriggerHash = Animator.StringToHash(animTrigger);
                bool hasParam = false;
                foreach (var param in anim.parameters)
                {
                    if (param.nameHash == activeTriggerHash)
                    {
                        hasParam = true;
                        break;
                    }
                }

                if (hasParam)
                {
                    anim.SetBool(activeTriggerHash, true);
                }else
                {
                    Debug.Log("No Animation Provided");
                }
            }
        }
    }

    private void HandleRoamingState()
    {
        agent.isStopped = false;

        if (activeTriggerHash != 0 && activeTriggerHash != hashIdle && activeTriggerHash != hashWalking)
        {
            anim.SetBool(activeTriggerHash, false);
            activeTriggerHash = 0;       // Clear Hash
            lastAnimTrigger = "Neutral"; 
        }

        if (resetAnim)
        {
            resetAnim = false;
            agent.ResetPath(); // Clear path
            timer = 0f;        
        }

        if (IsAgentDone())
        {
            if (!anim.GetBool(hashIdle)) anim.SetBool(hashIdle, true);
            if (anim.GetBool(hashWalking)) anim.SetBool(hashWalking, false);
            activeTriggerHash = hashIdle;

            timer += Time.deltaTime;
            if (timer >= intervalTime)
            {
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
            anim.SetBool(hashIdle, false);
            anim.SetBool(hashWalking, true);
            timer = 0f;
        }
    }

    /// <summary>
    /// Is NPC reached destination, or stuck at a point
    /// </summary>
    private bool IsAgentDone()
    {
        // if path still counting, then haven't reach destination
        if (agent.pathPending) return false;

        // ==========================================
        // If NPC enter destination stop range, count as reached destination
        // ==========================================
        float sqrDistance = (transform.position - destination).sqrMagnitude;
        if (sqrDistance <= sqrStopRadius)
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