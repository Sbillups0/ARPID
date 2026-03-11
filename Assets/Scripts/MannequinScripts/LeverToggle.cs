using UnityEngine;

public class LeverToggle : MonoBehaviour
{
    [Header("Lever")]
    public HingeJoint hinge;                    // lever hinge
    public float activateAngle = -75f;           // degrees past which it triggers
    public bool useGreaterThan = false;          // depends on your hinge direction
    public bool oneShot = true;                 // trigger once and stay

    [Header("Door Reveal Objects")]
    public GameObject wallClosed;               // the solid wall
    public GameObject wallWithDoorway;          // the doorway version

    [Header("Optional")]
    public bool startRevealed = false;

    bool revealed;

    void Start()
    {
        SetRevealed(startRevealed);

        if (hinge == null)
            hinge = GetComponent<HingeJoint>();
    }

    void Update()
    {
        if (hinge == null) return;
        if (oneShot && revealed) return;

        // hinge.angle is in degrees, relative to joint's reference position
        float a = hinge.angle;

        bool pulled =
            useGreaterThan ? (a >= activateAngle) : (a <= activateAngle);

        if (pulled)
            SetRevealed(true);
    }

    public void SetRevealed(bool value)
    {
        revealed = value;

        if (wallClosed) wallClosed.SetActive(!revealed);
        if (wallWithDoorway) wallWithDoorway.SetActive(revealed);
    }
}