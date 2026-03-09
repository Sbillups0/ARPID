using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabArmMannequins : MonoBehaviour
{
    [Header("Existing mannequin behavior")]
    public FreezeOnLook[] mannequins;
    public float gracePeriod = 1.5f;

    [Header("Optional GameManager trigger")]
    public BackroomsTestGameManager gameManager;
    public string grabbedFlagName = "FlashlightGrabbed";
    public bool setFlagOnGrab = true;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    bool hasArmed = false;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void OnEnable()
    {
        if (grab != null)
            grab.selectEntered.AddListener(OnGrab);
    }

    void OnDisable()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (hasArmed) return;
        hasArmed = true;

        foreach (var m in mannequins)
        {
            if (m == null) continue;
            m.Arm(gracePeriod);
        }

        if (setFlagOnGrab && gameManager != null && !string.IsNullOrEmpty(grabbedFlagName))
        {
            gameManager.SetFlag(grabbedFlagName, true);
        }

    }
}