using UnityEngine;

public class WallSwapGroup : MonoBehaviour
{
    [Header("Wall States")]
    public GameObject stateA;
    public GameObject stateB;

    [Header("What the player is 'looking at'")]
    public Transform watchTarget;
    public float maxLookAngle = 25f;
    public float maxLookDistance = 8f;
    public bool requireLineOfSight = true;
    public LayerMask occlusionMask = ~0;

    [Header("Swap Conditions")]
    public float lookAwaySecondsToSwap = 0.4f;
    public float swapCooldownSeconds = 1.0f;

    [Header("Runtime")]
    public bool startWithA = true;
    public bool swapOnlyOnce = true;   // NEW
    [Header("Collision")]
    public Collider blockerCollider;  // drag your BoxCollider here
    public bool colliderMatchesStateB = true;

    Transform _head;
    bool _inZone;
    bool _isAActive;
    bool _hasSwapped;                 // NEW
    float _lookAwayTimer;
    float _cooldownTimer;

    void Awake()
    {
        TryFindHead();

        _isAActive = startWithA;
        ApplyState();
    }

    void Update()
    {
        // XR tip: camera can be null at Awake in some XR setups, so retry.
        if (_head == null) TryFindHead();

        if (!_inZone || _head == null) return;
        if (swapOnlyOnce && _hasSwapped) return;

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        bool lookedAt = IsLookedAt();

        if (lookedAt)
        {
            _lookAwayTimer = 0f;
            return;
        }

        _lookAwayTimer += Time.deltaTime;

        if (_lookAwayTimer >= lookAwaySecondsToSwap)
        {
            SwapToB(); // NOTE: no toggling anymore
        }
    }

    void TryFindHead()
    {
        var cam = Camera.main;
        if (cam != null) _head = cam.transform;
    }

    public void SetInZone(bool inZone)
    {
        _inZone = inZone;
        if (!inZone) _lookAwayTimer = 0f;
    }

    bool IsLookedAt()
    {
        if (watchTarget == null) return false;

        Vector3 toTarget = watchTarget.position - _head.position;
        float dist = toTarget.magnitude;
        if (dist > maxLookDistance) return false;

        Vector3 dir = toTarget / dist;

        float angle = Vector3.Angle(_head.forward, dir);
        if (angle > maxLookAngle) return false;

        if (requireLineOfSight)
        {
            if (Physics.Raycast(_head.position, dir, out RaycastHit hit, dist, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                // Important: allow hitting either the group OR the active wall states.
                // (Raycasts often hit colliders on stateA/stateB, not the parent.)
                bool hitIsRelevant =
                    hit.transform == transform ||
                    hit.transform.IsChildOf(transform) ||
                    (stateA != null && hit.transform.IsChildOf(stateA.transform)) ||
                    (stateB != null && hit.transform.IsChildOf(stateB.transform));

                if (!hitIsRelevant) return false;
            }
        }

        return true;
    }

    void SwapToB()
    {
        // If we started with A, swapping means A->B.
        // If you ever start with B, you can add SwapToA too.
        if (!_isAActive) return; // already B

        _isAActive = false;
        ApplyState();

        _lookAwayTimer = 0f;
        _cooldownTimer = swapCooldownSeconds;

        if (swapOnlyOnce) _hasSwapped = true;
    }

    void ApplyState()
    {
        if (stateA != null) stateA.SetActive(_isAActive);
        if (stateB != null) stateB.SetActive(!_isAActive);

        if (blockerCollider != null)
        {
            // If StateB is the "closed" wall, enable collider when B is active
            bool bActive = !_isAActive;
            blockerCollider.enabled = colliderMatchesStateB ? bActive : !bActive;
        }
    }
}