using UnityEngine;

public class WallSwapGroup : MonoBehaviour
{
    [Header("Wall States")]
    public GameObject stateA;
    public GameObject stateB;

    [Header("What the player is 'looking at'")]
    public Transform watchTarget;
    public float maxLookDistance = 8f;

    [Tooltip("If true, requires a clear ray from camera to watchTarget.")]
    public bool requireLineOfSight = true;

    [Tooltip("Things that can block sight (walls, props, etc).")]
    public LayerMask occlusionMask = ~0;

    [Tooltip("Layers to ignore when checking line-of-sight (set this to Enemy so mannequins don't block).")]
    public LayerMask ignoreOccluderLayers;

    [Header("Swap Conditions")]
    public float lookAwaySecondsToSwap = 0.4f;
    public float swapCooldownSeconds = 1.0f;

    [Header("Look robustness")]
    [Tooltip("Ignores brief occlusions (enemy crossing view) before counting as 'looked away'.")]
    public float occlusionGraceSeconds = 0.20f;

    [Header("Runtime")]
    public bool startWithA = true;
    public bool swapOnlyOnce = true;

    [Header("Collision")]
    public Collider blockerCollider;          // drag your BoxCollider here
    public bool colliderMatchesStateB = true; // if StateB is the "closed" wall

    Transform _head;
    bool _inZone;
    bool _isAActive;
    bool _hasSwapped;
    float _lookAwayTimer;
    float _cooldownTimer;
    float _notLookedTimer;

    void Awake()
    {
        TryFindHead();
        _isAActive = startWithA;
        ApplyState();
    }

    void Update()
    {
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
            _notLookedTimer = 0f;
            return;
        }

        // Not looked at this frame
        _notLookedTimer += Time.deltaTime;

        // Ignore brief loss of sight (enemy crossing, tiny jitter)
        if (_notLookedTimer < occlusionGraceSeconds)
            return;

        _lookAwayTimer += Time.deltaTime;

        if (_lookAwayTimer >= lookAwaySecondsToSwap)
            SwapToB();
    }

    void TryFindHead()
    {
        var cam = Camera.main;
        if (cam != null) _head = cam.transform;
    }

    public void SetInZone(bool inZone)
    {
        _inZone = inZone;
        if (!inZone)
        {
            _lookAwayTimer = 0f;
            _notLookedTimer = 0f;
        }
    }

    bool IsLookedAt()
    {
        if (watchTarget == null) return false;

        var cam = Camera.main;
        if (cam == null) return false;

        // On-screen test (Option 3)
        Vector3 vp = cam.WorldToViewportPoint(watchTarget.position);
        if (vp.z <= 0f) return false;
        if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return false;

        // Distance gate
        float dist = Vector3.Distance(_head.position, watchTarget.position);
        if (dist > maxLookDistance) return false;

        if (requireLineOfSight)
        {
            Vector3 dir = (watchTarget.position - _head.position).normalized;

            // Allow excluding enemies from blocking LOS
            int mask = occlusionMask & ~ignoreOccluderLayers.value;

            if (Physics.Raycast(_head.position, dir, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            {
                bool hitIsRelevant =
                    hit.transform == transform ||
                    hit.transform.IsChildOf(transform) ||
                    (stateA != null && hit.transform.IsChildOf(stateA.transform)) ||
                    (stateB != null && hit.transform.IsChildOf(stateB.transform));

                // If we hit something that isn't part of this doorway group, LOS is blocked
                if (!hitIsRelevant) return false;
            }
        }

        return true;
    }

    void SwapToB()
    {
        if (!_isAActive) return; // already B

        _isAActive = false;
        ApplyState();

        _lookAwayTimer = 0f;
        _notLookedTimer = 0f;
        _cooldownTimer = swapCooldownSeconds;

        if (swapOnlyOnce) _hasSwapped = true;
    }

    void ApplyState()
    {
        if (stateA != null) stateA.SetActive(_isAActive);
        if (stateB != null) stateB.SetActive(!_isAActive);

        if (blockerCollider != null)
        {
            bool bActive = !_isAActive;
            blockerCollider.enabled = colliderMatchesStateB ? bActive : !bActive;
        }
    }
}