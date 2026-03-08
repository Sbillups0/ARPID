using UnityEngine;
using UnityEngine.AI;

public class FreezeOnLook : MonoBehaviour
{
    [Header("References")]
    public Transform targetCamera;          // XR camera (CenterEye)
    public NavMeshAgent agent;             

    [Header("Look detection")]
    public float maxLookDistance = 30f;
    public LayerMask occluderMask;          // walls, props (NOT the mannequin layer)

    [Header("Movement when not watched")]
    public Transform playerRoot;            // XR rig root to chase
    public float repathInterval = 0.25f;
    public float stopDistance = 1.3f;

    [Header("Pose swapping (optional)")]
    public Animator animator;               // optional
    public string poseIndexParam = "Pose";  // int param in Animator
    public float minPoseHold = 0.25f;
    public float maxPoseHold = 1.5f;

    [Header("Screen visibility")]
    public Renderer[] renderersToCheck;     // leave empty to auto-grab
    public float visibilityPadding = 0.02f; // helps at screen edges

    [Header("Watch smoothing")]
    public float watchGraceOn = 0.05f;      // seconds required to become watched
    public float watchGraceOff = 0.10f;     // seconds required to become unwatched
    float watchTimer;
    bool watchedState;

    float repathTimer;
    float poseTimer;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        if (renderersToCheck == null || renderersToCheck.Length == 0)
            renderersToCheck = GetComponentsInChildren<Renderer>();

        poseTimer = Random.Range(minPoseHold, maxPoseHold);
    }

    void Update()
    {
        if (targetCamera == null || playerRoot == null) return;

        bool nowWatched = IsWatched();

        // Smooth in/out to prevent flicker at screen edges
        watchTimer += nowWatched ? Time.deltaTime : -Time.deltaTime;
        watchTimer = Mathf.Clamp(watchTimer, -watchGraceOff, watchGraceOn);

        watchedState = watchTimer >= 0f;

        if (watchedState)
        {
            Freeze();
        }
        else
        {
            UnfreezeAndMove();
            UpdatePoseSwap();
        }
    }

    bool IsWatched()
    {
        if (targetCamera == null) return false;

        Camera cam = targetCamera.GetComponent<Camera>();
        if (cam == null) return false;

        Vector3 camPos = cam.transform.position;

        // Check if ANY renderer bounds are on-screen & visible
        foreach (var r in renderersToCheck)
        {
            if (r == null) continue;

            // Skip if too far (use bounds center for distance)
            float dist = Vector3.Distance(camPos, r.bounds.center);
            if (dist > maxLookDistance) continue;

            // Check a few key points on the bounds (center + extents)
            if (BoundsVisibleAndNotOccluded(cam, r.bounds))
                return true;
        }

        return false;
    }

    bool BoundsVisibleAndNotOccluded(Camera cam, Bounds b)
    {
        // Sample center + 6 face points (good coverage, cheap)
        Vector3 c = b.center;
        Vector3 e = b.extents;

        Vector3[] pts =
        {
            c,
            c + new Vector3( e.x, 0f, 0f),
            c + new Vector3(-e.x, 0f, 0f),
            c + new Vector3(0f,  e.y, 0f),
            c + new Vector3(0f, -e.y, 0f),
            c + new Vector3(0f, 0f,  e.z),
            c + new Vector3(0f, 0f, -e.z),
        };

        foreach (var p in pts)
        {
            if (PointOnScreen(cam, p) && HasLineOfSight(cam, p))
                return true;
        }

        return false;
    }

    bool PointOnScreen(Camera cam, Vector3 worldPoint)
    {
        Vector3 vp = cam.WorldToViewportPoint(worldPoint);

        // vp.z < 0 means behind the camera
        if (vp.z <= 0f) return false;

        float pad = visibilityPadding;
        return vp.x >= -pad && vp.x <= 1f + pad &&
               vp.y >= -pad && vp.y <= 1f + pad;
    }

    bool HasLineOfSight(Camera cam, Vector3 worldPoint)
    {
        Vector3 origin = cam.transform.position;
        Vector3 dir = worldPoint - origin;
        float dist = dir.magnitude;
        dir /= dist;

        // If something on occluderMask blocks the line, it's NOT watched.
        if (Physics.Raycast(origin, dir, dist, occluderMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    void Freeze()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (animator != null)
            animator.speed = 0f;
    }

    void UnfreezeAndMove()
    {
        if (animator != null)
            animator.speed = 1f;

        if (agent == null) return;

        agent.isStopped = false;

        // chase player, but don’t repath every frame
        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;

            Vector3 targetPos = playerRoot.position;
            agent.stoppingDistance = stopDistance;
            agent.SetDestination(targetPos);
        }
    }

    void UpdatePoseSwap()
    {
        if (animator == null) return;

        poseTimer -= Time.deltaTime;
        if (poseTimer <= 0f)
        {
            poseTimer = Random.Range(minPoseHold, maxPoseHold);

            // Example: randomly pick between 0..2
            int next = Random.Range(0, 3);
            animator.SetInteger(poseIndexParam, next);
        }
    }
}