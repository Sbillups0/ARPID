using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabArmMannequins : MonoBehaviour
{
    public FreezeOnLook[] mannequins;
    public float gracePeriod = 1.0f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    bool hasArmed = false;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
    }

    void OnDisable()
    {
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

        // Optional: if you have a Light component, turn it on here
        // var light = GetComponentInChildren<Light>();
        // if (light) light.enabled = true;
    }
}