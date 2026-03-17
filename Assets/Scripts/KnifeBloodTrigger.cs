using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class KnifeBloodTrigger : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private MannequinBloodTarget currentTarget;

    private void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        MannequinBloodTarget target = collision.collider.GetComponentInParent<MannequinBloodTarget>();

        if (target != null)
        {
            currentTarget = target;
            currentTarget.SetBloody();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        MannequinBloodTarget target = collision.collider.GetComponentInParent<MannequinBloodTarget>();

        if (target != null && target == currentTarget)
        {
            currentTarget.SetClean();
            currentTarget = null;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (currentTarget != null)
        {
            currentTarget.SetClean();
            currentTarget = null;
        }
    }
}