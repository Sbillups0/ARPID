using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class FreezeOnLook : MonoBehaviour
{
    [Header("Activation")]
    public bool behaviorEnabled = true;
    [Header("Death")]
    public string playerTag = "Player";
    public bool killOnlyWhenUnwatched = true;
    bool isDying;
    public float killCheckInterval = 0f;   // prevents spam in OnTriggerStay
    float nextKillCheckTime;
    [Header("Death - look check")]
    public float deathLookConeDegrees = 55f;   // smaller = stricter "must be looking at it"
    public Transform deathAimPoint;            
    [Header("Death - distance fallback")]
    public float killDistance = 0.6f;              // tune (0.4–0.8 usually)
    public Transform killPoint;                   
    public float killCooldown = 0.25f;             // prevents double-firing
    float nextAllowedKillTime;
    [Header("References")]
    public Transform targetCamera;          // XR camera 
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
    public Renderer[] renderersToCheck;     
    public float visibilityPadding = 0.02f;

    [Header("Watch smoothing")]
    public float watchGraceOn = 0.05f;      // seconds required to become watched
    public float watchGraceOff = 0.10f;     // seconds required to become unwatched

    [Header("Activation")]
    public bool armed = false;                 // threat armed?
    public float graceAfterArm = 1.0f;         // seconds before they can move after arming
    // Add this field (optional)
    [Header("Default (no animation) state")]
    public bool keepDefaultUntilArmed = true;
    float armedAtTime = -999f;
    float watchTimer;
    bool watchedState;

    float repathTimer;
    float poseTimer;
    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioSource deathSource;

    [Header("Footsteps")]
    public AudioClip[] footstepClips;
    public float stepInterval = 0.45f;          // base time between steps
    public float minMoveSpeedForSteps = 0.2f;   // agent velocity threshold
    public float stepIntervalSpeedScale = 1.0f; // higher = faster steps when running
    float nextStepTime;

    [Header("Death SFX")]
    public AudioClip deathClip;
    [Range(0f, 1f)] public float deathVolume = 1f;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isDying = false;
    }
    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        isDying = false;
        nextKillCheckTime = 0f;
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        if (renderersToCheck == null || renderersToCheck.Length == 0)
            renderersToCheck = GetComponentsInChildren<Renderer>();

        poseTimer = Random.Range(minPoseHold, maxPoseHold);

        // keep mannequin in its scene/default pose until armed
        if (keepDefaultUntilArmed && animator != null)
            animator.enabled = false;
    }

    void Update()
    {
        if (targetCamera == null || playerRoot == null) return;

        // If not armed, always frozen
        if (!armed)
        {
            Freeze();
            return;
        }

        // Grace period after arming: still frozen
        if (Time.time < armedAtTime + graceAfterArm)
        {
            Freeze();
            return;
        }

        bool nowWatched = IsWatched();

        watchTimer += nowWatched ? Time.deltaTime : -Time.deltaTime;
        watchTimer = Mathf.Clamp(watchTimer, -watchGraceOff, watchGraceOn);

        watchedState = watchTimer >= 0f;

        if (watchedState) Freeze();
        else
        {
            UnfreezeAndMove();
            UpdatePoseSwap();
            TryKillByDistance(); 
            UpdateFootsteps();     
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

            // Skip if too far 
            float dist = Vector3.Distance(camPos, r.bounds.center);
            if (dist > maxLookDistance) continue;

            // Check a few key points on the bounds
            if (BoundsVisibleAndNotOccluded(cam, r.bounds))
                return true;
        }

        return false;
    }

    bool BoundsVisibleAndNotOccluded(Camera cam, Bounds b)
    {
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

            // random poses
            int next = Random.Range(0, 3);
            animator.SetInteger(poseIndexParam, next);
        }
    }
    public void Arm(float graceSeconds)
    {
        armed = true;
        graceAfterArm = graceSeconds;
        armedAtTime = Time.time;

        watchTimer = 0f;
        watchedState = true;

        // turn animation on only when armed
        if (animator != null && keepDefaultUntilArmed)
            animator.enabled = true;

        Freeze();
    }
    void OnTriggerEnter(Collider other) => TryKill(other);
    void OnTriggerStay(Collider other)  => TryKill(other);

    void TryKill(Collider other)
    {
        if (isDying) return;

        if (!armed) return;
        if (Time.time < armedAtTime + graceAfterArm) return;

        
        if (!other.transform.root.CompareTag(playerTag)) return;

        // use an immediate look check for death (not smoothed watchedState).
        if (killOnlyWhenUnwatched && IsLookingAtMannequinForDeath()) return;

        isDying = true;
        PlayDeathSfx();
        Freeze();

        if (DeathFade.Instance != null)
            DeathFade.Instance.DieAndReload();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    bool IsLookingAtMannequinForDeath()
    {
        // Get a camera
        Camera cam = targetCamera ? targetCamera.GetComponent<Camera>() : null;
        if (cam == null) cam = Camera.main;
        if (cam == null) return false;

        // Choose an aim point on the mannequin
        Vector3 aim;
        if (deathAimPoint != null) aim = deathAimPoint.position;
        else if (renderersToCheck != null && renderersToCheck.Length > 0 && renderersToCheck[0] != null)
            aim = renderersToCheck[0].bounds.center;
        else
            aim = transform.position;

        Vector3 to = (aim - cam.transform.position);
        float dist = to.magnitude;
        if (dist <= 0.001f) return true;

        Vector3 dir = to / dist;

        // if it's behind you, you are NOT looking at it.
        float dot = Vector3.Dot(cam.transform.forward, dir);
        if (dot <= 0f) return false;

        float cos = Mathf.Cos(deathLookConeDegrees * Mathf.Deg2Rad);
        if (dot < cos) return false;

        // Line of sight (occluderMask)
        if (Physics.Raycast(cam.transform.position, dir, dist, occluderMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }
    void TryKillByDistance()
    {
        if (isDying) return;
        if (!armed) return;
        if (Time.time < armedAtTime + graceAfterArm) return;
        if (Time.time < nextAllowedKillTime) return;

        // Only allow kill when not looked at
        if (killOnlyWhenUnwatched && IsLookingAtMannequinForDeath()) return;

        Vector3 mannequinPos = (killPoint != null) ? killPoint.position : transform.position;

        // Use camera position (or playerRoot) as the "player" point
        Vector3 playerPos = (targetCamera != null) ? targetCamera.position : playerRoot.position;

        float d = Vector3.Distance(mannequinPos, playerPos);
        if (d > killDistance) return;

        nextAllowedKillTime = Time.time + killCooldown;

        isDying = true;
        PlayDeathSfx();
        Freeze();

        if (DeathFade.Instance != null)
            DeathFade.Instance.DieAndReload();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    void UpdateFootsteps()
    {
        if (footstepSource == null) return;
        if (footstepClips == null || footstepClips.Length == 0) return;
        if (agent == null) return;
        if (isDying) return;

        // Only play when actually moving
        float speed = agent.velocity.magnitude;
        bool shouldStep = !agent.isStopped && speed >= minMoveSpeedForSteps;

        if (!shouldStep)
        {
            // reset timer so it doesn't instantly step when resuming
            nextStepTime = Time.time + 0.05f;
            return;
        }

        if (Time.time < nextStepTime) return;

        // Optionally make steps faster when moving faster
        float interval = stepInterval;
        if (stepIntervalSpeedScale > 0.001f)
        {
            interval = stepInterval / Mathf.Max(0.5f, speed * stepIntervalSpeedScale);
        }

        nextStepTime = Time.time + interval;

        // Pick a random clip
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        footstepSource.PlayOneShot(clip);
    }

    void PlayDeathSfx()
    {
        if (deathSource == null || deathClip == null) return;
        deathSource.PlayOneShot(deathClip, deathVolume);
    }
}