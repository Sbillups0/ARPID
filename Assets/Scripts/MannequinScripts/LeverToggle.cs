using UnityEngine;

public class LeverToggle : MonoBehaviour
{
    [Header("Lever")]
    public HingeJoint hinge;
    public float activateAngle = -75f;
    public bool useGreaterThan = false;
    public bool oneShot = true;

    [Header("Locking")]
    public bool lockLeverDownOnActivate = true;
    public bool disableGrabOnActivate = true;   // requires XRGrabInteractable on same object (optional)

    [Header("Door Reveal Objects")]
    public GameObject wallClosed;
    public GameObject wallWithDoorway;

    [Header("Optional")]
    public bool startRevealed = false;

    bool revealed;

    void Start()
    {
        if (hinge == null)
            hinge = GetComponent<HingeJoint>();

        SetRevealed(startRevealed);
    }

    void Update()
    {
        if (hinge == null) return;
        if (oneShot && revealed) return;

        float a = hinge.angle;

        bool pulled = useGreaterThan ? (a >= activateAngle) : (a <= activateAngle);

        if (pulled)
            SetRevealed(true);
    }

    public void SetRevealed(bool value)
    {
        revealed = value;

        if (wallClosed) wallClosed.SetActive(!revealed);
        if (wallWithDoorway) wallWithDoorway.SetActive(revealed);

        if (revealed && lockLeverDownOnActivate)
            LockLeverDown();
    }

    void LockLeverDown()
    {
        if (hinge == null) return;

        // Force the hinge limits to a tiny window around the DOWN limit 
        var limits = hinge.limits;
        float down = limits.min;

        limits.min = down - 0.5f;
        limits.max = down + 0.5f;

        hinge.limits = limits;
        hinge.useLimits = true;

        var rb = hinge.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
        }

        // Optional: prevent grabbing after activation
        if (disableGrabOnActivate)
        {
            var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null) grab.enabled = false;
        }
    }
}