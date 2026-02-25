using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class ChangeOnGrab : MonoBehaviour
{
    [Header("Model References")]
    public GameObject cleanModel;
    public GameObject bloodyModel;

    [Header("Settings")]
    public float changeDelay = 1f;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    Coroutine swapRoutine;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        cleanModel.SetActive(true);
        bloodyModel.SetActive(false);
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (swapRoutine != null)
            StopCoroutine(swapRoutine);

        swapRoutine = StartCoroutine(SwapAfterDelay());
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (swapRoutine != null)
            StopCoroutine(swapRoutine);

        // Optional: revert when dropped
        cleanModel.SetActive(true);
        bloodyModel.SetActive(false);
    }

    IEnumerator SwapAfterDelay()
    {
        yield return new WaitForSeconds(changeDelay);

        cleanModel.SetActive(false);
        bloodyModel.SetActive(true);
    }
}